using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;

namespace Unity.IL2CPP.Contexts.Scheduling;

public static class PhaseWorkSchedulerFactory
{
	public static IPhaseWorkScheduler<GlobalWriteContext> ForPrimaryWrite(AssemblyConversionContext context)
	{
		return ForPrimaryWrite(context.GlobalWriteContext, context.GlobalSchedulingContext);
	}

	public static IPhaseWorkScheduler<GlobalWriteContext> ForPrimaryWrite(GlobalWriteContext context, GlobalSchedulingContext schedulingContext)
	{
		return For(context, schedulingContext, PhaseWorkSchedulerFactoryUtils.ForkForPrimaryWrite, (GlobalWriteContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
	}

	public static IPhaseWorkScheduler<GlobalPrimaryCollectionContext> ForPrimaryCollection(AssemblyConversionContext context)
	{
		return ForPrimaryCollection(context.GlobalPrimaryCollectionContext, context.GlobalSchedulingContext);
	}

	public static IPhaseWorkScheduler<GlobalPrimaryCollectionContext> ForPrimaryCollection(GlobalPrimaryCollectionContext context, GlobalSchedulingContext schedulingContext)
	{
		return For(context, schedulingContext, PhaseWorkSchedulerFactoryUtils.ForkForPrimaryCollection, (GlobalPrimaryCollectionContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
	}

	public static IPhaseWorkScheduler<GlobalSecondaryCollectionContext> ForSecondaryCollection(AssemblyConversionContext context)
	{
		return ForSecondaryCollection(context.GlobalSecondaryCollectionContext, context.GlobalSchedulingContext);
	}

	public static IPhaseWorkScheduler<GlobalSecondaryCollectionContext> ForSecondaryCollection(GlobalSecondaryCollectionContext context, GlobalSchedulingContext schedulingContext)
	{
		return For(context, schedulingContext, PhaseWorkSchedulerFactoryUtils.ForkForSecondaryCollection, (GlobalSecondaryCollectionContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
	}

	public static IPhaseWorkScheduler<GlobalWriteContext> ForSecondaryWrite(AssemblyConversionContext context)
	{
		return ForSecondaryWrite(context.GlobalWriteContext, context.GlobalSchedulingContext);
	}

	public static IPhaseWorkScheduler<GlobalWriteContext> ForSecondaryWrite(GlobalWriteContext context, GlobalSchedulingContext schedulingContext)
	{
		return For(context, schedulingContext, PhaseWorkSchedulerFactoryUtils.ForkForSecondaryWrite, (GlobalWriteContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
	}

	private static IPhaseWorkScheduler<TContext> For<TContext>(TContext context, GlobalSchedulingContext schedulingContext, Func<TContext, OverrideObjects, int, ForkedContextScope<int, TContext>> forker, Func<TContext, Exception, Exception> workerItemExceptionHandler)
	{
		if (schedulingContext.Parameters.EnableSerialConversion)
		{
			return new PhaseWorkSchedulerNoThreading<TContext>(context, schedulingContext);
		}
		return CreateScheduler(context, schedulingContext, forker, workerItemExceptionHandler, allowContextForMainThread: false);
	}

	public static PhaseWorkScheduler<TContext> CreateScheduler<TContext>(TContext context, GlobalSchedulingContext schedulingContext, Func<TContext, OverrideObjects, int, ForkedContextScope<int, TContext>> forker, Func<TContext, Exception, Exception> workerItemExceptionHandler, bool allowContextForMainThread)
	{
		using (schedulingContext.Services.TinyProfiler.Section("Create Scheduler"))
		{
			RealSchedulerComponent workScheduler = new RealSchedulerComponent();
			OverrideObjects overrideObjects = new OverrideObjects(workScheduler);
			int jobCount = (schedulingContext.Parameters.EnableSerialConversion ? 1 : schedulingContext.InputData.JobCount);
			PhaseWorkScheduler<TContext> scheduler = new PhaseWorkScheduler<TContext>(schedulingContext, (int count) => forker(context, overrideObjects, count), jobCount, workerItemExceptionHandler, allowContextForMainThread);
			workScheduler.Initialize(scheduler);
			return scheduler;
		}
	}
}
