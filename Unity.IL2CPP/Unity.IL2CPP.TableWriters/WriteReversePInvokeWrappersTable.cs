using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

public class WriteReversePInvokeWrappersTable : GeneratedCodeTableWriterBaseSimple<KeyValuePair<MethodReference, uint>>
{
	protected override string TableName => "Il2CppReversePInvokeWrapperTable";

	protected override string CodeTableType => "const Il2CppMethodPointer";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.ReversePInvokeWrappers.SortedItems);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_ReversePInvokeWrapperPointers");
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> allItems)
	{
		foreach (KeyValuePair<MethodReference, uint> method in allItems)
		{
			ReversePInvokeMethodBodyWriter.Create(context, method.Key).WriteMethodDeclaration(writer);
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, KeyValuePair<MethodReference, uint> item)
	{
		if (item.Key.IsUnmanagedCallersOnly)
		{
			IGeneratedCodeStream generatedCodeStream = writer;
			generatedCodeStream.Write($"reinterpret_cast<Il2CppMethodPointer>({item.Key.CppName})");
		}
		else
		{
			IGeneratedCodeStream generatedCodeStream = writer;
			generatedCodeStream.Write($"reinterpret_cast<Il2CppMethodPointer>({context.Global.Services.Naming.ForReversePInvokeWrapperMethod(context, item.Key)})");
		}
	}
}
