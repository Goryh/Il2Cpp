using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;

public class FullConverter : BasePerAssemblyConverter
{
	private class FullPerAssemblyPhaseResultsSetter : IPhaseResultsSetter<GlobalFullyForkedContext>
	{
		private readonly IUnrestrictedContextDataProvider _parentContext;

		public FullPerAssemblyPhaseResultsSetter(IUnrestrictedContextDataProvider parentContext)
		{
			_parentContext = parentContext;
		}

		public void SetPhaseResults(ReadOnlyCollection<GlobalFullyForkedContext> forkedContexts)
		{
			_parentContext.PhaseResults.SetCompletionPhaseResults(new AssemblyConversionResults.CompletionPhase(_parentContext.Collectors.Stats, _parentContext.StatefulServices.MessageLogger.Complete(), new Il2CppDataTable()));
		}
	}

	public override void Run(AssemblyConversionContext context)
	{
		InitializePhase.Run(context);
		SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
		GenericSharingAnalysisResults genericSharingAnalysisResults = BasePerAssemblyConverter.RunGenericSharingAnalysis(context);
		using (BasePerAssemblyConverter.CreateHackedScheduler(new object(), context.GlobalSchedulingContext))
		{
			using ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> forkedScope = Fork(context);
			foreach (ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext>.Data item in forkedScope.Items)
			{
				BaseConversionContainer value = item.Value;
				GlobalFullyForkedContext forkedContext = item.Context;
				value.RunPrimaryCollection(forkedContext, genericSharingAnalysisResults);
				value.RunPrimaryWrite(forkedContext);
				value.RunSecondaryCollection(forkedContext);
				value.RunSecondaryWrite(forkedContext);
				value.RunCompletion(forkedContext);
			}
		}
		WriteGlobalCodeRegistration(context);
	}

	protected override ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context, ReadOnlyCollection<BaseConversionContainer> containers, ReadOnlyCollection<OverrideObjects> overrideObjects)
	{
		return ContextForker.ForFullPerAssembly(context, containers, overrideObjects, new FullPerAssemblyPhaseResultsSetter(context.ContextDataProvider));
	}

	protected override ReadOnlyCollection<OverrideObjects> CreateContainerOverrideObjects(ReadOnlyCollection<BaseConversionContainer> containers)
	{
		return containers.Select((BaseConversionContainer c) => PerAssemblyUtilities.CreateOverrideObjectsForFull(c.IncludeTypeDefinitionInContext, c.Name, c.CleanName)).ToList().AsReadOnly();
	}

	private void WriteGlobalCodeRegistration(AssemblyConversionContext context)
	{
		ReadOnlyCollection<string> codeGenModules = context.Results.Initialize.AllAssembliesOrderedByDependency.Select(context.GlobalWriteContext.Services.Naming.ForCodeGenModule).Concat(new string[1] { "g_Il2CppGenerics_CodeGenModule" }).ToList()
			.AsReadOnly();
		CodeRegistrationWriter.WritePerAssemblyGlobalCodeRegistration(context.CreateSourceWritingContext(), codeGenModules);
	}
}
