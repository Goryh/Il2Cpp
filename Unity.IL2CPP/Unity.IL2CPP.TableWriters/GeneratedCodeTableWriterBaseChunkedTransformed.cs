using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.TableWriters;

public abstract class GeneratedCodeTableWriterBaseChunkedTransformed<TItem, TItem2> : ScheduledTableWriterBaseChunkedTransform<TItem, TItem2, IGeneratedCodeStream>
{
	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".cpp";
	}

	protected override IGeneratedCodeStream CreateFileStream(SourceWritingContext context, string fileName)
	{
		return context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, fileName);
	}
}
