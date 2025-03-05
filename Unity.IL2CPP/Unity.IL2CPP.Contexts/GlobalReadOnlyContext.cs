using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts;

public class GlobalReadOnlyContext
{
	public class ContextResults
	{
		private readonly IGlobalContextPhaseResultsProvider _phaseResults;

		public AssemblyConversionResults.InitializePhase Initialize => _phaseResults.Initialize;

		public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

		public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

		public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection => _phaseResults.SecondaryCollection;

		public AssemblyConversionResults.SetupPhase Setup => _phaseResults.Setup;

		public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
		{
			_phaseResults = phaseResults;
		}
	}

	public class ContextServices
	{
		public readonly INamingService Naming;

		public readonly IContextScopeService ContextScope;

		public readonly IGuidProvider GuidProvider;

		public readonly ITypeProviderService TypeProvider;

		public readonly IObjectFactory Factory;

		public readonly IWindowsRuntimeProjections WindowsRuntime;

		public readonly IPathFactoryService PathFactory;

		public readonly IDiagnosticsService Diagnostics;

		public readonly IMessageLogger MessageLogger;

		public readonly IVTableBuilderService VTable;

		public readonly IDataModelService TypeFactory;

		public readonly IErrorInformationService ErrorInformation;

		public readonly ITinyProfilerService TinyProfiler;

		public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
		{
			if (statefulServicesProvider.Naming == null)
			{
				throw new ArgumentNullException("Naming");
			}
			if (servicesProvider.ContextScope == null)
			{
				throw new ArgumentNullException("ContextScope");
			}
			if (servicesProvider.GuidProvider == null)
			{
				throw new ArgumentNullException("GuidProvider");
			}
			if (servicesProvider.TypeProvider == null)
			{
				throw new ArgumentNullException("TypeProvider");
			}
			if (servicesProvider.Factory == null)
			{
				throw new ArgumentNullException("Factory");
			}
			if (servicesProvider.WindowsRuntime == null)
			{
				throw new ArgumentNullException("WindowsRuntime");
			}
			if (statefulServicesProvider.PathFactory == null)
			{
				throw new ArgumentNullException("PathFactory");
			}
			if (statefulServicesProvider.Diagnostics == null)
			{
				throw new ArgumentNullException("Diagnostics");
			}
			if (statefulServicesProvider.VTable == null)
			{
				throw new ArgumentNullException("VTable");
			}
			if (statefulServicesProvider.TypeFactory == null)
			{
				throw new ArgumentNullException("TypeFactory");
			}
			if (statefulServicesProvider.ErrorInformation == null)
			{
				throw new ArgumentNullException("ErrorInformation");
			}
			if (servicesProvider.TinyProfiler == null)
			{
				throw new ArgumentNullException("TinyProfiler");
			}
			Naming = statefulServicesProvider.Naming;
			ContextScope = servicesProvider.ContextScope;
			GuidProvider = servicesProvider.GuidProvider;
			TypeProvider = servicesProvider.TypeProvider;
			Factory = servicesProvider.Factory;
			WindowsRuntime = servicesProvider.WindowsRuntime;
			PathFactory = statefulServicesProvider.PathFactory;
			Diagnostics = statefulServicesProvider.Diagnostics;
			MessageLogger = statefulServicesProvider.MessageLogger;
			VTable = statefulServicesProvider.VTable;
			TypeFactory = statefulServicesProvider.TypeFactory;
			ErrorInformation = statefulServicesProvider.ErrorInformation;
			TinyProfiler = servicesProvider.TinyProfiler;
		}
	}

	public readonly ContextServices Services;

	public readonly ContextResults Results;

	public readonly AssemblyConversionInputData InputData;

	public readonly AssemblyConversionParameters Parameters;

	private readonly ReadOnlyContext _instance;

	public GlobalReadOnlyContext(AssemblyConversionContext assemblyConversionContext)
		: this(assemblyConversionContext.ContextDataProvider)
	{
	}

	public GlobalReadOnlyContext(IGlobalContextDataProvider provider)
	{
		Services = new ContextServices(provider.Services, provider.StatefulServices);
		Results = new ContextResults(provider.PhaseResults);
		Parameters = provider.Parameters;
		InputData = provider.InputData;
		_instance = new ReadOnlyContext(this);
	}

	public ReadOnlyContext GetReadOnlyContext()
	{
		return _instance;
	}
}
