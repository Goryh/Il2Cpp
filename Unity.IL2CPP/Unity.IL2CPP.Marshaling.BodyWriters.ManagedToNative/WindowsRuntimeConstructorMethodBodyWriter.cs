using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class WindowsRuntimeConstructorMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
{
	private readonly TypeReference constructedObjectType;

	private readonly MethodReference factoryMethod;

	private readonly bool isComposingConstructor;

	private readonly string thisParameter = "__this";

	private readonly string identityField;

	public WindowsRuntimeConstructorMethodBodyWriter(ReadOnlyContext context, MethodReference constructor)
		: base(context, constructor, constructor, MarshalType.WindowsRuntime, useUnicodeCharset: true)
	{
		constructedObjectType = constructor.DeclaringType;
		identityField = context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
		TypeReference[] activationFactoryTypes = constructedObjectType.GetActivationFactoryTypes(context).ToArray();
		if (constructor.Parameters.Count != 0 || activationFactoryTypes.Length == 0)
		{
			factoryMethod = constructor.GetFactoryMethodForConstructor(activationFactoryTypes, isComposing: false);
			if (factoryMethod == null)
			{
				TypeReference[] composableFactories = constructedObjectType.GetComposableFactoryTypes().ToArray();
				factoryMethod = constructor.GetFactoryMethodForConstructor(composableFactories, isComposing: true);
				isComposingConstructor = true;
			}
			if (factoryMethod == null)
			{
				throw new InvalidOperationException(string.Format("Could not find factory method for Windows Runtime constructor " + constructor.FullName + "!"));
			}
		}
	}

	protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		string staticFieldsStructName = writer.Context.Global.Services.Naming.ForStaticFieldsStruct(writer.Context, constructedObjectType);
		string constructedTypeInfo = metadataAccess.TypeInfoFor(constructedObjectType);
		string staticFieldsAccess = string.Format($"(({staticFieldsStructName}*){constructedTypeInfo}->static_fields)");
		string parameters = GetFunctionCallParametersExpression(writer.Context, localVariableNames, includesRetVal: false);
		if (parameters.Length > 0)
		{
			parameters += ", ";
		}
		if (factoryMethod == null)
		{
			WriteActivateThroughIActivationFactory(writer, staticFieldsAccess, parameters);
		}
		else if (!isComposingConstructor)
		{
			ActivateThroughCustomActivationFactory(writer, staticFieldsAccess, parameters);
		}
		else
		{
			ActivateThroughCompositionFactory(writer, staticFieldsAccess, parameters, metadataAccess);
		}
	}

	private void WriteActivateThroughIActivationFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters)
	{
		WriteDeclareActivationFactory(writer, writer.Context.Global.Services.TypeProvider.IActivationFactoryTypeReference, staticFieldsAccess);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t hr = activationFactory->ActivateInstance({parameters}reinterpret_cast<Il2CppIInspectable**>(&{thisParameter}->{identityField}));");
		writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
		writer.WriteLine();
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_register_rcw({thisParameter});");
	}

	private void ActivateThroughCustomActivationFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters)
	{
		string factoryMethodName = factoryMethod.CppName;
		TypeReference defaultInterface = constructedObjectType.Resolve().ExtractDefaultInterface();
		string defaultInterfaceTypeName = defaultInterface.CppName;
		string defaultInterfaceVariableName = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(defaultInterface);
		writer.AddIncludeForTypeDefinition(writer.Context, defaultInterface);
		WriteDeclareActivationFactory(writer, factoryMethod.DeclaringType, staticFieldsAccess);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{defaultInterfaceTypeName}* {defaultInterfaceVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t hr = activationFactory->{factoryMethodName}({parameters}&{defaultInterfaceVariableName});");
		writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
		writer.WriteLine();
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"hr = {defaultInterfaceVariableName}->QueryInterface(Il2CppIUnknown::IID, reinterpret_cast<void**>(&{thisParameter}->{identityField}));");
		writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
		writer.WriteLine();
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCache[0].qiResult = {defaultInterfaceVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCache[0].iid = &{defaultInterfaceTypeName}::IID;");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCacheSize = 1;");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_register_rcw({thisParameter});");
	}

	private void ActivateThroughCompositionFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters, IRuntimeMetadataAccess metadataAccess)
	{
		string constructedTypeInfo = metadataAccess.TypeInfoFor(constructedObjectType);
		string factoryMethodName = factoryMethod.CppName;
		TypeReference defaultInterface = constructedObjectType.Resolve().ExtractDefaultInterface();
		string defaultInterfaceTypeName = defaultInterface.CppName;
		string defaultInterfaceVariableName = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(defaultInterface);
		writer.AddIncludeForTypeDefinition(writer.Context, defaultInterface);
		writer.WriteLine("Il2CppIInspectable* outerInstance = NULL;");
		writer.WriteLine("Il2CppIInspectable** innerInstance = NULL;");
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"bool isComposedConstruction = {thisParameter}->klass != {constructedTypeInfo};");
		WriteDeclareActivationFactory(writer, factoryMethod.DeclaringType, staticFieldsAccess);
		writer.WriteLine();
		writer.WriteLine("if (isComposedConstruction)");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"outerInstance = il2cpp_codegen_com_get_or_create_ccw<Il2CppIInspectable>({thisParameter});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"innerInstance = reinterpret_cast<Il2CppIInspectable**>(&{thisParameter}->{identityField});");
		}
		writer.WriteLine();
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{defaultInterfaceTypeName}* {defaultInterfaceVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t hr = activationFactory->{factoryMethodName}({parameters}outerInstance, innerInstance, &{defaultInterfaceVariableName});");
		writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
		writer.WriteLine();
		writer.WriteLine("if (isComposedConstruction)");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("outerInstance->Release();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{defaultInterfaceVariableName}->Release();");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"hr = {defaultInterfaceVariableName}->QueryInterface(Il2CppIUnknown::IID, reinterpret_cast<void**>(&{thisParameter}->{identityField}));");
			writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
			writer.WriteLine();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCache[0].qiResult = {defaultInterfaceVariableName};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCache[0].iid = &{defaultInterfaceTypeName}::IID;");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{thisParameter}->qiShortCacheSize = 1;");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_register_rcw({thisParameter});");
		}
	}

	private static void WriteDeclareActivationFactory(ICodeWriter writer, TypeReference factoryType, string staticFieldsAccess)
	{
		string factoryTypeName = factoryType.CppName;
		string factoryFieldGetter = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(factoryType);
		writer.WriteLine($"{factoryTypeName}* activationFactory = {staticFieldsAccess}->{factoryFieldGetter}();");
	}
}
