using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class AssemblyCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, CollectedAssemblyData>
{
	protected override string Name => "Collect Assembly Data";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override CollectedAssemblyData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		foreach (TypeDefinition type in item.GetAllTypes())
		{
			if (!MethodWriter.TypeMethodsCanBeDirectlyCalled(readOnlyContext, type))
			{
				continue;
			}
			foreach (MethodDefinition method in type.Methods)
			{
				if (!method.HasGenericParameters && MethodWriter.MethodNeedsWritten(readOnlyContext, method))
				{
					context.Collectors.Methods.Add(method);
				}
			}
		}
		return new CollectedAssemblyData();
	}
}
