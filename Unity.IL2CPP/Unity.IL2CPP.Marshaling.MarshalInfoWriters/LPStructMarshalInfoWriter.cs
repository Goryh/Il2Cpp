using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class LPStructMarshalInfoWriter : DefaultMarshalInfoWriter
{
	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(_typeRef.CppNameForVariable, _typeRef.CppNameForVariable + "*")
		};
	}

	public LPStructMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType)
		: base(type)
	{
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		return sourceVariable.LoadAddress(writer.Context);
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		return Emit.Dereference(variableName);
	}
}
