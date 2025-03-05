using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;

public class CollectGenericMethodsFromMethods : StepAction<GlobalPrimaryCollectionContext>
{
	private readonly ReadOnlyHashSet<GenericInstanceMethod> _collectedMethods;

	protected override string Name => "Collect Generic Methods from Methods";

	public CollectGenericMethodsFromMethods(ReadOnlyHashSet<GenericInstanceMethod> collectedMethods)
	{
		_collectedMethods = collectedMethods;
	}

	protected override bool Skip(GlobalPrimaryCollectionContext context)
	{
		return false;
	}

	protected override void Process(GlobalPrimaryCollectionContext context)
	{
		PrimaryCollectionContext collection = context.CreateCollectionContext();
		foreach (GenericInstanceMethod method in _collectedMethods)
		{
			if ((method.IsGenericInstance || method.DeclaringType.IsGenericInstance) && MethodWriter.MethodNeedsWritten(collection, method))
			{
				context.Collectors.GenericMethods.Add(collection, method);
			}
		}
	}
}
