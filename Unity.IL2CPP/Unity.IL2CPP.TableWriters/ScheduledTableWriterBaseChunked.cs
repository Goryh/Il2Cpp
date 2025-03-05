using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public abstract class ScheduledTableWriterBaseChunked<TItem, TCodeWriter> : ScheduledTableWriterBase<TItem, TCodeWriter> where TCodeWriter : ICodeStream
{
	protected abstract TCodeWriter CreateInMemoryWriter(SourceWritingContext context);

	protected abstract void WriteOtherStream(TCodeWriter fileWriter, TCodeWriter otherWriter);

	protected abstract void WriteDeclarations(SourceWritingContext context, TCodeWriter writer, ReadOnlyCollection<TItem> allItems);

	protected abstract void WriteItem(SourceWritingContext context, TCodeWriter writer, TItem item);

	protected abstract string FileName(ReadOnlyContext context);

	protected abstract TCodeWriter CreateFileStream(SourceWritingContext context, string fileName);

	protected void WriteTableEntry(SourceWritingContext context, TCodeWriter writer, TItem item)
	{
		WriteItem(context, writer, item);
		writer.WriteLine(",");
	}

	protected TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler, ReadOnlyCollection<TItem> items, int numberOfChunks)
	{
		if (items.Count == 0)
		{
			return TableInfo.Empty;
		}
		Tag tag = new Tag(new TableInfo(items.Count, CodeTableType, CodeTableName(scheduler.SchedulingContext), ExternTable), items.Count, items);
		scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, new(int, ReadOnlyCollection<TItem>)[1] { (-1, null) }.Concat(items.ChunkWithNumber(numberOfChunks)).ToList().AsReadOnly(), ChunkWorker, PostProcessWorker, tag);
		return tag.TableInfo;
	}

	private TCodeWriter ChunkWorker(WorkItemData<GlobalWriteContext, (int, ReadOnlyCollection<TItem>), Tag> data)
	{
		SourceWritingContext sourceWritingContext = data.Context.CreateSourceWritingContext();
		ITinyProfilerService tinyProfiler = sourceWritingContext.Global.Services.TinyProfiler;
		TCodeWriter writer = CreateInMemoryWriter(sourceWritingContext);
		if (data.Item.Item1 == -1)
		{
			using (tinyProfiler.Section(TableName, "Write Declarations"))
			{
				WriteDeclarations(sourceWritingContext, writer, data.Tag.AllItems);
			}
		}
		else
		{
			using (tinyProfiler.Section(TableName, "Write Chunk"))
			{
				foreach (TItem item in data.Item.Item2)
				{
					WriteTableEntry(sourceWritingContext, writer, item);
				}
			}
		}
		return writer;
	}

	private void PostProcessWorker(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<(int, ReadOnlyCollection<TItem>), TCodeWriter>>, Tag> data)
	{
		SourceWritingContext sourceWritingContext = data.Context.CreateSourceWritingContext();
		ITinyProfilerService tinyProfiler = sourceWritingContext.Global.Services.TinyProfiler;
		using TCodeWriter writer = CreateFileStream(sourceWritingContext, FileName(sourceWritingContext));
		using (tinyProfiler.Section("Flush Declarations"))
		{
			WriteOtherStream(writer, data.Item[0].Result);
		}
		using (tinyProfiler.Section("Write Table"))
		{
			ScheduledTableWriterBase<TItem, TCodeWriter>.WriteTableDeclaration(writer, data.Tag);
			writer.BeginBlock();
			using (tinyProfiler.Section("Flush Chunks"))
			{
				foreach (ResultData<(int, ReadOnlyCollection<TItem>), TCodeWriter> chunk in data.Item)
				{
					if (chunk.Item.Item1 != -1)
					{
						WriteOtherStream(writer, chunk.Result);
					}
				}
			}
			writer.EndBlock(semicolon: true);
		}
	}
}
