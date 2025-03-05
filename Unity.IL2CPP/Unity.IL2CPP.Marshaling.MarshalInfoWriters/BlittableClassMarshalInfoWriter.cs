using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class BlittableClassMarshalInfoWriter : TypeDefinitionMarshalInfoWriter
{
	private readonly bool _marshalAsPinnedValue;

	public BlittableClassMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
		: base(context, type, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper)
	{
		_marshalAsPinnedValue = !forFieldMarshaling && !forByReferenceType && !forReturnValue && !forNativeToManagedWrapper && marshalType == MarshalType.PInvoke;
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		if (_marshalAsPinnedValue)
		{
			return new MarshaledType[1]
			{
				new MarshaledType("void*")
			};
		}
		return base.GetMarshaledTypes(context);
	}

	private static string WriteLoadPinnedNativeData(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable)
	{
		return $"({sourceVariable.Load(writer.Context)} ? (({"RuntimeObject"}*){sourceVariable.Load(writer.Context)})+1 : {"NULL"})";
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (_marshalAsPinnedValue)
		{
			return WriteLoadPinnedNativeData(writer, sourceVariable);
		}
		return base.WriteMarshalVariableToNative(writer, sourceVariable, managedVariableName, metadataAccess);
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		if (_marshalAsPinnedValue)
		{
			return WriteLoadPinnedNativeData(writer, variableName);
		}
		return base.WriteMarshalEmptyVariableToNative(writer, variableName, methodParameters);
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		if (_marshalAsPinnedValue)
		{
			return string.Empty;
		}
		return base.WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess);
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		if (!_marshalAsPinnedValue)
		{
			base.WriteMarshalVariableFromNative(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor, metadataAccess);
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		if (!_marshalAsPinnedValue)
		{
			base.WriteMarshalCleanupVariable(writer, variableName, metadataAccess, managedVariableName);
		}
	}

	public override bool TreatAsValueType()
	{
		if (_marshalAsPinnedValue)
		{
			return true;
		}
		return base.TreatAsValueType();
	}

	public override string GetNativeSize(ReadOnlyContext context)
	{
		if (_marshalAsPinnedValue)
		{
			return DefaultMarshalInfoWriter.ComputeNativeSize(base.GetMarshaledTypes(context));
		}
		return base.GetNativeSize(context);
	}
}
