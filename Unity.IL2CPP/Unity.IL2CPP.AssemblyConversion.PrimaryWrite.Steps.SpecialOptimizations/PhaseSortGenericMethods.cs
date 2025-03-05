using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.SpecialOptimizations;

public class PhaseSortGenericMethods : ContextFreeScheduledStep<GlobalWriteContext>
{
	private readonly GenericMethodCollectorComponent _component;

	protected override string Name => "Phase Sort Generic Methods";

	public PhaseSortGenericMethods(GenericMethodCollectorComponent component)
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
