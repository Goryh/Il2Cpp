using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class PinnedArrayMarshalInfoWriter : ArrayMarshalInfoWriter
{
	public PinnedArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset = false)
		: base(context, arrayType, marshalType, marshalInfo, useUnicodeCharset)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			if (_arraySizeSelection == ArraySizeOptions.UseFirstMarshaledType)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{destinationVariable}{GetMarshaledTypes(writer.Context)[0].VariableName} = static_cast<uint32_t>(({sourceVariable.Load(writer.Context)})->max_length);");
			}
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = reinterpret_cast<{_arrayMarshaledTypeName}>(({sourceVariable.Load(writer.Context)})->{ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck: false)}(0));");
		}
	}

	public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, sourceVariable, destinationVariable));
		}
	}

	public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
	{
		string marshaledVariableName = "_" + sourceVariable.GetNiceName(writer.Context) + "_marshaled";
		WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		writer.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string arraySizeVariable = WriteArraySizeFromManagedArray(writer, sourceVariable, marshaledVariableName);
			AllocateAndStoreNativeArray(writer, marshaledVariableName, arraySizeVariable);
			WriteMarshalToNativeLoop(writer, sourceVariable, marshaledVariableName, null, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySizeVariable);
			return marshaledVariableName;
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string arraySize = MarshaledArraySizeFor(writer.Context, variableName, methodParameters);
			AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, arraySize);
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySize);
		}
	}

	public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		if (isIn)
		{
			return;
		}
		writer.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, destinationVariable, variableName));
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_marshal_free({variableName});");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
	}
}
