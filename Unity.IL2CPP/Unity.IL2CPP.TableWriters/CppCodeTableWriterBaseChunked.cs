using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.TableWriters;

public abstract class CppCodeTableWriterBaseChunked<TItem> : ScheduledTableWriterBaseChunked<TItem, ICppCodeStream>
{
	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override ICppCodeStream CreateFileStream(SourceWritingContext context, string fileName)
	{
		return context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, fileName);
	}

	protected override void WriteOtherStream(ICppCodeStream fileWriter, ICppCodeStream otherWriter)
	{
		fileWriter.Write(otherWriter);
	}

	protected override ICppCodeStream CreateInMemoryWriter(SourceWritingContext context)
	{
		return new InMemoryCppCodeWriter(context);
	}
}
