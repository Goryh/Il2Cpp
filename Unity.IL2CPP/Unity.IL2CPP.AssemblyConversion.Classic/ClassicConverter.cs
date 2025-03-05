using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.Classic;

internal class ClassicConverter : BaseAssemblyConverter
{
	public override void Run(AssemblyConversionContext context)
	{
		InitializePhase.Run(context);
		SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
		PrimaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
		PrimaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess, context.Results.Initialize.EntryAssembly);
		SecondaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
		context.Services.DataModel.TypeContext.FreezeInflations();
		SecondaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
		CompletionPhase.Run(context);
	}
}
