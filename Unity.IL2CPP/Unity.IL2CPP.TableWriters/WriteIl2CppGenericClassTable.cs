using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

internal class WriteIl2CppGenericClassTable : GeneratedCodeTableWriterBaseChunked<IIl2CppRuntimeType>
{
	protected override string TableName => "Il2CppGenericClassTable";

	protected override string CodeTableType => "Il2CppGenericClass* const";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.Types.SortedItems.Where((IIl2CppRuntimeType t) => t.Type.IsGenericInstance).ToList().AsReadOnly(), scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppGenericTypes");
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<IIl2CppRuntimeType> allItems)
	{
		writer.AddCodeGenMetadataIncludes();
		foreach (IIl2CppRuntimeType generic in allItems)
		{
			writer.WriteExternForGenericClass(generic.Type);
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, IIl2CppRuntimeType item)
	{
		writer.Write("&" + context.Global.Services.Naming.ForGenericClass(context, item.Type));
	}
}
