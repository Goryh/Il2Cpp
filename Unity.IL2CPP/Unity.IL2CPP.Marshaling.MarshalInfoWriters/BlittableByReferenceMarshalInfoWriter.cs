using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class BlittableByReferenceMarshalInfoWriter : DefaultMarshalInfoWriter
{
	private readonly TypeReference _elementType;

	private readonly MarshalType _marshalType;

	private readonly MarshalInfo _marshalInfo;

	private DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

	private MarshaledType[] _marshaledTypes;

	public override int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		return 0;
	}

	public BlittableByReferenceMarshalInfoWriter(ByReferenceType type, MarshalType marshalType, MarshalInfo marshalInfo)
		: base(type)
	{
		_elementType = type.ElementType;
		_marshalType = marshalType;
		_marshalInfo = marshalInfo;
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		if (_marshaledTypes == null)
		{
			MarshaledType[] elementMarshaledTypes = ElementTypeMarshalInfoWriter(context).GetMarshaledTypes(context);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(elementMarshaledTypes[0].Name + "*", elementMarshaledTypes[0].DecoratedName + "*")
			};
		}
		return _marshaledTypes;
	}

	private DefaultMarshalInfoWriter ElementTypeMarshalInfoWriter(ReadOnlyContext context)
	{
		if (_elementTypeMarshalInfoWriter == null)
		{
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, _marshalType, _marshalInfo, useUnicodeCharSet: true, forByReferenceType: true);
		}
		return _elementTypeMarshalInfoWriter;
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		return WriteMarshalVariableToNative(writer.Context, variableName);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{destinationVariable} = {WriteMarshalVariableToNative(writer.Context, sourceVariable)};");
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		return WriteMarshalVariableToNative(writer.Context, sourceVariable);
	}

	private string WriteMarshalVariableToNative(ReadOnlyContext context, ManagedMarshalValue variableName)
	{
		if (_elementType.CppNameForVariable != ElementTypeMarshalInfoWriter(context).GetMarshaledTypes(context)[0].Name)
		{
			return $"reinterpret_cast<{GetMarshaledTypes(context)[0].Name}>({variableName.Load(context)})";
		}
		return variableName.Load(context);
	}

	public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, Emit.Dereference(UndecorateVariable(writer.Context, destinationVariable)), managedVariableName, metadataAccess);
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		string emptyVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
		writer.WriteVariable(writer.Context, _elementType, emptyVariableName);
		return Emit.AddressOf(emptyVariableName);
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess));
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool returnValue, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		string managedTypeName = _typeRef.CppNameForVariable;
		if (managedTypeName != GetMarshaledTypes(writer.Context)[0].DecoratedName)
		{
			return $"reinterpret_cast<{managedTypeName}>({variableName})";
		}
		return variableName;
	}

	public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		if (!(variableName == WriteMarshalVariableToNative(writer.Context, destinationVariable)))
		{
			ElementTypeMarshalInfoWriter(writer.Context).WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), destinationVariable.Dereferenced, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
		}
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		if (ElementTypeMarshalInfoWriter(writer.Context).GetMarshaledTypes(writer.Context).Length > 1)
		{
			throw new InvalidOperationException("BlittableByReferenceMarshalInfoWriter cannot marshal " + ((ByReferenceType)_typeRef).ElementType.FullName + "&.");
		}
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshaledTypeForwardDeclaration(writer);
	}
}
