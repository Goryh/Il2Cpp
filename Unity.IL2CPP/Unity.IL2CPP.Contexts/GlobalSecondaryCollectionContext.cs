using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts;

public class GlobalSecondaryCollectionContext : ITinyProfilerProvider
{
	public class ContextCollectors
	{
	}

	public class ContextResults
	{
		private readonly IGlobalContextPhaseResultsProvider _phaseResults;

		public AssemblyConversionResults.InitializePhase Initialize => _phaseResults.Initialize;

		public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

		public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

		public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
		{
			_phaseResults = phaseResults;
		}
	}

	public class ContextServices
	{
		public readonly IWindowsRuntimeProjections WindowsRuntime;

		public readonly IErrorInformationService ErrorInformation;

		public readonly IMessageLogger MessageLogger;

		public readonly IVTableBuilderService VTable;

		public readonly ITinyProfilerService TinyProfiler;

		public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
		{
			WindowsRuntime = servicesProvider.WindowsRuntime;
			ErrorInformation = statefulServicesProvider.ErrorInformation;
			MessageLogger = statefulServicesProvider.MessageLogger;
			VTable = statefulServicesProvider.VTable;
			TinyProfiler = servicesProvider.TinyProfiler;
		}
	}

	private readonly IUnrestrictedContextDataProvider _parent;

	private readonly GlobalMinimalContext _globalMinimalContext;

	private readonly GlobalReadOnlyContext _globalReadOnlyContext;

	public readonly ContextCollectors Collectors;

	public readonly ContextServices Services;

	public readonly ContextResults Results;

	public readonly AssemblyConversionInputData InputData;

	public readonly AssemblyConversionParameters Parameters;

	ITinyProfilerService ITinyProfilerProvider.TinyProfiler => Services.TinyProfiler;

	public GlobalSecondaryCollectionContext(AssemblyConversionContext assemblyConversionContext)
		: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalMinimalContext, assemblyConversionContext.GlobalReadOnlyContext)
	{
	}

	public GlobalSecondaryCollectionContext(IUnrestrictedContextDataProvider parent, IGlobalContextDataProvider provider, GlobalMinimalContext globalMinimalContext, GlobalReadOnlyContext globalReadOnlyContext)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (globalReadOnlyContext == null)
		{
			throw new ArgumentNullException("globalReadOnlyContext");
		}
		if (globalMinimalContext == null)
		{
			throw new ArgumentNullException("globalMinimalContext");
		}
		_parent = parent;
		_globalMinimalContext = globalMinimalContext;
		_globalReadOnlyContext = globalReadOnlyContext;
		Collectors = new ContextCollectors();
		Services = new ContextServices(provider.Services, provider.StatefulServices);
		Results = new ContextResults(provider.PhaseResults);
		Parameters = provider.Parameters;
		InputData = provider.InputData;
	}

	public ForkedContextScope<int, GlobalSecondaryCollectionContext> ForkFor(Func<IUnrestrictedContextDataProvider, ForkedContextScope<int, GlobalSecondaryCollectionContext>> forker)
	{
		return forker(_parent);
	}

	public GlobalMinimalContext AsMinimal()
	{
		return _globalMinimalContext;
	}

	public GlobalReadOnlyContext AsReadOnly()
	{
		return _globalReadOnlyContext;
	}

	public MinimalContext CreateMinimalContext()
	{
		return AsMinimal().CreateMinimalContext();
	}

	public ReadOnlyContext GetReadOnlyContext()
	{
		return AsReadOnly().GetReadOnlyContext();
	}

	public SecondaryCollectionContext CreateCollectionContext()
	{
		return new SecondaryCollectionContext(this);
	}
}
