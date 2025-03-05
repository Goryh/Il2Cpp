using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.TableWriters;

public abstract class GeneratedCodeTableWriterBaseChunked<TItem> : ScheduledTableWriterBaseChunked<TItem, IGeneratedCodeStream>
{
	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".cpp";
	}

	protected override IGeneratedCodeStream CreateFileStream(SourceWritingContext context, string fileName)
	{
		return context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, fileName);
	}

	protected override void WriteOtherStream(IGeneratedCodeStream fileWriter, IGeneratedCodeStream otherWriter)
	{
		fileWriter.Write(otherWriter);
	}

	protected override IGeneratedCodeStream CreateInMemoryWriter(SourceWritingContext context)
	{
		return new InMemoryCodeWriter(context);
	}
}
