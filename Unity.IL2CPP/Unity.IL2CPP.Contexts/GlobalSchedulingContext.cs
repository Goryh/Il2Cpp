using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts;

public class GlobalSchedulingContext
{
	public class ContextResults
	{
		private readonly IGlobalContextPhaseResultsProvider _phaseResults;

		public AssemblyConversionResults.InitializePhase Initialize => _phaseResults.Initialize;

		public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

		public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

		public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection => _phaseResults.SecondaryCollection;

		public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
		{
			_phaseResults = phaseResults;
		}
	}

	public class ContextServices
	{
		public readonly IContextScopeService ContextScope;

		public readonly ITinyProfilerService TinyProfiler;

		public ContextServices(IGlobalContextServicesProvider servicesProvider)
		{
			if (servicesProvider.ContextScope == null)
			{
				throw new ArgumentNullException("ContextScope");
			}
			ContextScope = servicesProvider.ContextScope;
			TinyProfiler = servicesProvider.TinyProfiler;
		}
	}

	public readonly ContextResults Results;

	public readonly ContextServices Services;

	public readonly AssemblyConversionInputData InputData;

	public readonly AssemblyConversionParameters Parameters;

	public GlobalSchedulingContext(AssemblyConversionContext assemblyConversionContext)
		: this(assemblyConversionContext.ContextDataProvider)
	{
	}

	public GlobalSchedulingContext(IGlobalContextDataProvider provider)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		Results = new ContextResults(provider.PhaseResults);
		Services = new ContextServices(provider.Services);
		Parameters = provider.Parameters;
		InputData = provider.InputData;
	}
}
