using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public sealed class UnmarshalableMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private string GetMarshaledTypeName
	{
		get
		{
			if (!(_typeRef is GenericParameter))
			{
				return _typeRef.CppNameForVariable;
			}
			return "void*";
		}
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(GetMarshaledTypeName)
		};
	}

	public override string GetNativeSize(ReadOnlyContext context)
	{
		return "-1";
	}

	public UnmarshalableMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
		: base(type)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		throw new InvalidOperationException("Cannot marshal " + _typeRef.FullName + " to native!");
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		throw new InvalidOperationException("Cannot marshal " + _typeRef.FullName + " from native!");
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		throw new InvalidOperationException("Cannot marshal " + _typeRef.FullName + " to native!");
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		throw new InvalidOperationException("Cannot marshal " + _typeRef.FullName + " from native!");
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return false;
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddForwardDeclaration(_typeRef);
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return "il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal type '%s'.\", " + metadataAccess.Il2CppTypeFor(_typeRef) + ")";
	}
}
