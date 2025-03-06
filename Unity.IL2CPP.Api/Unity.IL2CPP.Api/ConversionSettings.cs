using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public class ConversionSettings
{
	[Analytics("Whether or not emitting null checks was enabled\n\nNote that this option can also be enabled via the code generation option field")]
	[HelpDetails("Enables generation of null checks", null)]
	public bool EmitNullChecks;

	[Analytics("Explicitly disable NullChecks even if it was enabled")]
	[HelpDetails("Explicitly disable NullChecks even if it was enabled", null)]
	public bool DisableNullChecks;

	[Analytics("Whether or not emitting stack traces was enabled\n\nNote that this option can also be enabled via the code generation option field")]
	[HelpDetails("Enables generation of stacktrace sentries in C++ code at the start of every managed method. This enables support for stacktraces for platforms that do not have system APIs to walk the stack (for example, one such platform is WebGL)", null)]
	public bool EnableStacktrace;

	[Analytics("Whether or not deep profiler support was enabled\n\nNote that this option can also be enabled via the feature field")]
	[HideFromHelp]
	public bool EnableDeepProfiler;

	[Analytics("Whether or not emitting stats was enabled\n\nNote that this option can also be enabled via the diagnostic option field")]
	[HelpDetails("Enables conversion statistics", null)]
	public bool EnableStats;

	[Analytics("Whether or not array bounds checks was enabled\n\nNote that this option can also be enabled via the code generation option field")]
	[HelpDetails("Enables generation of array bounds checks", null)]
	public bool EnableArrayBoundsCheck;

	[Analytics("Explicitly disable ArrayBoundCheck even if it was enabled")]
	[HelpDetails("Explicitly disable ArrayBoundCheck even if it was enabled", null)]
	public bool DisableArrayBoundsCheck;

	[Analytics("Whether or not divide by zero checks was enabled\n\nNote that this option can also be enabled via the code generation option field")]
	[HelpDetails("Enables generation of divide by zero checks", null)]
	public bool EnableDivideByZeroCheck;

	[HideFromHelp]
	public bool EnableErrorMessageTest;

	[HideFromHelp]
	public bool EnablePrimitiveValueTypeGenericSharing = true;

	[HideFromHelp]
	public ProfilerOptions ProfilerOptions = ProfilerOptions.MethodEnterExit;

	[HideFromHelp]
	public bool EmitSourceMapping;

	[HideFromHelp]
	public bool EmitMethodMap;

	[Analytics("Whether or not emitting comments was enabled\n\nNote that this option can also be enabled via the code generation option field")]
	[HelpDetails("Annotations to the generated code will be emitted as comments", null)]
	public bool EmitComments;

	[HideFromHelp]
	public bool NeverAttachDialog;

	[HideFromHelp]
	public bool EmitAttachDialog;

	[HideFromHelp]
	public bool CodeConversionCache;

	[HelpDetails("String to match the name of method(s) to show the assembly output for", null)]
	public string AssemblyMethod;

	[Analytics("Whether or not generic sharing should be disabled")]
	[HelpDetails("Disables generic sharing", null)]
	public bool DisableGenericSharing;

	public bool EmitReversePInvokeWrapperDebuggingHelpers;

	[Analytics("The maximum recursive generic depth")]
	[HelpDetails("Set the maximum depth to implement recursive generic methods. The default value is 7.", null)]
	public int MaximumRecursiveGenericDepth = -1;

	[Analytics("The number of times to iterate looking for generic virtual methods")]
	[HelpDetails("Set the maximum number of times to iterate looking for generic virtual methods. The default value is 1.", null)]
	public int GenericVirtualMethodIterations = -1;

	[HideFromHelp]
	public ConversionMode ConversionMode = ConversionMode.Classic;

	[Analytics("Flags indicating which code generation options were enabled")]
	[HelpDetails("Specify an option related to code generation", null)]
	public CodeGenerationOptions CodeGenerationOption;

	[Analytics("Flags indicating which file generation options were enabled")]
	[HelpDetails("Specify an option related to file output", null)]
	public FileGenerationOptions FileGenerationOption;

	[Analytics("Flags indicating which generics options were enabled")]
	[HelpDetails("Specify an option related to generics", null)]
	public GenericsOptions GenericsOption;

	[Analytics("Flags indicating whether or not various features were enabled")]
	[HelpDetails("Enable a feature of il2cpp", null)]
	public Features Feature;

	[Analytics("Flags indicating which diagnostic options were enabled")]
	[HelpDetails("Enable a diagnostic ability", null)]
	public DiagnosticOptions DiagnosticOption;

	[HideFromHelp]
	public TestingOptions TestingOption;
}
