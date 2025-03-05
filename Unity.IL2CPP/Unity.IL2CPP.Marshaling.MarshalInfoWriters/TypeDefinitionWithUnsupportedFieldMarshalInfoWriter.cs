using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public sealed class TypeDefinitionWithUnsupportedFieldMarshalInfoWriter : CustomMarshalInfoWriter
{
	private readonly FieldDefinition _faultyField;

	public override string GetNativeSize(ReadOnlyContext context)
	{
		return "-1";
	}

	public TypeDefinitionWithUnsupportedFieldMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, FieldDefinition faultyField)
		: base(type, marshalType, forFieldMarshaling: false, forByReferenceType: false, forReturnValue: false, forNativeToManagedWrapper: false)
	{
		_faultyField = faultyField;
	}

	private void WriteThrowNotSupportedException(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string exceptionVariableName = _faultyField.CppName + "Exception";
		writer.WriteStatement($"{"Exception_t*"} {exceptionVariableName} = {GetMarshalingException(writer.Context, metadataAccess)}");
		writer.WriteStatement(Emit.RaiseManagedException(exceptionVariableName));
	}

	protected override void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_ToNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalToNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WriteThrowNotSupportedException(bodyWriter, metadataAccess);
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_FromNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalFromNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WriteThrowNotSupportedException(bodyWriter, metadataAccess);
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine(MarshalCleanupFunctionDeclaration());
		writer.WriteLine("{");
		writer.WriteLine("}");
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return false;
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		if (_faultyField.FieldType.MetadataType != MetadataType.Class && (!_faultyField.FieldType.IsArray || ((ArrayType)_faultyField.FieldType).ElementType.MetadataType != MetadataType.Class))
		{
			return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal field '%s' of type '%s'.\", {metadataAccess.FieldInfo(_faultyField)}, {metadataAccess.Il2CppTypeFor(_type)})";
		}
		return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal field '%s' of type '%s': Reference type field marshaling is not supported.\", {metadataAccess.FieldInfo(_faultyField)}, {metadataAccess.Il2CppTypeFor(_type)})";
	}
}
