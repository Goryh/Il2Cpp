using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class WindowsRuntimeNullableMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly MarshaledType[] _marshaledTypes;

	private readonly GenericInstanceType _ireferenceInstance;

	private readonly TypeReference _boxedType;

	private readonly string _interfaceTypeName;

	private readonly DefaultMarshalInfoWriter _boxedTypeMarshalInfoWriter;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public WindowsRuntimeNullableMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
		: base(type)
	{
		_boxedType = ((GenericInstanceType)type).GenericArguments[0];
		_ireferenceInstance = context.Global.Services.TypeFactory.CreateGenericInstanceType((TypeDefinition)context.Global.Services.TypeProvider.IReferenceType, null, _boxedType);
		_interfaceTypeName = _ireferenceInstance.CppName;
		string interfacePtrTypeName = _interfaceTypeName + "*";
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(interfacePtrTypeName, interfacePtrTypeName)
		};
		_boxedTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _boxedType, MarshalType.WindowsRuntime);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
		writer.AddIncludeForTypeDefinition(writer.Context, _ireferenceInstance);
		_boxedTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddForwardDeclaration("struct " + _ireferenceInstance.CppName);
	}

	private static FieldReference GetHasValueField(TypeDefinition nullableTypeDef, TypeResolver typeResolver)
	{
		return typeResolver.Resolve(nullableTypeDef.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Boolean));
	}

	private static FieldReference GetValueField(TypeDefinition nullableTypeDef, TypeResolver typeResolver)
	{
		return typeResolver.Resolve(nullableTypeDef.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Var));
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		SourceWritingContext context = writer.Context;
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(_typeRef);
		TypeDefinition nullableTypeDef = _typeRef.Resolve();
		string hasValueFieldGetter = GetHasValueField(nullableTypeDef, typeResolver).CppName;
		string fieldValueGetter = GetValueField(nullableTypeDef, typeResolver).CppName;
		string fieldValueVariable = sourceVariable.GetNiceName(writer.Context) + "_value";
		string boxedValueVariable = sourceVariable.GetNiceName(writer.Context) + "_boxed";
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)}.{hasValueFieldGetter})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_boxedType.CppNameForVariable} {fieldValueVariable} = {sourceVariable.Load(writer.Context)}.{fieldValueGetter};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{context.Global.Services.TypeProvider.SystemObject.CppNameForVariable} {boxedValueVariable} = Box({metadataAccess.TypeInfoFor(_boxedType)}, &{fieldValueVariable});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_get_or_create_ccw<{_interfaceTypeName}>({boxedValueVariable});");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {"NULL"};");
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		SourceWritingContext context = writer.Context;
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(_typeRef);
		TypeDefinition nullableTypeDef = _typeRef.Resolve();
		FieldReference hasValueField = GetHasValueField(nullableTypeDef, typeResolver);
		string hasValueFieldName = GetHasValueField(nullableTypeDef, typeResolver).CppName;
		FieldReference valueField = GetValueField(nullableTypeDef, typeResolver);
		string fieldValueName = GetValueField(nullableTypeDef, typeResolver).CppName;
		string getValueMethodName = context.Global.Services.TypeFactory.ResolverFor(_ireferenceInstance).Resolve(writer.Context.Global.Services.TypeProvider.IReferenceType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_Value")).CppName;
		string boxedValueVariableName = destinationVariable.GetNiceName(writer.Context) + "_value_marshaled";
		string hr = context.Global.Services.Naming.ForInteropHResultVariable();
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("Il2CppIManagedObjectHolder* imanagedObject = NULL;");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {hr} = ({variableName})->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&imanagedObject));");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IL2CPP_HR_SUCCEEDED({hr}))");
			using (new BlockWriter(writer))
			{
				writer.WriteFieldSetter(valueField, destinationVariable.Load(writer.Context) + "." + fieldValueName, "*static_cast<" + _boxedType.CppNameForVariable + "*>(UnBox(imanagedObject->GetManagedObject()))");
				writer.WriteLine("imanagedObject->Release();");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				_boxedTypeMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, boxedValueVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"hr = ({variableName})->{getValueMethodName}(&{boxedValueVariableName});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_raise_exception_if_failed({hr}, false);");
				writer.WriteLine();
				string unmarshaled = _boxedTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, boxedValueVariableName, methodParameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod, metadataAccess);
				writer.WriteFieldSetter(valueField, destinationVariable.Load(writer.Context) + "." + fieldValueName, unmarshaled);
			}
			writer.WriteLine();
			writer.WriteFieldSetter(hasValueField, destinationVariable.Load(writer.Context) + "." + hasValueFieldName, "true");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			writer.WriteFieldSetter(hasValueField, destinationVariable.Load(writer.Context) + "." + hasValueFieldName, "false");
		}
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		string unmarshaledVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
		writer.WriteVariable(writer.Context, _typeRef, unmarshaledVariableName);
		return unmarshaledVariableName;
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"({variableName})->Release();");
		}
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return _boxedTypeMarshalInfoWriter.CanMarshalTypeToNative(context);
	}

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return _boxedTypeMarshalInfoWriter.CanMarshalTypeFromNative(context);
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return _boxedTypeMarshalInfoWriter.GetMarshalingException(context, metadataAccess);
	}
}
