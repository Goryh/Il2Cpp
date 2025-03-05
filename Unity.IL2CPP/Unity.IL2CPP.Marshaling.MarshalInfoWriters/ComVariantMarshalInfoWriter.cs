using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class ComVariantMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly MarshaledType[] _marshaledTypes;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public ComVariantMarshalInfoWriter(TypeReference type)
		: base(type)
	{
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType("Il2CppVariant", "Il2CppVariant")
		};
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"il2cpp_codegen_com_marshal_variant((RuntimeObject*)({sourceVariable.Load(writer.Context)}), &({destinationVariable}));");
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, "(RuntimeObject*)il2cpp_codegen_com_marshal_variant_result(&({0}))", variableName);
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		writer.WriteLine($"il2cpp_codegen_com_destroy_variant(&({variableName}));");
	}
}
