using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.SourceWriters.Utils;

namespace Unity.IL2CPP.SourceWriters;

public class GenericMethodSourceWriter : SourceWriterBase<GenericInstanceMethod, GenericInstanceMethod>
{
	private const string BaseFileName = "GenericMethods";

	public override string Name => "WriteGenericMethodDefinition";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.Generics;

	private GenericMethodSourceWriter()
	{
	}

	public static void EnqueueWrites(SourceWritingContext context, ICollection<GenericInstanceMethod> items)
	{
		ScheduledStreamFactory.Create(context, "GenericMethods.cpp".ToNPath(), new GenericMethodSourceWriter()).Write(context.Global, items);
	}

	public override IEnumerable<GenericInstanceMethod> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<GenericInstanceMethod> items)
	{
		return items;
	}

	public override ReadOnlyCollection<ReadOnlyCollection<GenericInstanceMethod>> Chunk(ReadOnlyCollection<GenericInstanceMethod> items)
	{
		return items.ChunkByApproximateGeneratedCodeSize();
	}

	protected override string ProfilerSectionDetailsForItem(GenericInstanceMethod item)
	{
		return item.Name;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceMethod item, NPath filePath)
	{
		SourceWriter.WriteGenericMethodDefinition(context, writer, item);
	}

	protected override void WriteHeader(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteFooter(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteEnd(IGeneratedMethodCodeStream writer)
	{
		MethodWriter.WriteInlineMethodDefinitions(writer.Context, writer.FileName.FileNameWithoutExtension, writer);
	}
}
