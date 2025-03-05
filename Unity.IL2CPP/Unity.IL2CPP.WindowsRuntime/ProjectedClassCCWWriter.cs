using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.WindowsRuntime;

internal class ProjectedClassCCWWriter : ICCWWriter
{
	private readonly TypeReference _typeRef;

	public ProjectedClassCCWWriter(TypeReference type)
	{
		_typeRef = type;
	}

	public void Write(IGeneratedMethodCodeWriter writer)
	{
	}

	public void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		DefaultMarshalInfoWriter marshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeRef, MarshalType.WindowsRuntime);
		marshalInfoWriter.WriteIncludesForMarshaling(writer);
		if (marshalInfoWriter.CanMarshalTypeToNative(writer.Context))
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_typeRef.CppNameForVariable} {"_this"} = reinterpret_cast<{_typeRef.CppNameForVariable}>(obj);");
			string nativeInstance = marshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue("_this"), "_this", metadataAccess);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"return {nativeInstance};");
		}
		else
		{
			writer.WriteStatement(Emit.RaiseManagedException(marshalInfoWriter.GetMarshalingException(writer.Context, metadataAccess)));
		}
	}
}
