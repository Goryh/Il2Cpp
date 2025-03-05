using Unity.IL2CPP.Api;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionParameters
{
	public readonly bool EmitSourceMapping;

	public readonly bool EmitMethodMap;

	public readonly bool NoLazyStaticConstructors;

	public readonly bool EmitNullChecks;

	public readonly bool EnableStacktrace;

	public readonly bool EnableArrayBoundsCheck;

	public readonly bool EnableDivideByZeroCheck;

	public readonly bool EmitComments;

	public readonly bool EnableSerialConversion;

	public readonly bool VirtualCallsViaInvokers;

	public readonly bool SharedGenericCallsViaInvokers;

	public readonly bool DelegateCallsViaInvokers;

	public readonly bool CanShareEnumTypes;

	public readonly bool EnablePrimitiveValueTypeGenericSharing;

	public readonly bool DisableGenericSharing;

	public readonly bool DisableFullGenericSharing;

	public readonly bool FullGenericSharingOnly;

	public readonly bool FullGenericSharingStaticConstructors;

	public readonly ProfilerOptions ProfilerOptions;

	public readonly RuntimeBackend Backend;

	public readonly CodeGenerationOptions CodeGenerationOptions;

	public readonly DiagnosticOptions DiagnosticOptions;

	public readonly bool EnableStats;

	public readonly bool NeverAttachDialog;

	public readonly bool EmitAttachDialog;

	public readonly bool DebuggerOff;

	public readonly bool EmitReversePInvokeWrapperDebuggingHelpers;

	public readonly bool EnableReload;

	public readonly bool CodeConversionCache;

	public readonly bool EnableDebugger;

	public readonly bool EnableDeepProfiler;

	public readonly bool EnableAnalytics;

	public readonly bool EnableErrorMessageTest;

	public readonly bool GoogleBenchmark;

	public readonly bool EnableInlining;

	public AssemblyConversionParameters(CodeGenerationOptions codeGenerationOptions, FileGenerationOptions fileGenerationOptions, GenericsOptions genericsOptions, ProfilerOptions profilerOptions, RuntimeBackend runtimeBackend, RuntimeProfile profile, DiagnosticOptions diagnosticOptions, Features features, TestingOptions testingOptions)
	{
		CodeGenerationOptions = codeGenerationOptions;
		DiagnosticOptions = diagnosticOptions;
		Backend = runtimeBackend;
		ProfilerOptions = profilerOptions;
		EmitSourceMapping = fileGenerationOptions.HasFlag(FileGenerationOptions.EmitSourceMapping);
		EmitMethodMap = fileGenerationOptions.HasFlag(FileGenerationOptions.EmitMethodMap);
		NoLazyStaticConstructors = !codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableLazyStaticConstructors);
		EmitNullChecks = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableNullChecks);
		EnableStacktrace = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableStacktrace);
		EnableArrayBoundsCheck = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableArrayBoundsCheck);
		EnableDivideByZeroCheck = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableDivideByZeroCheck);
		EmitComments = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableComments);
		EnableSerialConversion = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableSerial);
		EnableInlining = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableInlining);
		VirtualCallsViaInvokers = codeGenerationOptions.HasFlag(CodeGenerationOptions.VirtualCallsViaInvokers);
		SharedGenericCallsViaInvokers = codeGenerationOptions.HasFlag(CodeGenerationOptions.SharedGenericCallsViaInvokers);
		DelegateCallsViaInvokers = codeGenerationOptions.HasFlag(CodeGenerationOptions.DelegateCallsViaInvokers);
		CanShareEnumTypes = genericsOptions.HasFlag(GenericsOptions.EnableEnumTypeSharing);
		EnablePrimitiveValueTypeGenericSharing = genericsOptions.HasFlag(GenericsOptions.EnablePrimitiveValueTypeGenericSharing);
		DisableGenericSharing = !genericsOptions.HasFlag(GenericsOptions.EnableSharing);
		DisableFullGenericSharing = DisableGenericSharing || genericsOptions.HasFlag(GenericsOptions.EnableLegacyGenericSharing);
		FullGenericSharingOnly = genericsOptions.HasFlag(GenericsOptions.EnableFullSharing);
		FullGenericSharingStaticConstructors = genericsOptions.HasFlag(GenericsOptions.EnableFullSharingForStaticConstructors);
		EnableStats = diagnosticOptions.HasFlag(DiagnosticOptions.EnableStats);
		NeverAttachDialog = diagnosticOptions.HasFlag(DiagnosticOptions.NeverAttachDialog);
		EmitAttachDialog = diagnosticOptions.HasFlag(DiagnosticOptions.EmitAttachDialog);
		DebuggerOff = diagnosticOptions.HasFlag(DiagnosticOptions.DebuggerOff);
		EmitReversePInvokeWrapperDebuggingHelpers = diagnosticOptions.HasFlag(DiagnosticOptions.EmitReversePInvokeWrapperDebuggingHelpers);
		EnableReload = features.HasFlag(Features.EnableReload);
		EnableDebugger = features.HasFlag(Features.EnableDebugger);
		CodeConversionCache = features.HasFlag(Features.EnableCodeConversionCache);
		EnableDeepProfiler = features.HasFlag(Features.EnableDeepProfiler);
		EnableAnalytics = features.HasFlag(Features.EnableAnalytics);
		EnableErrorMessageTest = testingOptions.HasFlag(TestingOptions.EnableErrorMessageTest);
		GoogleBenchmark = testingOptions.HasFlag(TestingOptions.EnableGoogleBenchmark);
	}
}
