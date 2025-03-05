using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

internal class WriteIl2CppGenericMethodDefinitions : CppCodeTableWriterBaseChunked<KeyValuePair<Il2CppMethodSpec, uint>>
{
	private readonly ReadOnlyGenericInstanceTable _genericInstanceTable;

	private readonly IMetadataCollectionResults _metadataCollection;

	protected override string TableName => "Il2CppGenericMethodDefinitions";

	protected override string CodeTableType => "const Il2CppMethodSpec";

	protected override bool ExternTable => true;

	public WriteIl2CppGenericMethodDefinitions(ReadOnlyGenericInstanceTable genericInstanceTable, IMetadataCollectionResults metadataCollection)
	{
		_genericInstanceTable = genericInstanceTable;
		_metadataCollection = metadataCollection;
	}

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.SortedItems, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppMethodSpecTable");
	}

	protected override void WriteDeclarations(SourceWritingContext context, ICppCodeStream writer, ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, uint>> allItems)
	{
	}

	protected override void WriteItem(SourceWritingContext context, ICppCodeStream writer, KeyValuePair<Il2CppMethodSpec, uint> item)
	{
		Il2CppMethodSpec methodSpec = item.Key;
		writer.Write($"{{ {_metadataCollection.GetMethodIndex(methodSpec.GenericMethod.Resolve())}, {((methodSpec.TypeGenericInstanceData != null) ? ((int)_genericInstanceTable.Table[methodSpec.TypeGenericInstanceData]) : (-1))}, {((methodSpec.MethodGenericInstanceData != null) ? ((int)_genericInstanceTable.Table[methodSpec.MethodGenericInstanceData]) : (-1))} }}");
	}
}
