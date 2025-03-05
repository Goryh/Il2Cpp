using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public abstract class ScheduledTableWriterBaseSimple<TItem, TCodeWriter> : ScheduledTableWriterBase<TItem, TCodeWriter> where TCodeWriter : ICodeStream
{
	protected abstract void WriteDeclarations(SourceWritingContext context, TCodeWriter writer, ReadOnlyCollection<TItem> allItems);

	protected abstract void WriteItem(SourceWritingContext context, TCodeWriter writer, TItem item);

	protected abstract string FileName(ReadOnlyContext context);

	protected abstract TCodeWriter CreateFileStream(SourceWritingContext context, string fileName);

	protected void WriteTableEntry(SourceWritingContext context, TCodeWriter writer, TItem item)
	{
		WriteItem(context, writer, item);
		writer.WriteLine(",");
	}

	protected TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler, ReadOnlyCollection<TItem> items)
	{
		if (items.Count == 0)
		{
			return TableInfo.Empty;
		}
		Tag tag = new Tag(new TableInfo(items.Count, CodeTableType, CodeTableName(scheduler.SchedulingContext), ExternTable), items.Count, items);
		scheduler.Enqueue(scheduler.QueuingContext, items, Worker, tag);
		return tag.TableInfo;
	}

	private void Worker(WorkItemData<GlobalWriteContext, ReadOnlyCollection<TItem>, Tag> data)
	{
		SourceWritingContext sourceWritingContext = data.Context.CreateSourceWritingContext();
		using TCodeWriter writer = CreateFileStream(sourceWritingContext, FileName(sourceWritingContext));
		WriteFileContents(sourceWritingContext, writer, data.Item, data.Tag);
	}

	protected void WriteFileContents(SourceWritingContext context, TCodeWriter writer, ReadOnlyCollection<TItem> items, Tag tag)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Write Declarations"))
		{
			WriteDeclarations(context, writer, tag.AllItems);
		}
		using (tinyProfiler.Section("Write Table"))
		{
			ScheduledTableWriterBase<TItem, TCodeWriter>.WriteTableDeclaration(writer, tag);
			writer.BeginBlock();
			foreach (TItem item in items)
			{
				WriteTableEntry(context, writer, item);
			}
			writer.EndBlock(semicolon: true);
		}
	}
}
