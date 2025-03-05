using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public abstract class ScheduledTableWriterBaseChunkedTransform<TItem, TItem2, TCodeWriter> : ScheduledTableWriterBase<Tuple<TItem, TItem2>, TCodeWriter> where TCodeWriter : ICodeStream
{
	protected TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler, ReadOnlyCollection<TItem> items, int numberOfChunks)
	{
		if (items.Count == 0)
		{
			return TableInfo.Empty;
		}
		Tag tag = new Tag(new TableInfo(items.Count, CodeTableType, CodeTableName(scheduler.SchedulingContext), ExternTable), items.Count, null);
		scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, items.Chunk(numberOfChunks), ChunkWorker, PostProcessWorker, tag);
		return tag.TableInfo;
	}

	protected abstract TItem2 Transform(ReadOnlyContext context, TItem item);

	protected abstract void WriteDeclarations(SourceWritingContext context, TCodeWriter writer, ReadOnlyCollection<Tuple<TItem, TItem2>> allItems);

	protected abstract void WriteItem(SourceWritingContext context, TCodeWriter writer, Tuple<TItem, TItem2> item);

	protected abstract string FileName(ReadOnlyContext context);

	protected abstract TCodeWriter CreateFileStream(SourceWritingContext context, string fileName);

	private ReadOnlyCollection<Tuple<TItem, TItem2>> ChunkWorker(WorkItemData<GlobalWriteContext, ReadOnlyCollection<TItem>, Tag> data)
	{
		ReadOnlyContext context = data.Context.GetReadOnlyContext();
		List<Tuple<TItem, TItem2>> result = new List<Tuple<TItem, TItem2>>();
		using (data.Context.Services.TinyProfiler.Section(TableName, "Transform"))
		{
			foreach (TItem item in data.Item)
			{
				result.Add(new Tuple<TItem, TItem2>(item, Transform(context, item)));
			}
		}
		return result.AsReadOnly();
	}

	private void PostProcessWorker(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<ReadOnlyCollection<TItem>, ReadOnlyCollection<Tuple<TItem, TItem2>>>>, Tag> data)
	{
		SourceWritingContext sourceWritingContext = data.Context.CreateSourceWritingContext();
		ITinyProfilerService tinyProfiler = sourceWritingContext.Global.Services.TinyProfiler;
		using TCodeWriter writer = CreateFileStream(sourceWritingContext, FileName(sourceWritingContext));
		ReadOnlyCollection<Tuple<TItem, TItem2>> orderedItems = data.Item.SelectMany((ResultData<ReadOnlyCollection<TItem>, ReadOnlyCollection<Tuple<TItem, TItem2>>> d) => d.Result).ToList().AsReadOnly();
		using (tinyProfiler.Section("Write Declarations"))
		{
			WriteDeclarations(sourceWritingContext, writer, orderedItems);
		}
		using (tinyProfiler.Section("Write Table"))
		{
			ScheduledTableWriterBase<Tuple<TItem, TItem2>, TCodeWriter>.WriteTableDeclaration(writer, data.Tag);
			writer.BeginBlock();
			foreach (Tuple<TItem, TItem2> item in orderedItems)
			{
				WriteItem(sourceWritingContext, writer, item);
				writer.WriteLine(",");
			}
			writer.EndBlock(semicolon: true);
		}
	}
}
