using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class FixedArrayMarshalInfoWriter : ArrayMarshalInfoWriter
{
	public FixedArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo)
		: base(context, arrayType, marshalType, marshalInfo)
	{
	}

	public override void WriteFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
	{
		string fieldName = field.CppName + fieldNameSuffix;
		writer.WriteLine($"{_elementTypeMarshalInfoWriter.GetMarshaledTypes(writer.Context)[0].DecoratedName} {fieldName}[{_arraySize}];");
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({_arraySize} > ({sourceVariable.Load(writer.Context)})->max_length)");
			using (new BlockWriter(writer))
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_argument_exception(\"\", \"Type could not be marshaled because the length of an embedded array instance does not match the declared length in the layout.\")"));
			}
			writer.WriteLine();
			WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => _arraySize.ToString());
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		string arraySize = MarshaledArraySizeFor(writer.Context, variableName, methodParameters);
		AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, arraySize);
		WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySize);
	}

	public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => MarshaledArraySizeFor(writer.Context, variableName, methodParameters));
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		WriteCleanupLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => _arraySize.ToString());
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		WriteCleanupOutVariableLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => _arraySize.ToString());
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		base.WriteIncludesForFieldDeclaration(writer);
		writer.AddIncludeForTypeDefinition(writer.Context, _elementType);
	}
}
