using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public class WriteMethodPointerTable : GeneratedCodeTableWriterBaseChunkedTransformed<CollectMethodTables.GenericMethodPointerTableEntry, string>
{
	protected override string TableName => "Il2CppGenericMethodPointerTable";

	protected override string CodeTableType => "const Il2CppMethodPointer";

	protected override bool ExternTable => true;

	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppGenericMethodPointers");
	}

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.MethodTables.SortedGenericMethodPointerTableValues, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string Transform(ReadOnlyContext context, CollectMethodTables.GenericMethodPointerTableEntry item)
	{
		return item.Name();
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<Tuple<CollectMethodTables.GenericMethodPointerTableEntry, string>> allItems)
	{
		foreach (Tuple<CollectMethodTables.GenericMethodPointerTableEntry, string> method in allItems)
		{
			if (!method.Item1.IsNull)
			{
				writer.WriteLine($"IL2CPP_EXTERN_C void {method.Item2} ();");
			}
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, Tuple<CollectMethodTables.GenericMethodPointerTableEntry, string> item)
	{
		string name = (item.Item1.IsNull ? "NULL" : ("(Il2CppMethodPointer)&" + item.Item2));
		writer.Write((!context.Global.Parameters.EmitComments) ? name : $"{name}/* {item.Item1.Index}*/");
	}
}
