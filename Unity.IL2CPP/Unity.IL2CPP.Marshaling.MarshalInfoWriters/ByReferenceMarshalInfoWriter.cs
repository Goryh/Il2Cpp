using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class ByReferenceMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly TypeReference _elementType;

	private readonly MarshalType _marshalType;

	private readonly MarshalInfo _marshalInfo;

	private readonly bool _forNativeToManagedWrapper;

	private DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

	private MarshaledType[] _marshaledTypes;

	public ByReferenceMarshalInfoWriter(ByReferenceType type, MarshalType marshalType, MarshalInfo marshalInfo, bool forNativeToManagedWrapper)
		: base(type)
	{
		_elementType = type.ElementType;
		_marshalType = marshalType;
		_marshalInfo = marshalInfo;
		_forNativeToManagedWrapper = forNativeToManagedWrapper;
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		if (_marshaledTypes == null)
		{
			_marshaledTypes = (from t in ElementTypeMarshalInfoWriter(context).GetMarshaledTypes(context)
				select new MarshaledType(t.Name + "*", t.DecoratedName + "*", t.VariableName)).ToArray();
		}
		return _marshaledTypes;
	}

	private DefaultMarshalInfoWriter ElementTypeMarshalInfoWriter(ReadOnlyContext context)
	{
		if (_elementTypeMarshalInfoWriter == null)
		{
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, _marshalType, _marshalInfo, useUnicodeCharSet: false, forByReferenceType: true, forFieldMarshaling: false, forReturnValue: false, _forNativeToManagedWrapper);
		}
		return _elementTypeMarshalInfoWriter;
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteIncludesForMarshaling(writer);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		string dereferenceVariableName = CleanVariableName(writer.Context, destinationVariable) + "_dereferenced";
		DefaultMarshalInfoWriter defaultMarshalInfoWriter = ElementTypeMarshalInfoWriter(writer.Context);
		defaultMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, dereferenceVariableName);
		defaultMarshalInfoWriter.WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, dereferenceVariableName, managedVariableName, metadataAccess);
		writer.WriteLine($"{destinationVariable} = &{dereferenceVariableName};");
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		string marshaledVariableName = "_" + variableName.GetNiceName(writer.Context) + "_marshaled";
		if (_elementType.MetadataType == MetadataType.Class && ElementTypeMarshalInfoWriter(writer.Context) is CustomMarshalInfoWriter)
		{
			WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		}
		else
		{
			string emptyVariableName = "_" + variableName.GetNiceName(writer.Context) + "_empty";
			ElementTypeMarshalInfoWriter(writer.Context).WriteNativeVariableDeclarationOfType(writer, emptyVariableName);
			MarshaledType[] marshaledTypes = GetMarshaledTypes(writer.Context);
			foreach (MarshaledType marshaledType in marshaledTypes)
			{
				writer.WriteLine($"{marshaledType.Name} {marshaledVariableName + marshaledType.VariableName} = &{emptyVariableName + marshaledType.VariableName};");
			}
		}
		return marshaledVariableName;
	}

	public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, Emit.Dereference(UndecorateVariable(writer.Context, destinationVariable)), managedVariableName, metadataAccess);
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		string dereferencedVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_unmarshaled_dereferenced";
		DefaultMarshalInfoWriter defaultMarshalInfoWriter = ElementTypeMarshalInfoWriter(writer.Context);
		defaultMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, dereferencedVariableName, variableName, metadataAccess);
		defaultMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(dereferencedVariableName), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
		return Emit.AddressOf(dereferencedVariableName);
	}

	public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		string dereferencedVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_unmarshaled_dereferenced";
		DefaultMarshalInfoWriter defaultMarshalInfoWriter = ElementTypeMarshalInfoWriter(writer.Context);
		defaultMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, dereferencedVariableName, variableName, metadataAccess);
		defaultMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(dereferencedVariableName), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
		destinationVariable.Dereferenced.WriteStore(writer, dereferencedVariableName);
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		string dereferencedVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_unmarshaled_dereferenced";
		DefaultMarshalInfoWriter defaultMarshalInfoWriter = ElementTypeMarshalInfoWriter(writer.Context);
		defaultMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, dereferencedVariableName, variableName, metadataAccess);
		defaultMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(dereferencedVariableName), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
		defaultMarshalInfoWriter.WriteMarshalCleanupVariable(writer, Emit.Dereference(variableName), metadataAccess, destinationVariable.Dereferenced.Load(writer.Context));
		destinationVariable.Dereferenced.WriteStore(writer, dereferencedVariableName);
	}

	public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		string storageVariable = unmarshaledVariableName + "_dereferenced";
		ElementTypeMarshalInfoWriter(writer.Context).WriteDeclareAndAllocateObject(writer, storageVariable, Emit.Dereference(marshaledVariableName), metadataAccess);
		writer.WriteLine($"{_typeRef.CppNameForVariable} {unmarshaledVariableName} = &{storageVariable};");
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		string emptyVariable = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
		writer.WriteVariable(writer.Context, _elementType, emptyVariable);
		return Emit.AddressOf(emptyVariable);
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		ElementTypeMarshalInfoWriter(writer.Context).WriteMarshalCleanupOutVariable(writer, Emit.Dereference(variableName), metadataAccess, (managedVariableName != null) ? Emit.Dereference(managedVariableName) : null);
	}

	public override string DecorateVariable(ReadOnlyContext context, string unmarshaledParameterName, string marshaledVariableName)
	{
		return ElementTypeMarshalInfoWriter(context).DecorateVariable(context, unmarshaledParameterName, marshaledVariableName);
	}

	public override string UndecorateVariable(ReadOnlyContext context, string variableName)
	{
		return ElementTypeMarshalInfoWriter(context).UndecorateVariable(context, variableName);
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return ElementTypeMarshalInfoWriter(context).CanMarshalTypeToNative(context);
	}

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return ElementTypeMarshalInfoWriter(context).CanMarshalTypeFromNative(context);
	}

	public override bool CanMarshalTypeFromNativeAsReturnValue(ReadOnlyContext context)
	{
		return false;
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return "il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal type '%s'.\", " + metadataAccess.Il2CppTypeFor(_typeRef) + ")";
	}
}
