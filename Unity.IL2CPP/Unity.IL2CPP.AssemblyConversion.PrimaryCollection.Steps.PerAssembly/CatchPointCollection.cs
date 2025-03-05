using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class CatchPointCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, ICatchPointProvider>
{
	protected override string Name => "Collect Catch Points";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EnableDebugger;
	}

	protected override ICatchPointProvider ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		CatchPointCollector collector = new CatchPointCollector();
		foreach (TypeDefinition allType in item.GetAllTypes())
		{
			foreach (MethodDefinition method in allType.Methods.Where((MethodDefinition m) => m.HasBody && m.Body.Instructions.Count > 0))
			{
				ControlFlowGraph cfg = ControlFlowGraph.Create(method);
				ExceptionSupport exceptionSupport = new ExceptionSupport(context.GetReadOnlyContext(), method, cfg.FlowTree, null);
				Queue<Node> nodesToProcess = new Queue<Node>();
				nodesToProcess.Enqueue(exceptionSupport.FlowTree.Children);
				while (nodesToProcess.Count > 0)
				{
					Node current = nodesToProcess.Dequeue();
					nodesToProcess.Enqueue(current.Children);
					if (current.Type == NodeType.Catch)
					{
						collector.AddCatchPoint(collectionContext, method, current);
					}
				}
			}
		}
		return collector;
	}
}
