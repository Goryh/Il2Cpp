using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

internal class WriteIl2CppGenericInstDefinitions : GeneratedCodeTableWriterBaseChunkedTransformed<KeyValuePair<IIl2CppRuntimeType[], uint>, string>
{
	protected override string TableName => "Il2CppGenericInstDefinitions";

	protected override string CodeTableType => "const Il2CppGenericInst* const";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.GenericInstanceTable.SortedItems, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppGenericInstTable");
	}

	protected override string Transform(ReadOnlyContext context, KeyValuePair<IIl2CppRuntimeType[], uint> item)
	{
		return context.Global.Services.Naming.ForGenericInst(context, item.Key);
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<Tuple<KeyValuePair<IIl2CppRuntimeType[], uint>, string>> allItems)
	{
		writer.AddCodeGenMetadataIncludes();
		foreach (Tuple<KeyValuePair<IIl2CppRuntimeType[], uint>, string> item in allItems.Select((Tuple<KeyValuePair<IIl2CppRuntimeType[], uint>, string> item) => item))
		{
			IIl2CppRuntimeType[] inst = item.Item1.Key;
			string instName = item.Item2;
			for (int j = 0; j < inst.Length; j++)
			{
				writer.WriteExternForIl2CppType(inst[j]);
			}
			IGeneratedCodeStream generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"static const Il2CppType* {instName + "_Types"}[] = {{ {inst.Select((IIl2CppRuntimeType t) => MetadataUtils.TypeRepositoryTypeFor(context, t)).AggregateWithComma(context)} }};");
			generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"extern const Il2CppGenericInst {instName};");
			generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"const Il2CppGenericInst {instName} = {{ {inst.Length}, {instName}_Types }};");
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, Tuple<KeyValuePair<IIl2CppRuntimeType[], uint>, string> item)
	{
		writer.Write($"&{item.Item2}");
	}
}
