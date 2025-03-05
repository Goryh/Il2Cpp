using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Forking;

public static class ContextForker
{
	public static ForkedContextScope<AssemblyDefinition, GlobalWriteContext> ForPrimaryWrite(AssemblyConversionContext context)
	{
		return ForPrimaryWrite(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
	}

	public static ForkedContextScope<AssemblyDefinition, GlobalWriteContext> ForPrimaryWrite(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		return CreateForPrimaryWrite(context.ContextDataProvider, assemblies);
	}

	public static ForkedContextScope<TItem, GlobalWriteContext> ForPrimaryWrite<TItem>(AssemblyConversionContext context, TItem[] items)
	{
		return ForPrimaryWrite(context.ContextDataProvider, items);
	}

	public static ForkedContextScope<TItem, GlobalPrimaryCollectionContext> ForPrimaryCollection<TItem>(AssemblyConversionContext context, TItem[] items)
	{
		return ForPrimaryCollection(context.ContextDataProvider, items);
	}

	public static ForkedContextScope<TItem, GlobalWriteContext> ForPrimaryWrite<TItem>(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items)
	{
		return CreateForPrimaryWrite(context, items);
	}

	public static ForkedContextScope<int, GlobalWriteContext> ForPrimaryWrite(IUnrestrictedContextDataProvider context, int count)
	{
		return ForPrimaryWrite(context, Enumerable.Range(0, count).ToArray());
	}

	public static ForkedContextScope<TItem, GlobalWriteContext> ForPrimaryWrite<TItem>(IUnrestrictedContextDataProvider context, TItem[] items)
	{
		return CreateForPrimaryWrite(context, items.AsReadOnly());
	}

	public static ForkedContextScope<TItem, GlobalPrimaryCollectionContext> ForPrimaryCollection<TItem>(IUnrestrictedContextDataProvider context, TItem[] items)
	{
		return CreateForPrimaryCollection(context, items.AsReadOnly());
	}

	private static ForkedContextScope<TItem, GlobalWriteContext> CreateForPrimaryWrite<TItem>(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items)
	{
		return new ForkedContextScope<TItem, GlobalWriteContext>(context, items, (IUnrestrictedContextDataProvider c) => new PrimaryWriteAssembliesDataForker(c));
	}

	private static ForkedContextScope<TItem, GlobalPrimaryCollectionContext> CreateForPrimaryCollection<TItem>(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items)
	{
		return new ForkedContextScope<TItem, GlobalPrimaryCollectionContext>(context, items, (IUnrestrictedContextDataProvider c) => new PrimaryCollectionDataForker(c));
	}

	public static ForkedContextScope<TItem, GlobalFullyForkedContext> ForPartialPerAssembly<TItem>(AssemblyConversionContext context, ReadOnlyCollection<TItem> items, ReadOnlyCollection<OverrideObjects> overrideObjects, IPhaseResultsSetter<GlobalFullyForkedContext> phaseResultsSetter)
	{
		return ForPartialPerAssembly(context.ContextDataProvider, items, overrideObjects, phaseResultsSetter);
	}

	public static ForkedContextScope<TItem, GlobalFullyForkedContext> ForPartialPerAssembly<TItem>(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, ReadOnlyCollection<OverrideObjects> overrideObjects, IPhaseResultsSetter<GlobalFullyForkedContext> phaseResultsSetter)
	{
		return new ForkedContextScope<TItem, GlobalFullyForkedContext>(context, items, (IUnrestrictedContextDataProvider c) => new PartialPerAssemblyDataForker(c), overrideObjects, useParallelMerge: true, ForkCreationMode.Upfront, phaseResultsSetter);
	}

	public static ForkedContextScope<TItem, GlobalFullyForkedContext> ForFullPerAssembly<TItem>(AssemblyConversionContext context, ReadOnlyCollection<TItem> items, ReadOnlyCollection<OverrideObjects> overrideObjects, IPhaseResultsSetter<GlobalFullyForkedContext> phaseResultsSetter)
	{
		return ForFullPerAssembly(context.ContextDataProvider, items, overrideObjects, phaseResultsSetter);
	}

	public static ForkedContextScope<TItem, GlobalFullyForkedContext> ForFullPerAssembly<TItem>(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, ReadOnlyCollection<OverrideObjects> overrideObjects, IPhaseResultsSetter<GlobalFullyForkedContext> phaseResultsSetter)
	{
		return new ForkedContextScope<TItem, GlobalFullyForkedContext>(context, items, (IUnrestrictedContextDataProvider c) => new FullPerAssemblyDataForker(c), overrideObjects, useParallelMerge: true, ForkCreationMode.Upfront, phaseResultsSetter);
	}
}
