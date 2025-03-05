using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class LPArrayMarshalInfoWriter : ArrayMarshalInfoWriter
{
	public LPArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo)
		: base(context, arrayType, marshalType, marshalInfo)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string arraySizeVariable = WriteArraySizeFromManagedArray(writer, sourceVariable, destinationVariable);
			AllocateAndStoreNativeArray(writer, destinationVariable, arraySizeVariable);
			WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySizeVariable);
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			WriteAssignNullArray(writer, destinationVariable);
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({destinationVariable.Load(writer.Context)} == {"NULL"})");
			using (new BlockWriter(writer))
			{
				AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, MarshaledArraySizeFor(writer.Context, variableName, methodParameters));
			}
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, delegate
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter2 = writer;
				generatedMethodCodeWriter2.WriteLine($"{"il2cpp_array_size_t"} {"_arrayLength"} = ({destinationVariable.Load(writer.Context)})->max_length;");
				return "_arrayLength";
			});
		}
	}

	public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, destinationVariable, variableName));
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteCleanupLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteLoopCountVariable(bodyWriter, variableName, managedVariableName));
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_marshal_free({variableName});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteCleanupOutVariableLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteLoopCountVariable(bodyWriter, variableName, managedVariableName));
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_marshal_free({variableName});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}

	private string WriteLoopCountVariable(IGeneratedCodeWriter bodyWriter, string variableName, string managedVariableName)
	{
		string loopCountVariableName = CleanVariableName(bodyWriter.Context, variableName) + "_CleanupLoopCount";
		string loopCount = ((managedVariableName == null) ? MarshaledArraySizeFor(bodyWriter.Context, variableName, null) : string.Format("({0} != {1}) ? ({0})->max_length : 0", managedVariableName, "NULL"));
		bodyWriter.WriteLine($"const {"il2cpp_array_size_t"} {loopCountVariableName} = {loopCount};");
		return loopCountVariableName;
	}
}
