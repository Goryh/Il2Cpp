using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class CollectInvokers : PerAssemblyAndGenericsScheduledStepFuncWithGlobalPostProcessingFunc<GlobalSecondaryCollectionContext, ReadOnlyCollection<Il2CppMethodSpec>, InvokerCollection, ReadOnlyInvokerCollection>
{
	protected override string Name => "Collect Invokers";

	protected override string PostProcessingSectionName => "Merge Invokers";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override InvokerCollection ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item)
	{
		InvokerCollection collector = new InvokerCollection();
		SecondaryCollectionContext collectionContext = context.CreateCollectionContext();
		ReadOnlyContext readonlyContext = collectionContext.AsReadonly();
		foreach (TypeDefinition allType in item.GetAllTypes())
		{
			foreach (MethodDefinition method in allType.Methods)
			{
				if (ShouldCollectInvoker(readonlyContext, method))
				{
					collector.Add(collectionContext, method);
				}
			}
		}
		return collector;
	}

	public static bool ShouldCollectInvoker(ReadOnlyContext context, MethodReference method)
	{
		if (method.DeclaringType.HasGenericParameters)
		{
			return false;
		}
		if (method.HasGenericParameters)
		{
			return false;
		}
		if (method.IsUnmanagedCallersOnly)
		{
			return false;
		}
		if (!MethodWriter.MethodNeedsWritten(context, method))
		{
			return false;
		}
		return true;
	}

	protected override InvokerCollection ProcessItem(GlobalSecondaryCollectionContext context, ReadOnlyCollection<Il2CppMethodSpec> item)
	{
		InvokerCollection collector = new InvokerCollection();
		SecondaryCollectionContext collectionContext = context.CreateCollectionContext();
		foreach (Il2CppMethodSpec genericMethod in item)
		{
			if (MethodTables.MethodNeedsTable(collectionContext, genericMethod))
			{
				collector.Add(collectionContext, genericMethod.GenericMethod);
			}
		}
		return collector;
	}

	protected override string ProfilerDetailsForItem2(ReadOnlyCollection<Il2CppMethodSpec> workerItem)
	{
		return "Generic Methods";
	}

	protected override ReadOnlyInvokerCollection CreateEmptyResult()
	{
		return null;
	}

	protected override ReadOnlyCollection<object> OrderItemsForScheduling(GlobalSchedulingContext context, ReadOnlyCollection<AssemblyDefinition> items, ReadOnlyCollection<ReadOnlyCollection<Il2CppMethodSpec>> items2)
	{
		return items2.SelectMany((ReadOnlyCollection<Il2CppMethodSpec> i) => i).ToList().Chunk(context.InputData.JobCount * 2)
			.Concat(items.Cast<object>())
			.ToList()
			.AsReadOnly();
	}

	protected override ReadOnlyInvokerCollection PostProcess(GlobalSecondaryCollectionContext context, ReadOnlyCollection<InvokerCollection> data)
	{
		InvokerCollection collector = new InvokerCollection();
		foreach (InvokerCollection result in data)
		{
			collector.Add(result);
		}
		return collector.Complete();
	}
}
