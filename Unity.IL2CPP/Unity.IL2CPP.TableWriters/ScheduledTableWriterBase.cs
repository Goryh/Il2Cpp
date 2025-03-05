using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public abstract class ScheduledTableWriterBase<TItem, TCodeWriter> where TCodeWriter : ICodeWriter
{
	protected class Tag
	{
		public readonly TableInfo TableInfo;

		public readonly int ItemCount;

		public readonly ReadOnlyCollection<TItem> AllItems;

		public Tag(TableInfo info, int itemCount, ReadOnlyCollection<TItem> allItems)
		{
			TableInfo = info;
			ItemCount = itemCount;
			AllItems = allItems;
		}
	}

	protected abstract string TableName { get; }

	protected abstract string CodeTableType { get; }

	protected abstract bool ExternTable { get; }

	public abstract TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler);

	protected abstract string CodeTableName(GlobalSchedulingContext context);

	protected static void WriteTableDeclaration(TCodeWriter writer, Tag tag)
	{
		WriteTableDeclaration(writer, tag.TableInfo);
	}

	protected static void WriteTableDeclaration(TCodeWriter writer, TableInfo tableInfo)
	{
		TCodeWriter val;
		if (tableInfo.ExternTable)
		{
			ref TCodeWriter reference = ref writer;
			val = default(TCodeWriter);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			reference.WriteLine(tableInfo.GetDeclaration());
		}
		ref TCodeWriter reference2;
		if (default(TCodeWriter) == null)
		{
			val = writer;
			reference2 = ref val;
		}
		else
		{
			reference2 = ref writer;
		}
		ref TCodeWriter reference3 = ref reference2;
		reference3.WriteLine($"{tableInfo.Type} {tableInfo.Name}[{((tableInfo.Count == 0) ? "1" : tableInfo.Count.ToString())}] = ");
	}

	protected static void WriteTable<TTableItem>(TCodeWriter writer, IEnumerable<TTableItem> items, Func<TTableItem, string> getTableValue, TableInfo tableInfo)
	{
		WriteTableDeclaration(writer, tableInfo);
		writer.BeginBlock();
		foreach (TTableItem data in items)
		{
			ref TCodeWriter reference = ref writer;
			TCodeWriter val = default(TCodeWriter);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			reference.Write(getTableValue(data));
			writer.Write(",");
		}
		writer.EndBlock(semicolon: true);
	}
}
