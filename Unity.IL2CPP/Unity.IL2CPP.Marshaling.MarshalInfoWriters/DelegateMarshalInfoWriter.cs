using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class DelegateMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	protected readonly MarshaledType[] _marshaledTypes;

	protected readonly bool _forFieldMarshaling;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public DelegateMarshalInfoWriter(TypeReference type, bool forFieldMarshaling)
		: base(type)
	{
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType("Il2CppMethodPointer", "Il2CppMethodPointer")
		};
		_forFieldMarshaling = forFieldMarshaling;
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{destinationVariable} = il2cpp_codegen_marshal_delegate(reinterpret_cast<{"MulticastDelegate_t*"}>({sourceVariable.Load(writer.Context)}));");
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, "il2cpp_codegen_marshal_function_ptr_to_delegate<{0}>({1}, {2})", writer.Context.Global.Services.Naming.ForType(_typeRef), variableName, metadataAccess.TypeInfoFor(_typeRef));
	}

	public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
	}

	public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
	{
		writer.WriteLine($"{GetMarshaledTypes(writer.Context)[0].DecoratedName} {variableName} = NULL;");
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}
}
