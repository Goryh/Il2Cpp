using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.GenericsCollection.CodeFlow;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;

internal class CCWMarshallingFunctionsCollectionForGenerics : StepAction<GlobalPrimaryCollectionContext>
{
	private readonly IImmutableGenericsCollection _genericsCollectionData;

	private readonly CodeFlowCollectionResults _codeFlowResults;

	protected override string Name => "Collect Generic CCWMarshallingFunctions";

	public CCWMarshallingFunctionsCollectionForGenerics(IImmutableGenericsCollection genericsCollectionData, CodeFlowCollectionResults codeFlowResults)
	{
		_genericsCollectionData = genericsCollectionData;
		_codeFlowResults = codeFlowResults;
	}

	protected override bool Skip(GlobalPrimaryCollectionContext context)
	{
		return false;
	}

	protected override void Process(GlobalPrimaryCollectionContext context)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		foreach (TypeReference type in _codeFlowResults.InstantiatedGenericsAndArrays)
		{
			if (type.NeedsComCallableWrapper(collectionContext))
			{
				context.Collectors.CCWMarshallingFunctionCollector.Add(collectionContext, type);
			}
		}
		foreach (GenericInstanceType type2 in _genericsCollectionData.TypeDeclarations)
		{
			if (CCWMarshalingFunctionCollection.NeedsComCallableWrapperForMarshaledType(collectionContext, type2))
			{
				context.Collectors.CCWMarshallingFunctionCollector.Add(collectionContext, type2);
			}
		}
	}
}
