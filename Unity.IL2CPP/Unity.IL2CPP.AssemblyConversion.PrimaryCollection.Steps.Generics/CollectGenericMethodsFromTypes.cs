using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;

public class CollectGenericMethodsFromTypes : StepAction<GlobalPrimaryCollectionContext>
{
	private readonly ReadOnlyCollection<GenericInstanceType> _chunk;

	protected override string Name => "Collect Generic Methods from Types";

	public CollectGenericMethodsFromTypes(ReadOnlyCollection<GenericInstanceType> chunk)
	{
		_chunk = chunk;
	}

	protected override bool Skip(GlobalPrimaryCollectionContext context)
	{
		return false;
	}

	protected override void Process(GlobalPrimaryCollectionContext context)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		foreach (GenericInstanceType item in _chunk)
		{
			foreach (LazilyInflatedMethod methodContext in item.IterateLazilyInflatedMethods(collectionContext))
			{
				if ((methodContext.DeclaringType.IsGenericInstance || methodContext.InflatedMethod.IsGenericInstance) && MethodWriter.MethodNeedsWritten(collectionContext, methodContext.InflatedMethod))
				{
					context.Collectors.GenericMethods.Add(collectionContext, methodContext.InflatedMethod);
				}
			}
		}
	}
}
