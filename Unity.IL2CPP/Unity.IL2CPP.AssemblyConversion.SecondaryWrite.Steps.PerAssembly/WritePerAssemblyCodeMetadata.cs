using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.TableWriters;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;

public class WritePerAssemblyCodeMetadata : PerAssemblyScheduledStepFuncWithGlobalPostProcessing<GlobalWriteContext, AssemblyCodeMetadata>
{
	public class Tables
	{
		public readonly TableInfo InvokerTable;

		public readonly TableInfo ReversePInvokeWrappersTable;

		public readonly TableInfo GenericMethodPointerTable;

		public readonly TableInfo GenericAdjustorThunkTable;

		public readonly TableInfo InteropDataTable;

		public readonly TableInfo WindowsRuntimeFactoryTable;

		public Tables(TableInfo invokerTable, TableInfo reversePInvokeWrappersTable, TableInfo genericMethodPointerTable, TableInfo genericAdjustorThunkTable, TableInfo interopDataTable, TableInfo windowsRuntimeFactoryTable)
		{
			InvokerTable = invokerTable;
			ReversePInvokeWrappersTable = reversePInvokeWrappersTable;
			GenericMethodPointerTable = genericMethodPointerTable;
			GenericAdjustorThunkTable = genericAdjustorThunkTable;
			InteropDataTable = interopDataTable;
			WindowsRuntimeFactoryTable = windowsRuntimeFactoryTable;
		}
	}

	protected override string Name => "Write Assembly Code Metadata";

	protected override string PostProcessingSectionName => "Write Global Code Metadata";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public override void Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				TableInfo invokerTable = new WriteInvokerTable().Schedule(scheduler);
				TableInfo reversePInvokeWrappersTable = new WriteReversePInvokeWrappersTable().Schedule(scheduler);
				TableInfo genericMethodPointerTable = new WriteMethodPointerTable().Schedule(scheduler);
				TableInfo genericAdjustorThunkTable = new WriteGenericAdjustorThunkTable().Schedule(scheduler);
				TableInfo interopDataTable = new WriteInteropDataTable().Schedule(scheduler);
				TableInfo windowsRuntimeFactoryTable;
				using (scheduler.SchedulingContext.Services.TinyProfiler.Section("Schedule Windows RUntime Tables"))
				{
					windowsRuntimeFactoryTable = new WriteWindowsRuntimeFactoryTable().Schedule(scheduler);
				}
				Tables tables = new Tables(invokerTable, reversePInvokeWrappersTable, genericMethodPointerTable, genericAdjustorThunkTable, interopDataTable, windowsRuntimeFactoryTable);
				scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, items, WorkerWrapper, PostProcessWrapper, tables);
			}
		}
	}

	private void PostProcessWrapper(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<AssemblyDefinition, AssemblyCodeMetadata>>, Tables> workerData)
	{
		using (workerData.Context.Services.TinyProfiler.Section(PostProcessingSectionName))
		{
			PostProcess(workerData.Context, workerData.Item, workerData.Tag);
		}
	}

	private AssemblyCodeMetadata WorkerWrapper(WorkItemData<GlobalWriteContext, AssemblyDefinition, Tables> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			return ProcessItem(workerData.Context, workerData.Item);
		}
	}

	protected override AssemblyCodeMetadata ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
	{
		return PerAssemblyCodeMetadataWriter.Write(context.CreateSourceWritingContext(), item, context.Results.PrimaryWrite.GenericContextCollections[item], null, null);
	}

	protected override void PostProcess(GlobalWriteContext context, ReadOnlyCollection<ResultData<AssemblyDefinition, AssemblyCodeMetadata>> data)
	{
		throw new NotSupportedException();
	}

	protected void PostProcess(GlobalWriteContext context, ReadOnlyCollection<ResultData<AssemblyDefinition, AssemblyCodeMetadata>> data, Tables tables)
	{
		SourceWritingContext sourceWritingContext = context.CreateSourceWritingContext();
		CodeRegistrationWriter.WriteCodeRegistration(sourceWritingContext, sourceWritingContext.Global.Results.SecondaryWritePart3.UnresolvedIndirectCallsTableInfo, data.Select((ResultData<AssemblyDefinition, AssemblyCodeMetadata> d) => d.Result).ToList().AsReadOnly(), tables);
	}
}
