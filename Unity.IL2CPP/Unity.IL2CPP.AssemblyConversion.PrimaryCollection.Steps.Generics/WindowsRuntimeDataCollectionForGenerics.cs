using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;

public class WindowsRuntimeDataCollectionForGenerics : StepAction<GlobalPrimaryCollectionContext>
{
	private readonly IImmutableGenericsCollection _genericsCollectionData;

	protected override string Name => "Add Windows Runtime type names";

	public WindowsRuntimeDataCollectionForGenerics(IImmutableGenericsCollection genericsCollectionData)
	{
		_genericsCollectionData = genericsCollectionData;
	}

	protected override bool Skip(GlobalPrimaryCollectionContext context)
	{
		return false;
	}

	protected override void Process(GlobalPrimaryCollectionContext context)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		IWindowsRuntimeTypeWithNameCollector typeWithNameCollector = context.Collectors.WindowsRuntimeTypeWithNames;
		foreach (GenericInstanceType type in _genericsCollectionData.TypeDeclarations)
		{
			TypeDefinition typeDef = type.Resolve();
			if ((typeDef == context.Services.TypeProvider.IReferenceType || typeDef == context.Services.TypeProvider.IReferenceArrayType) && type.IsComOrWindowsRuntimeInterface(collectionContext))
			{
				typeWithNameCollector.AddWindowsRuntimeTypeWithName(collectionContext, type, type.GetWindowsRuntimeTypeName(collectionContext));
				context.Collectors.Stats.RecordWindowsRuntimeBoxedType();
				continue;
			}
			TypeReference windowsRuntimeType = context.Services.WindowsRuntime.ProjectToWindowsRuntime(collectionContext, type);
			if (type != windowsRuntimeType && (windowsRuntimeType.IsComOrWindowsRuntimeInterface(collectionContext) || windowsRuntimeType.IsWindowsRuntimeDelegate(collectionContext)))
			{
				typeWithNameCollector.AddWindowsRuntimeTypeWithName(collectionContext, type, windowsRuntimeType.GetWindowsRuntimeTypeName(collectionContext));
			}
		}
	}
}
