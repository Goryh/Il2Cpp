using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class HandleRefMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly TypeDefinition _typeDefinition;

	private readonly bool _forByReferenceType;

	private readonly MarshaledType[] _marshaledTypes;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public HandleRefMarshalInfoWriter(TypeReference type, bool forByReferenceType)
		: base(type)
	{
		_typeDefinition = type.Resolve();
		_forByReferenceType = forByReferenceType;
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType("void*", "void*")
		};
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (!CanMarshalTypeToNative(writer.Context))
		{
			throw new InvalidOperationException("Cannot marshal HandleRef by reference to native code.");
		}
		FieldDefinition handleField = _typeDefinition.Fields.SingleOrDefault((FieldDefinition f) => f.Name == "_handle");
		if (handleField == null)
		{
			throw new InvalidOperationException($"Unable to locate the handle field on {_typeDefinition}");
		}
		writer.WriteLine($"{destinationVariable} = (void*){sourceVariable.Load(writer.Context)}.{handleField.CppName};");
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		throw new InvalidOperationException("Cannot marshal HandleRef from native code");
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		throw new InvalidOperationException("Cannot marshal HandleRef from native code");
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return !_forByReferenceType;
	}

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return false;
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return string.Format("il2cpp_codegen_get_marshal_directive_exception(\"HandleRefs cannot be marshaled ByRef or from unmanaged to managed.\")", _typeRef);
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}
}
