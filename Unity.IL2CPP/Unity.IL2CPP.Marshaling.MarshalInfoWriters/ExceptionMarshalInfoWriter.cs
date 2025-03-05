using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class ExceptionMarshalInfoWriter : DefaultMarshalInfoWriter
{
	private readonly TypeReference _int32Type;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(_int32Type.CppNameForVariable)
		};
	}

	public ExceptionMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
		: base(type)
	{
		_int32Type = context.Global.Services.TypeProvider.Int32TypeReference;
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, writer.Context.Global.Services.TypeProvider.SystemException);
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		return "0";
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{destinationVariable} = {WriteMarshalVariableToNative(writer, sourceVariable, managedVariableName, metadataAccess)};");
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		return $"({sourceVariable.Load(writer.Context)} != {"NULL"} ? reinterpret_cast<RuntimeException*>({sourceVariable.Load(writer.Context)})->hresult : IL2CPP_S_OK)";
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		return "NULL";
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess));
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		return $"({variableName} != IL2CPP_S_OK ? reinterpret_cast<{writer.Context.Global.Services.TypeProvider.SystemException.CppNameForVariable}>(il2cpp_codegen_com_get_exception({variableName}, false)) : {"NULL"})";
	}
}
