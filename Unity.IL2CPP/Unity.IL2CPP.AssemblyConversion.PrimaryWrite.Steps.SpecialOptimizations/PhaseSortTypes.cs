using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.SpecialOptimizations;

public class PhaseSortTypes : ContextFreeScheduledStep<GlobalWriteContext>
{
	private readonly TypeCollectorComponent _component;

	protected override string Name => "Phase Sort Types";

	public PhaseSortTypes(TypeCollectorComponent component)
	{
		_component = component;
	}

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return context.Parameters.EnableSerialConversion;
	}

	protected override void Worker()
	{
		_component.PhaseSortItemsToReduceFinalSortTime();
	}
}
