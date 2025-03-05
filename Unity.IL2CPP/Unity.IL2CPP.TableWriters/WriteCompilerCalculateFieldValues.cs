using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

public class WriteCompilerCalculateFieldValues : ScheduledTableWriterBase<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>, ICppCodeStream>
{
	private class WritingData
	{
		public TypeDefinition Definition { get; }

		public MetadataTypeDefinitionInfo Metadata { get; }

		public TableInfo FieldTable { get; }

		public WritingData(TypeDefinition definition, MetadataTypeDefinitionInfo metadata, TableInfo fieldTable)
		{
			Definition = definition;
			Metadata = metadata;
			FieldTable = fieldTable;
		}
	}

	protected override string TableName => "Il2CppCCFieldValuesTable";

	protected override string CodeTableType => "IL2CPP_EXTERN_C_CONST int32_t*";

	protected override bool ExternTable => false;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryCollection.Metadata.GetTypeInfos());
	}

	private TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler, ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> items)
	{
		if (items.Count == 0)
		{
			return TableInfo.Empty;
		}
		Tag tag = new Tag(new TableInfo(items.Count, CodeTableType, CodeTableName(scheduler.SchedulingContext), ExternTable), items.Count, items);
		scheduler.Enqueue(scheduler.QueuingContext, items, PrepareTablesWorker, tag);
		return tag.TableInfo;
	}

	private void PrepareTablesWorker(WorkItemData<GlobalWriteContext, ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>>, Tag> data)
	{
		using (data.Context.Services.TinyProfiler.Section(TableName))
		{
			List<WritingData> fieldWritingData = data.Item.Select((KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> t) => new WritingData(t.Key, t.Value, new TableInfo(t.Key.Fields.Count, "IL2CPP_EXTERN_C const int32_t", data.Tag.TableInfo.Name + t.Value.Index, externTable: false))).ToList();
			List<(ReadOnlyCollection<WritingData>, int)> chunkedFiles = fieldWritingData.ChunkByItemsPer(3000).Select((ReadOnlyCollection<WritingData> item, int index) => (item: item, index: index)).ToList();
			data.Context.Services.Scheduler.EnqueueItems(data.Context, chunkedFiles, WriteChunkedFile, data.Tag);
			WriteFieldTable(data.Context.CreateSourceWritingContext(), fieldWritingData, data.Tag);
		}
	}

	private void WriteChunkedFile(WorkItemData<GlobalWriteContext, (ReadOnlyCollection<WritingData>, int), Tag> data)
	{
		SourceWritingContext context = data.Context.CreateSourceWritingContext();
		string fileNumberPostfix = ((data.Item.Item2 == 0) ? string.Empty : data.Item.Item2.ToString());
		using IGeneratedCodeStream writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppCCalculateFieldValues" + fileNumberPostfix + ".cpp");
		writer.WriteClangWarningDisables();
		foreach (WritingData item2 in data.Item.Item1.Where((WritingData item) => item.FieldTable.Count > 0))
		{
			writer.AddIncludeForTypeDefinition(context, item2.Definition);
			ScheduledTableWriterBase<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>, ICppCodeStream>.WriteTable(writer, item2.Definition.Fields, (FieldDefinition f) => OffsetOf(context, f), item2.FieldTable);
		}
	}

	private void WriteFieldTable(SourceWritingContext context, List<WritingData> fieldData, Tag tag)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppCCFieldValuesTable.cpp");
		foreach (WritingData varName in fieldData.Where((WritingData item) => item.FieldTable.Count > 0))
		{
			ICppCodeStream cppCodeStream = writer;
			cppCodeStream.WriteLine($"IL2CPP_EXTERN_C_CONST int32_t {varName.FieldTable.Name}[{varName.FieldTable.Count}];");
		}
		ScheduledTableWriterBase<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>, ICppCodeStream>.WriteTable(writer, fieldData, (WritingData d) => (d.FieldTable.Count <= 0) ? "NULL" : d.FieldTable.Name, tag.TableInfo);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_FieldOffsetTable");
	}

	private static string OffsetOf(ReadOnlyContext context, FieldDefinition field)
	{
		if (field.IsLiteral)
		{
			return "0";
		}
		if (field.DeclaringType.HasGenericParameters)
		{
			if (field.IsThreadStatic)
			{
				return "THREAD_STATIC_FIELD_OFFSET";
			}
			return "0";
		}
		if (field.IsThreadStatic)
		{
			return GetFieldOffset(context.Global.Services.Naming.ForThreadFieldsStruct(context, field.DeclaringType), field, " | THREAD_LOCAL_STATIC_MASK");
		}
		if (field.IsNormalStatic)
		{
			return GetFieldOffset(context.Global.Services.Naming.ForStaticFieldsStruct(context, field.DeclaringType), field, "");
		}
		if (field.DeclaringType.IsEnum)
		{
			return "static_cast<int32_t>(sizeof(" + context.Global.Services.Naming.ForType(context.Global.Services.TypeProvider.SystemObject) + "))";
		}
		return GetFieldOffset(field.DeclaringType.CppName, field, field.DeclaringType.IsValueType ? (" + static_cast<int32_t>(sizeof(" + context.Global.Services.Naming.ForType(context.Global.Services.TypeProvider.SystemObject) + "))") : "");
	}

	private static string GetFieldOffset(string declaringTypeName, FieldReference field, string suffix)
	{
		return $"static_cast<int32_t>(offsetof({declaringTypeName}, {field.CppName})){suffix}";
	}
}
