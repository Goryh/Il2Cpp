using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.DataModel;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.Contexts;

public class AssemblyConversionContext : IDisposable
{
	public class DataProvider : IGlobalContextDataProvider, IUnrestrictedContextDataProvider, IAllContextsProvider
	{
		private readonly AssemblyConversionContext _context;

		AssemblyConversionParameters IGlobalContextDataProvider.Parameters => _context.Parameters;

		AssemblyConversionInputData IUnrestrictedContextDataProvider.InputData => _context.InputData;

		AssemblyConversionResults IUnrestrictedContextDataProvider.PhaseResults => _context.Results;

		IUnrestrictedContextCollectorProvider IUnrestrictedContextDataProvider.Collectors => _context.Collectors;

		IUnrestrictedContextServicesProvider IUnrestrictedContextDataProvider.Services => _context.Services;

		IUnrestrictedContextStatefulServicesProvider IUnrestrictedContextDataProvider.StatefulServices => _context.StatefulServices;

		AssemblyConversionParameters IUnrestrictedContextDataProvider.Parameters => _context.Parameters;

		AssemblyConversionInputData IGlobalContextDataProvider.InputData => _context.InputData;

		IGlobalContextCollectorProvider IGlobalContextDataProvider.Collectors => _context.Collectors;

		IGlobalContextServicesProvider IGlobalContextDataProvider.Services => _context.Services;

		IGlobalContextStatefulServicesProvider IGlobalContextDataProvider.StatefulServices => _context.StatefulServices;

		IGlobalContextResultsProvider IGlobalContextDataProvider.Results => _context.Collectors;

		IGlobalContextPhaseResultsProvider IGlobalContextDataProvider.PhaseResults => _context.Results;

		GlobalWriteContext IAllContextsProvider.GlobalWriteContext => _context.GlobalWriteContext;

		GlobalPrimaryCollectionContext IAllContextsProvider.GlobalPrimaryCollectionContext => _context.GlobalPrimaryCollectionContext;

		GlobalSecondaryCollectionContext IAllContextsProvider.GlobalSecondaryCollectionContext => _context.GlobalSecondaryCollectionContext;

		GlobalReadOnlyContext IAllContextsProvider.GlobalReadOnlyContext => _context.GlobalReadOnlyContext;

		GlobalMinimalContext IAllContextsProvider.GlobalMinimalContext => _context.GlobalMinimalContext;

		public DataProvider(AssemblyConversionContext context)
		{
			_context = context;
		}
	}

	public GlobalWriteContext GlobalWriteContext;

	public GlobalPrimaryCollectionContext GlobalPrimaryCollectionContext;

	public GlobalSecondaryCollectionContext GlobalSecondaryCollectionContext;

	public GlobalReadOnlyContext GlobalReadOnlyContext;

	public GlobalMinimalContext GlobalMinimalContext;

	public GlobalSchedulingContext GlobalSchedulingContext;

	public readonly AssemblyConversionInputData InputData;

	public readonly AssemblyConversionInputDataForTopLevelAccess InputDataForTopLevel;

	public readonly AssemblyConversionParameters Parameters;

	public readonly AssemblyConversionCollectors Collectors;

	public readonly AssemblyConversionServices Services;

	public readonly AssemblyConversionStatefulServices StatefulServices;

	public readonly AssemblyConversionResults Results = new AssemblyConversionResults();

	public readonly DataProvider ContextDataProvider;

	private AssemblyConversionContext(TinyProfiler2 tinyProfiler, AssemblyConversionInputData inputData, AssemblyConversionParameters parameters, AssemblyConversionInputDataForTopLevelAccess dataForTopLevel)
	{
		InputData = inputData;
		InputDataForTopLevel = dataForTopLevel;
		Parameters = parameters;
		Collectors = new AssemblyConversionCollectors();
		ContextDataProvider = new DataProvider(this);
		Services = new AssemblyConversionServices(tinyProfiler);
		StatefulServices = new AssemblyConversionStatefulServices();
	}

	public static AssemblyConversionContext SetupNew(TinyProfiler2 tinyProfiler, AssemblyConversionInputData inputData, AssemblyConversionParameters parameters, AssemblyConversionInputDataForTopLevelAccess dataForTopLevel)
	{
		AssemblyConversionContext assemblyConversionContext = new AssemblyConversionContext(tinyProfiler, inputData, parameters, dataForTopLevel);
		assemblyConversionContext.GlobalReadOnlyContext = new GlobalReadOnlyContext(assemblyConversionContext);
		assemblyConversionContext.GlobalMinimalContext = new GlobalMinimalContext(assemblyConversionContext);
		assemblyConversionContext.GlobalWriteContext = new GlobalWriteContext(assemblyConversionContext);
		assemblyConversionContext.GlobalPrimaryCollectionContext = new GlobalPrimaryCollectionContext(assemblyConversionContext);
		assemblyConversionContext.GlobalSecondaryCollectionContext = new GlobalSecondaryCollectionContext(assemblyConversionContext);
		assemblyConversionContext.GlobalSchedulingContext = new GlobalSchedulingContext(assemblyConversionContext);
		return assemblyConversionContext;
	}

	public MinimalContext CreateMinimalContext()
	{
		return GlobalMinimalContext.CreateMinimalContext();
	}

	public PrimaryCollectionContext CreatePrimaryCollectionContext()
	{
		return GlobalPrimaryCollectionContext.CreateCollectionContext();
	}

	public SecondaryCollectionContext CreateSecondaryCollectionContext()
	{
		return GlobalSecondaryCollectionContext.CreateCollectionContext();
	}

	public ReadOnlyContext CreateReadOnlyContext()
	{
		return GlobalReadOnlyContext.GetReadOnlyContext();
	}

	public SourceWritingContext CreateSourceWritingContext()
	{
		return new SourceWritingContext(GlobalWriteContext);
	}

	public AssemblyWriteContext CreateAssemblyWritingContext(AssemblyDefinition assembly)
	{
		return new AssemblyWriteContext(CreateSourceWritingContext(), assembly);
	}

	public void Dispose()
	{
		Services.DataModel.Dispose();
	}
}
