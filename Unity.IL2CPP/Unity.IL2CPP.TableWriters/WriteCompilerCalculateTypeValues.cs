using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

public class WriteCompilerCalculateTypeValues : ScheduledTableWriterBase<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>, ICppCodeStream>
{
	private class WritingData
	{
		public TypeDefinition Type { get; }

		public MetadataTypeDefinitionInfo Metadata { get; }

		public string SizeVarName { get; }

		public WritingData(TypeDefinition type, MetadataTypeDefinitionInfo metadata, string sizeVarName)
		{
			Type = type;
			Metadata = metadata;
			SizeVarName = sizeVarName;
		}
	}

	protected override string TableName => "Il2CppCCTypeValuesTable";

	protected override string CodeTableType => "IL2CPP_EXTERN_C_CONST Il2CppTypeDefinitionSizes*";

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
			string typeDefinitionSizeVarBaseName = data.Context.Services.ContextScope.ForMetadataGlobalVar("g_typeDefinitionSize");
			List<WritingData> writingData = data.Item.Select((KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> t) => new WritingData(t.Key, t.Value, $"{typeDefinitionSizeVarBaseName}{t.Value.Index}")).ToList();
			List<(ReadOnlyCollection<WritingData>, int)> chunkedFiles = writingData.ChunkByItemsPer(5400).Select((ReadOnlyCollection<WritingData> item, int index) => (item: item, index: index)).ToList();
			data.Context.Services.Scheduler.EnqueueItems(data.Context, chunkedFiles, WriteChunkedFile, data.Tag);
			WriteTypeTable(data.Context.CreateSourceWritingContext(), writingData, data.Tag);
		}
	}

	private void WriteChunkedFile(WorkItemData<GlobalWriteContext, (ReadOnlyCollection<WritingData>, int), Tag> data)
	{
		SourceWritingContext context = data.Context.CreateSourceWritingContext();
		string fileNumberPostfix = ((data.Item.Item2 == 0) ? string.Empty : data.Item.Item2.ToString());
		using IGeneratedCodeStream writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppCCalculateTypeValues" + fileNumberPostfix + ".cpp");
		writer.WriteClangWarningDisables();
		foreach (WritingData item in data.Item.Item1)
		{
			TypeDefinition type = item.Type;
			writer.AddIncludeForTypeDefinition(context, type);
			if (type.IsEnum)
			{
				writer.AddIncludeForTypeDefinition(context, type.GetUnderlyingEnumType().Resolve());
			}
			IGeneratedCodeStream generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"extern const Il2CppTypeDefinitionSizes {item.SizeVarName};");
			generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"const Il2CppTypeDefinitionSizes {item.SizeVarName} = {{ {Sizes(context, type)} }};");
		}
		writer.WriteClangWarningEnables();
	}

	private void WriteTypeTable(SourceWritingContext context, List<WritingData> items, Tag tag)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppCCTypeValuesTable.cpp");
		foreach (WritingData item in items)
		{
			ICppCodeStream cppCodeStream = writer;
			cppCodeStream.WriteLine($"extern const Il2CppTypeDefinitionSizes {item.SizeVarName};");
		}
		ScheduledTableWriterBase<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>, ICppCodeStream>.WriteTable(writer, items, (WritingData d) => Emit.AddressOf(d.SizeVarName), tag.TableInfo);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppTypeDefinitionSizesTable");
	}

	private static string Sizes(MinimalContext context, TypeDefinition type)
	{
		bool hasCompileTimeSize = !type.HasGenericParameters || type.ClassSize > 0;
		return $"{InstanceSizeFor(type, hasCompileTimeSize)}, {NativeSizeFor(context, type, hasCompileTimeSize)}, {((!type.HasGenericParameters && (type.Fields.Any((FieldDefinition f) => f.IsNormalStatic) || type.StoresNonFieldsInStaticFields())) ? ("sizeof(" + context.Global.Services.Naming.ForStaticFieldsStruct(context, type) + ")") : "0")}, {((!type.HasGenericParameters && type.Fields.Any((FieldDefinition f) => f.IsThreadStatic)) ? ("sizeof(" + context.Global.Services.Naming.ForThreadFieldsStruct(context, type) + ")") : "0")}";
	}

	private static string InstanceSizeFor(TypeDefinition type, bool hasCompileTimeSize)
	{
		if (type.IsInterface || !hasCompileTimeSize)
		{
			return "0";
		}
		if (type.HasGenericParameters)
		{
			return $"{type.ClassSize} + sizeof(RuntimeObject)";
		}
		if (type.IsEnum)
		{
			type = type.GetUnderlyingEnumType().Resolve();
		}
		string name = (type.IsIntegralType ? type.CppNameForVariable : type.CppName);
		return "sizeof(" + name + ")" + (type.IsValueType ? "+ sizeof(RuntimeObject)" : string.Empty);
	}

	private static string NativeSizeFor(MinimalContext context, TypeDefinition type, bool hasCompileTimeSize)
	{
		if (!hasCompileTimeSize)
		{
			return "0";
		}
		if (type.HasGenericParameters)
		{
			return $"{type.ClassSize}";
		}
		return MarshalDataCollector.MarshalInfoWriterFor(context, type, MarshalType.PInvoke, null, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(type)).GetNativeSize(context);
	}
}
