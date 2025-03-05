using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter : TypeDefinitionMarshalInfoWriter
{
	public TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
		: base(context, type, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper)
	{
	}

	private void WriteThrowNotSupportedException(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteStatement($"{"Exception_t*"} _marshalingException = {GetMarshalingException(writer.Context, metadataAccess)}");
		writer.WriteStatement(Emit.RaiseManagedException("_marshalingException"));
	}

	protected override void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_ToNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalToNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WriteThrowNotSupportedException(writer, metadataAccess);
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_FromNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalFromNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WriteThrowNotSupportedException(writer, metadataAccess);
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine(MarshalCleanupFunctionDeclaration());
		writer.WriteLine("{");
		writer.WriteLine("}");
	}

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return false;
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return false;
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return "il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal type '%s'.\", " + metadataAccess.Il2CppTypeFor(_typeRef) + ")";
	}
}
