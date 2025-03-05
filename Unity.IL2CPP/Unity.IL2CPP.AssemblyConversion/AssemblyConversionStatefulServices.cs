using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionStatefulServices : IGlobalContextStatefulServicesProvider, IUnrestrictedContextStatefulServicesProvider
{
	public readonly SourceAnnotationWriterComponent SourceAnnotationWriter = new SourceAnnotationWriterComponent();

	public readonly NamingComponent Naming;

	public readonly ErrorInformationComponent ErrorInformation = new ErrorInformationComponent();

	public readonly ImmediateSchedulerComponent Scheduler = new ImmediateSchedulerComponent();

	public readonly DiagnosticsComponent Diagnostics = new DiagnosticsComponent();

	public readonly MessageLoggerComponent MessageLogger = new MessageLoggerComponent();

	public readonly VTableBuilderComponent VTableBuilder = new VTableBuilderComponent();

	public readonly TypeFactoryComponent TypeFactory = new TypeFactoryComponent();

	public readonly PathFactoryComponent PathFactory = new PathFactoryComponent();

	SourceAnnotationWriterComponent IUnrestrictedContextStatefulServicesProvider.SourceAnnotationWriter => SourceAnnotationWriter;

	NamingComponent IUnrestrictedContextStatefulServicesProvider.Naming => Naming;

	ErrorInformationComponent IUnrestrictedContextStatefulServicesProvider.ErrorInformation => ErrorInformation;

	ImmediateSchedulerComponent IUnrestrictedContextStatefulServicesProvider.Scheduler => Scheduler;

	DiagnosticsComponent IUnrestrictedContextStatefulServicesProvider.Diagnostics => Diagnostics;

	MessageLoggerComponent IUnrestrictedContextStatefulServicesProvider.MessageLogger => MessageLogger;

	VTableBuilderComponent IUnrestrictedContextStatefulServicesProvider.VTableBuilder => VTableBuilder;

	TypeFactoryComponent IUnrestrictedContextStatefulServicesProvider.TypeFactory => TypeFactory;

	IDiagnosticsService IGlobalContextStatefulServicesProvider.Diagnostics => Diagnostics;

	IMessageLogger IGlobalContextStatefulServicesProvider.MessageLogger => MessageLogger;

	IWorkScheduler IGlobalContextStatefulServicesProvider.Scheduler => Scheduler;

	INamingService IGlobalContextStatefulServicesProvider.Naming => Naming;

	ISourceAnnotationWriter IGlobalContextStatefulServicesProvider.SourceAnnotationWriter => SourceAnnotationWriter;

	IErrorInformationService IGlobalContextStatefulServicesProvider.ErrorInformation => ErrorInformation;

	IVTableBuilderService IGlobalContextStatefulServicesProvider.VTable => VTableBuilder;

	IDataModelService IGlobalContextStatefulServicesProvider.TypeFactory => TypeFactory;

	PathFactoryComponent IUnrestrictedContextStatefulServicesProvider.PathFactory => PathFactory;

	IPathFactoryService IGlobalContextStatefulServicesProvider.PathFactory => PathFactory;

	public AssemblyConversionStatefulServices()
	{
		Naming = new NamingComponent();
	}
}
