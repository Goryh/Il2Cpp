using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class UriMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly MarshaledType[] _marshaledTypes;

	private readonly TypeDefinition _windowsFoundationUri;

	private readonly TypeDefinition _iUriInterface;

	private readonly string _iUriInterfaceTypeName;

	public sealed override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public UriMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type)
		: base(type)
	{
		_windowsFoundationUri = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
		_iUriInterface = _windowsFoundationUri.ExtractDefaultInterface().Resolve();
		_iUriInterfaceTypeName = _iUriInterface.CppName;
		string iUriInterfaceTypeNameWithStar = _iUriInterfaceTypeName + "*";
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(iUriInterfaceTypeNameWithStar, iUriInterfaceTypeNameWithStar)
		};
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddForwardDeclaration("struct " + _iUriInterfaceTypeName + ";");
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
		writer.AddIncludeForTypeDefinition(writer.Context, _windowsFoundationUri);
		writer.AddIncludeForTypeDefinition(writer.Context, _iUriInterface);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		SourceWritingContext context = writer.Context;
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			MethodDefinition originalStringGetter = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_OriginalString" && m.Parameters.Count == 0);
			string sourceVariableAsString = sourceVariable.GetNiceName(writer.Context) + "AsString";
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{context.Global.Services.TypeProvider.SystemString.CppNameForVariable} {sourceVariableAsString};");
			writer.WriteMethodCallWithResultStatement(metadataAccess, sourceVariable.Load(writer.Context), originalStringGetter, originalStringGetter, MethodCallType.Normal, sourceVariableAsString);
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, context.Global.Services.TypeProvider.SystemString, MarshalType.WindowsRuntime);
			string sourceVariableAsHString = defaultMarshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue(sourceVariableAsString), sourceVariableAsString, metadataAccess);
			MethodDefinition windowsFoundationUriFactoryMethod = GetNativeUriFactoryMethod(writer.Context);
			string windowsFoundationUriStaticFields = $"(({writer.Context.Global.Services.Naming.ForStaticFieldsStruct(context, _windowsFoundationUri)}*){metadataAccess.TypeInfoFor(_windowsFoundationUri)}->static_fields)";
			string factoryFieldGetter = context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(windowsFoundationUriFactoryMethod.DeclaringType);
			string factoryMethodName = windowsFoundationUriFactoryMethod.CppName;
			string hr = context.Global.Services.Naming.ForInteropHResultVariable();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {hr} = {windowsFoundationUriStaticFields}->{factoryFieldGetter}()->{factoryMethodName}({sourceVariableAsHString}, {Emit.AddressOf(destinationVariable)});");
			defaultMarshalInfoWriter.WriteMarshalCleanupVariable(writer, sourceVariableAsHString, metadataAccess);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_raise_exception_if_failed({hr}, false);");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {"NULL"};");
		}
	}

	private MethodDefinition GetNativeUriFactoryMethod(ReadOnlyContext context)
	{
		TypeReference[] array = _windowsFoundationUri.GetActivationFactoryTypes(context).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			foreach (MethodDefinition method in array[i].Resolve().Methods)
			{
				if (method.Parameters.Count == 1 && method.Parameters[0].ParameterType.MetadataType == MetadataType.String)
				{
					return method;
				}
			}
		}
		throw new InvalidProgramException("Could not find factory method to create Windows.Foundation.Uri object!");
	}

	public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			MethodDefinition rawUriGetter = _iUriInterface.Methods.Single((MethodDefinition m) => m.Name == "get_RawUri" && m.Parameters.Count == 0);
			DefaultMarshalInfoWriter stringMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, writer.Context.Global.Services.TypeProvider.SystemString, MarshalType.WindowsRuntime);
			ManagedMarshalValue uriAsString = new ManagedMarshalValue(destinationVariable.GetNiceName(writer.Context) + "AsString");
			string uriAsHString = stringMarshalInfoWriter.WriteMarshalEmptyVariableToNative(writer, uriAsString, methodParameters);
			string hr = writer.Context.Global.Services.Naming.ForInteropHResultVariable();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {hr} = ({variableName})->{rawUriGetter.CppName}({Emit.AddressOf(uriAsHString)});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_raise_exception_if_failed({hr}, false);");
			writer.WriteLine();
			uriAsString = new ManagedMarshalValue(stringMarshalInfoWriter.WriteMarshalVariableFromNative(writer, uriAsHString, methodParameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod, metadataAccess));
			stringMarshalInfoWriter.WriteMarshalCleanupOutVariable(writer, uriAsHString, metadataAccess);
			writer.WriteLine();
			string uriTemp = destinationVariable.GetNiceName(writer.Context) + "Temp";
			MethodDefinition uriCtor = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_typeRef.CppNameForVariable} {uriTemp} = {Emit.NewObj(writer.Context, _typeRef, metadataAccess)};");
			writer.WriteMethodCallStatement(metadataAccess, uriTemp, uriCtor, uriCtor, MethodCallType.Normal, uriAsString.Load(writer.Context));
			destinationVariable.WriteStore(writer, uriTemp);
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			destinationVariable.WriteStore(writer, "NULL");
		}
	}

	public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"({variableName})->Release();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}
}
