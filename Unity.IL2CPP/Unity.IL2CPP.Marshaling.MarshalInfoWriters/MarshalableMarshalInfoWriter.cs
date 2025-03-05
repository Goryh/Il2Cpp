using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public abstract class MarshalableMarshalInfoWriter : DefaultMarshalInfoWriter
{
	protected MarshalableMarshalInfoWriter(TypeReference type)
		: base(type)
	{
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		string marshaledVariableName = "_" + sourceVariable.GetNiceName(writer.Context) + "_marshaled";
		WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		WriteMarshalVariableToNative(writer, sourceVariable, marshaledVariableName, managedVariableName, metadataAccess);
		return marshaledVariableName;
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		string variableNameWithoutDereference = variableName.Replace("*", "");
		string unmarshaledVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_unmarshaled";
		WriteDeclareAndAllocateObject(writer, unmarshaledVariableName, variableNameWithoutDereference, metadataAccess);
		WriteMarshalVariableFromNative(writer, variableName, new ManagedMarshalValue(unmarshaledVariableName), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: false, metadataAccess);
		return unmarshaledVariableName;
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		string marshaledVariableName = "_" + variableName.GetNiceName(writer.Context) + "_marshaled";
		WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		return marshaledVariableName;
	}
}
