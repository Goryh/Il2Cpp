using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class WindowsRuntimeTypeMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly MarshaledType[] _marshaledTypes;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public WindowsRuntimeTypeMarshalInfoWriter(TypeReference type)
		: base(type)
	{
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType("Il2CppWindowsRuntimeTypeName", "Il2CppWindowsRuntimeTypeName")
		};
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"il2cpp_codegen_marshal_type_to_native({sourceVariable.Load(writer.Context)}, {destinationVariable});");
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		writer.WriteLine($"il2cpp_codegen_delete_native_type({variableName});");
	}

	public override bool TreatAsValueType()
	{
		return true;
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, "il2cpp_codegen_marshal_type_from_native(" + variableName + ")");
	}
}
