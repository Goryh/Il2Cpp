using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Unity.IL2CPP.Api;

namespace il2cpp.Compilation;

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Il2CppCommandLineArguments), GenerationMode = JsonSourceGenerationMode.Serialization)]
[GeneratedCode("System.Text.Json.SourceGeneration", "7.0.9.1816")]
internal class CommandLineJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<ProfilerOptions>? _ProfilerOptions;

	private JsonTypeInfo<string>? _String;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<ConversionMode>? _ConversionMode;

	private JsonTypeInfo<CodeGenerationOptions>? _CodeGenerationOptions;

	private JsonTypeInfo<FileGenerationOptions>? _FileGenerationOptions;

	private JsonTypeInfo<GenericsOptions>? _GenericsOptions;

	private JsonTypeInfo<Features>? _Features;

	private JsonTypeInfo<DiagnosticOptions>? _DiagnosticOptions;

	private JsonTypeInfo<TestingOptions>? _TestingOptions;

	private JsonTypeInfo<ConversionSettings>? _ConversionSettings;

	private JsonTypeInfo<ConversionStatistics>? _ConversionStatistics;

	private JsonTypeInfo<string[]>? _StringArray;

	private JsonTypeInfo<ConversionRequest>? _ConversionRequest;

	private JsonTypeInfo<BuildConfiguration>? _BuildConfiguration;

	private JsonTypeInfo<CompilationSettings>? _CompilationSettings;

	private JsonTypeInfo<EmscriptenCompilationRequest>? _EmscriptenCompilationRequest;

	private JsonTypeInfo<CommandLogMode>? _CommandLogMode;

	private JsonTypeInfo<CompilationRequest>? _CompilationRequest;

	private JsonTypeInfo<SettingsForConversionAndCompilation>? _SettingsForConversionAndCompilation;

	private JsonTypeInfo<Il2CppCommandLineArguments>? _Il2CppCommandLineArguments;

	private static CommandLineJsonContext? s_defaultContext;

	private static readonly JsonEncodedText PropName_ConversionRequest = JsonEncodedText.Encode("ConversionRequest");

	private static readonly JsonEncodedText PropName_CompilationRequest = JsonEncodedText.Encode("CompilationRequest");

	private static readonly JsonEncodedText PropName_SettingsForConversionAndCompilation = JsonEncodedText.Encode("SettingsForConversionAndCompilation");

	private static readonly JsonEncodedText PropName_ConvertToCpp = JsonEncodedText.Encode("ConvertToCpp");

	private static readonly JsonEncodedText PropName_CompileCpp = JsonEncodedText.Encode("CompileCpp");

	private static readonly JsonEncodedText PropName_ConvertInGraph = JsonEncodedText.Encode("ConvertInGraph");

	private static readonly JsonEncodedText PropName_CustomIl2CppRoot = JsonEncodedText.Encode("CustomIl2CppRoot");

	private static readonly JsonEncodedText PropName_Settings = JsonEncodedText.Encode("Settings");

	private static readonly JsonEncodedText PropName_Statistics = JsonEncodedText.Encode("Statistics");

	private static readonly JsonEncodedText PropName_Assembly = JsonEncodedText.Encode("Assembly");

	private static readonly JsonEncodedText PropName_Directory = JsonEncodedText.Encode("Directory");

	private static readonly JsonEncodedText PropName_ExtraTypesFile = JsonEncodedText.Encode("ExtraTypesFile");

	private static readonly JsonEncodedText PropName_Generatedcppdir = JsonEncodedText.Encode("Generatedcppdir");

	private static readonly JsonEncodedText PropName_SymbolsFolder = JsonEncodedText.Encode("SymbolsFolder");

	private static readonly JsonEncodedText PropName_ExecutableAssembliesFolderOnDevice = JsonEncodedText.Encode("ExecutableAssembliesFolderOnDevice");

	private static readonly JsonEncodedText PropName_EntryAssemblyName = JsonEncodedText.Encode("EntryAssemblyName");

	private static readonly JsonEncodedText PropName_DebugAssemblyName = JsonEncodedText.Encode("DebugAssemblyName");

	private static readonly JsonEncodedText PropName_DebugEnableAttach = JsonEncodedText.Encode("DebugEnableAttach");

	private static readonly JsonEncodedText PropName_DebugRiderInstallPath = JsonEncodedText.Encode("DebugRiderInstallPath");

	private static readonly JsonEncodedText PropName_DebugSolutionPath = JsonEncodedText.Encode("DebugSolutionPath");

	private static readonly JsonEncodedText PropName_EnableDotMemory = JsonEncodedText.Encode("EnableDotMemory");

	private static readonly JsonEncodedText PropName_DotMemoryOutputPath = JsonEncodedText.Encode("DotMemoryOutputPath");

	private static readonly JsonEncodedText PropName_DotMemoryCollectAllocations = JsonEncodedText.Encode("DotMemoryCollectAllocations");

	private static readonly JsonEncodedText PropName_EnableDotTrace = JsonEncodedText.Encode("EnableDotTrace");

	private static readonly JsonEncodedText PropName_DotTraceProfilingType = JsonEncodedText.Encode("DotTraceProfilingType");

	private static readonly JsonEncodedText PropName_DotTraceOutputPath = JsonEncodedText.Encode("DotTraceOutputPath");

	private static readonly JsonEncodedText PropName_AdditionalCpp = JsonEncodedText.Encode("AdditionalCpp");

	private static readonly JsonEncodedText PropName_EnableAnalytics = JsonEncodedText.Encode("EnableAnalytics");

	private static readonly JsonEncodedText PropName_EmitNullChecks = JsonEncodedText.Encode("EmitNullChecks");

	private static readonly JsonEncodedText PropName_EnableStacktrace = JsonEncodedText.Encode("EnableStacktrace");

	private static readonly JsonEncodedText PropName_EnableDeepProfiler = JsonEncodedText.Encode("EnableDeepProfiler");

	private static readonly JsonEncodedText PropName_EnableStats = JsonEncodedText.Encode("EnableStats");

	private static readonly JsonEncodedText PropName_EnableArrayBoundsCheck = JsonEncodedText.Encode("EnableArrayBoundsCheck");

	private static readonly JsonEncodedText PropName_EnableDivideByZeroCheck = JsonEncodedText.Encode("EnableDivideByZeroCheck");

	private static readonly JsonEncodedText PropName_EnableErrorMessageTest = JsonEncodedText.Encode("EnableErrorMessageTest");

	private static readonly JsonEncodedText PropName_EnablePrimitiveValueTypeGenericSharing = JsonEncodedText.Encode("EnablePrimitiveValueTypeGenericSharing");

	private static readonly JsonEncodedText PropName_ProfilerOptions = JsonEncodedText.Encode("ProfilerOptions");

	private static readonly JsonEncodedText PropName_EmitSourceMapping = JsonEncodedText.Encode("EmitSourceMapping");

	private static readonly JsonEncodedText PropName_EmitMethodMap = JsonEncodedText.Encode("EmitMethodMap");

	private static readonly JsonEncodedText PropName_EmitComments = JsonEncodedText.Encode("EmitComments");

	private static readonly JsonEncodedText PropName_NeverAttachDialog = JsonEncodedText.Encode("NeverAttachDialog");

	private static readonly JsonEncodedText PropName_EmitAttachDialog = JsonEncodedText.Encode("EmitAttachDialog");

	private static readonly JsonEncodedText PropName_CodeConversionCache = JsonEncodedText.Encode("CodeConversionCache");

	private static readonly JsonEncodedText PropName_AssemblyMethod = JsonEncodedText.Encode("AssemblyMethod");

	private static readonly JsonEncodedText PropName_DisableGenericSharing = JsonEncodedText.Encode("DisableGenericSharing");

	private static readonly JsonEncodedText PropName_EmitReversePInvokeWrapperDebuggingHelpers = JsonEncodedText.Encode("EmitReversePInvokeWrapperDebuggingHelpers");

	private static readonly JsonEncodedText PropName_MaximumRecursiveGenericDepth = JsonEncodedText.Encode("MaximumRecursiveGenericDepth");

	private static readonly JsonEncodedText PropName_GenericVirtualMethodIterations = JsonEncodedText.Encode("GenericVirtualMethodIterations");

	private static readonly JsonEncodedText PropName_ConversionMode = JsonEncodedText.Encode("ConversionMode");

	private static readonly JsonEncodedText PropName_CodeGenerationOption = JsonEncodedText.Encode("CodeGenerationOption");

	private static readonly JsonEncodedText PropName_FileGenerationOption = JsonEncodedText.Encode("FileGenerationOption");

	private static readonly JsonEncodedText PropName_GenericsOption = JsonEncodedText.Encode("GenericsOption");

	private static readonly JsonEncodedText PropName_Feature = JsonEncodedText.Encode("Feature");

	private static readonly JsonEncodedText PropName_DiagnosticOption = JsonEncodedText.Encode("DiagnosticOption");

	private static readonly JsonEncodedText PropName_TestingOption = JsonEncodedText.Encode("TestingOption");

	private static readonly JsonEncodedText PropName_StatsOutputDir = JsonEncodedText.Encode("StatsOutputDir");

	private static readonly JsonEncodedText PropName_Emscripten = JsonEncodedText.Encode("Emscripten");

	private static readonly JsonEncodedText PropName_Platform = JsonEncodedText.Encode("Platform");

	private static readonly JsonEncodedText PropName_Architecture = JsonEncodedText.Encode("Architecture");

	private static readonly JsonEncodedText PropName_Outputpath = JsonEncodedText.Encode("Outputpath");

	private static readonly JsonEncodedText PropName_DontDeployBaselib = JsonEncodedText.Encode("DontDeployBaselib");

	private static readonly JsonEncodedText PropName_Verbose = JsonEncodedText.Encode("Verbose");

	private static readonly JsonEncodedText PropName_ToolChainPath = JsonEncodedText.Encode("ToolChainPath");

	private static readonly JsonEncodedText PropName_Forcerebuild = JsonEncodedText.Encode("Forcerebuild");

	private static readonly JsonEncodedText PropName_DisableBeeBuilder = JsonEncodedText.Encode("DisableBeeBuilder");

	private static readonly JsonEncodedText PropName_Libil2cppStatic = JsonEncodedText.Encode("Libil2cppStatic");

	private static readonly JsonEncodedText PropName_DisableRuntimeLumping = JsonEncodedText.Encode("DisableRuntimeLumping");

	private static readonly JsonEncodedText PropName_Libil2cppCacheDirectory = JsonEncodedText.Encode("Libil2cppCacheDirectory");

	private static readonly JsonEncodedText PropName_IncludeFileNamesInHashes = JsonEncodedText.Encode("IncludeFileNamesInHashes");

	private static readonly JsonEncodedText PropName_UseDependenciesToolChain = JsonEncodedText.Encode("UseDependenciesToolChain");

	private static readonly JsonEncodedText PropName_BeeJobs = JsonEncodedText.Encode("BeeJobs");

	private static readonly JsonEncodedText PropName_SetEnvironmentVariables = JsonEncodedText.Encode("SetEnvironmentVariables");

	private static readonly JsonEncodedText PropName_BaselibDirectory = JsonEncodedText.Encode("BaselibDirectory");

	private static readonly JsonEncodedText PropName_AvoidDynamicLibraryCopy = JsonEncodedText.Encode("AvoidDynamicLibraryCopy");

	private static readonly JsonEncodedText PropName_SysrootPath = JsonEncodedText.Encode("SysrootPath");

	private static readonly JsonEncodedText PropName_AdditionalDefines = JsonEncodedText.Encode("AdditionalDefines");

	private static readonly JsonEncodedText PropName_AdditionalLibraries = JsonEncodedText.Encode("AdditionalLibraries");

	private static readonly JsonEncodedText PropName_AdditionalIncludeDirectories = JsonEncodedText.Encode("AdditionalIncludeDirectories");

	private static readonly JsonEncodedText PropName_AdditionalLinkDirectories = JsonEncodedText.Encode("AdditionalLinkDirectories");

	private static readonly JsonEncodedText PropName_CommandLog = JsonEncodedText.Encode("CommandLog");

	private static readonly JsonEncodedText PropName_Plugin = JsonEncodedText.Encode("Plugin");

	private static readonly JsonEncodedText PropName_TargetIsSimulator = JsonEncodedText.Encode("TargetIsSimulator");

	private static readonly JsonEncodedText PropName_OutputFileExtension = JsonEncodedText.Encode("OutputFileExtension");

	private static readonly JsonEncodedText PropName_Identifier = JsonEncodedText.Encode("Identifier");

	private static readonly JsonEncodedText PropName_DebuggerPort = JsonEncodedText.Encode("DebuggerPort");

	private static readonly JsonEncodedText PropName_IncrementalGCTimeSlice = JsonEncodedText.Encode("IncrementalGCTimeSlice");

	private static readonly JsonEncodedText PropName_RelativeDataPath = JsonEncodedText.Encode("RelativeDataPath");

	private static readonly JsonEncodedText PropName_DontLinkCrt = JsonEncodedText.Encode("DontLinkCrt");

	private static readonly JsonEncodedText PropName_TargetBitcode = JsonEncodedText.Encode("TargetBitcode");

	private static readonly JsonEncodedText PropName_EnableArmPacBti = JsonEncodedText.Encode("EnableArmPacBti");

	private static readonly JsonEncodedText PropName_Configuration = JsonEncodedText.Encode("Configuration");

	private static readonly JsonEncodedText PropName_TreatWarningsAsErrors = JsonEncodedText.Encode("TreatWarningsAsErrors");

	private static readonly JsonEncodedText PropName_CompilerFlags = JsonEncodedText.Encode("CompilerFlags");

	private static readonly JsonEncodedText PropName_LinkerFlags = JsonEncodedText.Encode("LinkerFlags");

	private static readonly JsonEncodedText PropName_LinkerFlagsFile = JsonEncodedText.Encode("LinkerFlagsFile");

	private static readonly JsonEncodedText PropName_BuildPackageForTesting = JsonEncodedText.Encode("BuildPackageForTesting");

	private static readonly JsonEncodedText PropName_TestPackageDataFiles = JsonEncodedText.Encode("TestPackageDataFiles");

	private static readonly JsonEncodedText PropName_JsPre = JsonEncodedText.Encode("JsPre");

	private static readonly JsonEncodedText PropName_JsLibraries = JsonEncodedText.Encode("JsLibraries");

	private static readonly JsonEncodedText PropName_EmscriptenTemp = JsonEncodedText.Encode("EmscriptenTemp");

	private static readonly JsonEncodedText PropName_EmscriptenCache = JsonEncodedText.Encode("EmscriptenCache");

	private static readonly JsonEncodedText PropName_Dotnetprofile = JsonEncodedText.Encode("Dotnetprofile");

	private static readonly JsonEncodedText PropName_DevelopmentMode = JsonEncodedText.Encode("DevelopmentMode");

	private static readonly JsonEncodedText PropName_EnableDebugger = JsonEncodedText.Encode("EnableDebugger");

	private static readonly JsonEncodedText PropName_GenerateUsymFile = JsonEncodedText.Encode("GenerateUsymFile");

	private static readonly JsonEncodedText PropName_UsymtoolPath = JsonEncodedText.Encode("UsymtoolPath");

	private static readonly JsonEncodedText PropName_DebuggerOff = JsonEncodedText.Encode("DebuggerOff");

	private static readonly JsonEncodedText PropName_WriteBarrierValidation = JsonEncodedText.Encode("WriteBarrierValidation");

	private static readonly JsonEncodedText PropName_EnableReload = JsonEncodedText.Encode("EnableReload");

	private static readonly JsonEncodedText PropName_GoogleBenchmark = JsonEncodedText.Encode("GoogleBenchmark");

	private static readonly JsonEncodedText PropName_Cachedirectory = JsonEncodedText.Encode("Cachedirectory");

	private static readonly JsonEncodedText PropName_ProfilerReport = JsonEncodedText.Encode("ProfilerReport");

	private static readonly JsonEncodedText PropName_ProfilerOutputFile = JsonEncodedText.Encode("ProfilerOutputFile");

	private static readonly JsonEncodedText PropName_ProfilerUseTraceEvents = JsonEncodedText.Encode("ProfilerUseTraceEvents");

	private static readonly JsonEncodedText PropName_PrintCommandLine = JsonEncodedText.Encode("PrintCommandLine");

	private static readonly JsonEncodedText PropName_ExternalLibIl2Cpp = JsonEncodedText.Encode("ExternalLibIl2Cpp");

	private static readonly JsonEncodedText PropName_StaticLibIl2Cpp = JsonEncodedText.Encode("StaticLibIl2Cpp");

	private static readonly JsonEncodedText PropName_DataFolder = JsonEncodedText.Encode("DataFolder");

	private static readonly JsonEncodedText PropName_Jobs = JsonEncodedText.Encode("Jobs");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = Create_Boolean(base.Options, makeReadOnly: true));

	public JsonTypeInfo<ProfilerOptions> ProfilerOptions => _ProfilerOptions ?? (_ProfilerOptions = Create_ProfilerOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<string> String => _String ?? (_String = Create_String(base.Options, makeReadOnly: true));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = Create_Int32(base.Options, makeReadOnly: true));

	public JsonTypeInfo<ConversionMode> ConversionMode => _ConversionMode ?? (_ConversionMode = Create_ConversionMode(base.Options, makeReadOnly: true));

	public JsonTypeInfo<CodeGenerationOptions> CodeGenerationOptions => _CodeGenerationOptions ?? (_CodeGenerationOptions = Create_CodeGenerationOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<FileGenerationOptions> FileGenerationOptions => _FileGenerationOptions ?? (_FileGenerationOptions = Create_FileGenerationOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<GenericsOptions> GenericsOptions => _GenericsOptions ?? (_GenericsOptions = Create_GenericsOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<Features> Features => _Features ?? (_Features = Create_Features(base.Options, makeReadOnly: true));

	public JsonTypeInfo<DiagnosticOptions> DiagnosticOptions => _DiagnosticOptions ?? (_DiagnosticOptions = Create_DiagnosticOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<TestingOptions> TestingOptions => _TestingOptions ?? (_TestingOptions = Create_TestingOptions(base.Options, makeReadOnly: true));

	public JsonTypeInfo<ConversionSettings> ConversionSettings => _ConversionSettings ?? (_ConversionSettings = Create_ConversionSettings(base.Options, makeReadOnly: true));

	public JsonTypeInfo<ConversionStatistics> ConversionStatistics => _ConversionStatistics ?? (_ConversionStatistics = Create_ConversionStatistics(base.Options, makeReadOnly: true));

	public JsonTypeInfo<string[]> StringArray => _StringArray ?? (_StringArray = Create_StringArray(base.Options, makeReadOnly: true));

	public JsonTypeInfo<ConversionRequest> ConversionRequest => _ConversionRequest ?? (_ConversionRequest = Create_ConversionRequest(base.Options, makeReadOnly: true));

	public JsonTypeInfo<BuildConfiguration> BuildConfiguration => _BuildConfiguration ?? (_BuildConfiguration = Create_BuildConfiguration(base.Options, makeReadOnly: true));

	public JsonTypeInfo<CompilationSettings> CompilationSettings => _CompilationSettings ?? (_CompilationSettings = Create_CompilationSettings(base.Options, makeReadOnly: true));

	public JsonTypeInfo<EmscriptenCompilationRequest> EmscriptenCompilationRequest => _EmscriptenCompilationRequest ?? (_EmscriptenCompilationRequest = Create_EmscriptenCompilationRequest(base.Options, makeReadOnly: true));

	public JsonTypeInfo<CommandLogMode> CommandLogMode => _CommandLogMode ?? (_CommandLogMode = Create_CommandLogMode(base.Options, makeReadOnly: true));

	public JsonTypeInfo<CompilationRequest> CompilationRequest => _CompilationRequest ?? (_CompilationRequest = Create_CompilationRequest(base.Options, makeReadOnly: true));

	public JsonTypeInfo<SettingsForConversionAndCompilation> SettingsForConversionAndCompilation => _SettingsForConversionAndCompilation ?? (_SettingsForConversionAndCompilation = Create_SettingsForConversionAndCompilation(base.Options, makeReadOnly: true));

	public JsonTypeInfo<Il2CppCommandLineArguments> Il2CppCommandLineArguments => _Il2CppCommandLineArguments ?? (_Il2CppCommandLineArguments = Create_Il2CppCommandLineArguments(base.Options, makeReadOnly: true));

	private static JsonSerializerOptions s_defaultOptions { get; } = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		IgnoreReadOnlyFields = false,
		IgnoreReadOnlyProperties = false,
		IncludeFields = true,
		WriteIndented = true
	};

	public static CommandLineJsonContext Default => s_defaultContext ?? (s_defaultContext = new CommandLineJsonContext(new JsonSerializerOptions(s_defaultOptions)));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<bool> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(bool))) == null) ? JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter) : JsonMetadataServices.CreateValueInfo<bool>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ProfilerOptions> Create_ProfilerOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<ProfilerOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(ProfilerOptions))) == null) ? JsonMetadataServices.CreateValueInfo<ProfilerOptions>(options, JsonMetadataServices.GetEnumConverter<ProfilerOptions>(options)) : JsonMetadataServices.CreateValueInfo<ProfilerOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<string> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(string))) == null) ? JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter) : JsonMetadataServices.CreateValueInfo<string>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<int> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(int))) == null) ? JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter) : JsonMetadataServices.CreateValueInfo<int>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ConversionMode> Create_ConversionMode(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<ConversionMode> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(ConversionMode))) == null) ? JsonMetadataServices.CreateValueInfo<ConversionMode>(options, JsonMetadataServices.GetEnumConverter<ConversionMode>(options)) : JsonMetadataServices.CreateValueInfo<ConversionMode>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<CodeGenerationOptions> Create_CodeGenerationOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<CodeGenerationOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(CodeGenerationOptions))) == null) ? JsonMetadataServices.CreateValueInfo<CodeGenerationOptions>(options, JsonMetadataServices.GetEnumConverter<CodeGenerationOptions>(options)) : JsonMetadataServices.CreateValueInfo<CodeGenerationOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<FileGenerationOptions> Create_FileGenerationOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<FileGenerationOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(FileGenerationOptions))) == null) ? JsonMetadataServices.CreateValueInfo<FileGenerationOptions>(options, JsonMetadataServices.GetEnumConverter<FileGenerationOptions>(options)) : JsonMetadataServices.CreateValueInfo<FileGenerationOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<GenericsOptions> Create_GenericsOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<GenericsOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(GenericsOptions))) == null) ? JsonMetadataServices.CreateValueInfo<GenericsOptions>(options, JsonMetadataServices.GetEnumConverter<GenericsOptions>(options)) : JsonMetadataServices.CreateValueInfo<GenericsOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<Features> Create_Features(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<Features> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Features))) == null) ? JsonMetadataServices.CreateValueInfo<Features>(options, JsonMetadataServices.GetEnumConverter<Features>(options)) : JsonMetadataServices.CreateValueInfo<Features>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<DiagnosticOptions> Create_DiagnosticOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<DiagnosticOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(DiagnosticOptions))) == null) ? JsonMetadataServices.CreateValueInfo<DiagnosticOptions>(options, JsonMetadataServices.GetEnumConverter<DiagnosticOptions>(options)) : JsonMetadataServices.CreateValueInfo<DiagnosticOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<TestingOptions> Create_TestingOptions(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<TestingOptions> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(TestingOptions))) == null) ? JsonMetadataServices.CreateValueInfo<TestingOptions>(options, JsonMetadataServices.GetEnumConverter<TestingOptions>(options)) : JsonMetadataServices.CreateValueInfo<TestingOptions>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ConversionSettings> Create_ConversionSettings(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<ConversionSettings> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(ConversionSettings))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ConversionSettings>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<ConversionSettings> objectInfo = new JsonObjectInfoValues<ConversionSettings>
			{
				ObjectCreator = () => new ConversionSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = ConversionSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void ConversionSettingsSerializeHandler(Utf8JsonWriter writer, ConversionSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_EmitNullChecks, value.EmitNullChecks);
		writer.WriteBoolean(PropName_EnableStacktrace, value.EnableStacktrace);
		writer.WriteBoolean(PropName_EnableDeepProfiler, value.EnableDeepProfiler);
		writer.WriteBoolean(PropName_EnableStats, value.EnableStats);
		writer.WriteBoolean(PropName_EnableArrayBoundsCheck, value.EnableArrayBoundsCheck);
		writer.WriteBoolean(PropName_EnableDivideByZeroCheck, value.EnableDivideByZeroCheck);
		writer.WriteBoolean(PropName_EnableErrorMessageTest, value.EnableErrorMessageTest);
		writer.WriteBoolean(PropName_EnablePrimitiveValueTypeGenericSharing, value.EnablePrimitiveValueTypeGenericSharing);
		writer.WritePropertyName(PropName_ProfilerOptions);
		JsonSerializer.Serialize(writer, value.ProfilerOptions, Default.ProfilerOptions);
		writer.WriteBoolean(PropName_EmitSourceMapping, value.EmitSourceMapping);
		writer.WriteBoolean(PropName_EmitMethodMap, value.EmitMethodMap);
		writer.WriteBoolean(PropName_EmitComments, value.EmitComments);
		writer.WriteBoolean(PropName_NeverAttachDialog, value.NeverAttachDialog);
		writer.WriteBoolean(PropName_EmitAttachDialog, value.EmitAttachDialog);
		writer.WriteBoolean(PropName_CodeConversionCache, value.CodeConversionCache);
		if (value.AssemblyMethod != null)
		{
			writer.WriteString(PropName_AssemblyMethod, value.AssemblyMethod);
		}
		writer.WriteBoolean(PropName_DisableGenericSharing, value.DisableGenericSharing);
		writer.WriteBoolean(PropName_EmitReversePInvokeWrapperDebuggingHelpers, value.EmitReversePInvokeWrapperDebuggingHelpers);
		writer.WriteNumber(PropName_MaximumRecursiveGenericDepth, value.MaximumRecursiveGenericDepth);
		writer.WriteNumber(PropName_GenericVirtualMethodIterations, value.GenericVirtualMethodIterations);
		writer.WritePropertyName(PropName_ConversionMode);
		JsonSerializer.Serialize(writer, value.ConversionMode, Default.ConversionMode);
		writer.WritePropertyName(PropName_CodeGenerationOption);
		JsonSerializer.Serialize(writer, value.CodeGenerationOption, Default.CodeGenerationOptions);
		writer.WritePropertyName(PropName_FileGenerationOption);
		JsonSerializer.Serialize(writer, value.FileGenerationOption, Default.FileGenerationOptions);
		writer.WritePropertyName(PropName_GenericsOption);
		JsonSerializer.Serialize(writer, value.GenericsOption, Default.GenericsOptions);
		writer.WritePropertyName(PropName_Feature);
		JsonSerializer.Serialize(writer, value.Feature, Default.Features);
		writer.WritePropertyName(PropName_DiagnosticOption);
		JsonSerializer.Serialize(writer, value.DiagnosticOption, Default.DiagnosticOptions);
		writer.WritePropertyName(PropName_TestingOption);
		JsonSerializer.Serialize(writer, value.TestingOption, Default.TestingOptions);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ConversionStatistics> Create_ConversionStatistics(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<ConversionStatistics> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(ConversionStatistics))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ConversionStatistics>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<ConversionStatistics> objectInfo = new JsonObjectInfoValues<ConversionStatistics>
			{
				ObjectCreator = () => new ConversionStatistics(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = ConversionStatisticsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void ConversionStatisticsSerializeHandler(Utf8JsonWriter writer, ConversionStatistics? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.StatsOutputDir != null)
		{
			writer.WriteString(PropName_StatsOutputDir, value.StatsOutputDir);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<string[]> Create_StringArray(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<string[]> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(string[]))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string[]>(options, customConverter);
		}
		else
		{
			JsonCollectionInfoValues<string[]> info = new JsonCollectionInfoValues<string[]>
			{
				ObjectCreator = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = StringArraySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateArrayInfo(options, info);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void StringArraySerializeHandler(Utf8JsonWriter writer, string[]? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Length; i++)
		{
			writer.WriteStringValue(value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<ConversionRequest> Create_ConversionRequest(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<ConversionRequest> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(ConversionRequest))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ConversionRequest>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<ConversionRequest> objectInfo = new JsonObjectInfoValues<ConversionRequest>
			{
				ObjectCreator = () => new ConversionRequest(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = ConversionRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void ConversionRequestSerializeHandler(Utf8JsonWriter writer, ConversionRequest? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.Settings != null)
		{
			writer.WritePropertyName(PropName_Settings);
			ConversionSettingsSerializeHandler(writer, value.Settings);
		}
		if (value.Statistics != null)
		{
			writer.WritePropertyName(PropName_Statistics);
			ConversionStatisticsSerializeHandler(writer, value.Statistics);
		}
		if (value.Assembly != null)
		{
			writer.WritePropertyName(PropName_Assembly);
			StringArraySerializeHandler(writer, value.Assembly);
		}
		if (value.Directory != null)
		{
			writer.WritePropertyName(PropName_Directory);
			StringArraySerializeHandler(writer, value.Directory);
		}
		if (value.ExtraTypesFile != null)
		{
			writer.WritePropertyName(PropName_ExtraTypesFile);
			StringArraySerializeHandler(writer, value.ExtraTypesFile);
		}
		if (value.Generatedcppdir != null)
		{
			writer.WriteString(PropName_Generatedcppdir, value.Generatedcppdir);
		}
		if (value.SymbolsFolder != null)
		{
			writer.WriteString(PropName_SymbolsFolder, value.SymbolsFolder);
		}
		if (value.ExecutableAssembliesFolderOnDevice != null)
		{
			writer.WriteString(PropName_ExecutableAssembliesFolderOnDevice, value.ExecutableAssembliesFolderOnDevice);
		}
		if (value.EntryAssemblyName != null)
		{
			writer.WriteString(PropName_EntryAssemblyName, value.EntryAssemblyName);
		}
		if (value.DebugAssemblyName != null)
		{
			writer.WritePropertyName(PropName_DebugAssemblyName);
			StringArraySerializeHandler(writer, value.DebugAssemblyName);
		}
		writer.WriteBoolean(PropName_DebugEnableAttach, value.DebugEnableAttach);
		if (value.DebugRiderInstallPath != null)
		{
			writer.WriteString(PropName_DebugRiderInstallPath, value.DebugRiderInstallPath);
		}
		if (value.DebugSolutionPath != null)
		{
			writer.WriteString(PropName_DebugSolutionPath, value.DebugSolutionPath);
		}
		writer.WriteBoolean(PropName_EnableDotMemory, value.EnableDotMemory);
		if (value.DotMemoryOutputPath != null)
		{
			writer.WriteString(PropName_DotMemoryOutputPath, value.DotMemoryOutputPath);
		}
		writer.WriteBoolean(PropName_DotMemoryCollectAllocations, value.DotMemoryCollectAllocations);
		writer.WriteBoolean(PropName_EnableDotTrace, value.EnableDotTrace);
		if (value.DotTraceProfilingType != null)
		{
			writer.WriteString(PropName_DotTraceProfilingType, value.DotTraceProfilingType);
		}
		if (value.DotTraceOutputPath != null)
		{
			writer.WriteString(PropName_DotTraceOutputPath, value.DotTraceOutputPath);
		}
		if (value.AdditionalCpp != null)
		{
			writer.WritePropertyName(PropName_AdditionalCpp);
			StringArraySerializeHandler(writer, value.AdditionalCpp);
		}
		writer.WriteBoolean(PropName_EnableAnalytics, value.EnableAnalytics);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<BuildConfiguration> Create_BuildConfiguration(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<BuildConfiguration> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(BuildConfiguration))) == null) ? JsonMetadataServices.CreateValueInfo<BuildConfiguration>(options, JsonMetadataServices.GetEnumConverter<BuildConfiguration>(options)) : JsonMetadataServices.CreateValueInfo<BuildConfiguration>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<CompilationSettings> Create_CompilationSettings(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<CompilationSettings> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(CompilationSettings))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<CompilationSettings>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<CompilationSettings> objectInfo = new JsonObjectInfoValues<CompilationSettings>
			{
				ObjectCreator = () => new CompilationSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = CompilationSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void CompilationSettingsSerializeHandler(Utf8JsonWriter writer, CompilationSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.OutputFileExtension != null)
		{
			writer.WriteString(PropName_OutputFileExtension, value.OutputFileExtension);
		}
		if (value.Identifier != null)
		{
			writer.WriteString(PropName_Identifier, value.Identifier);
		}
		writer.WriteNumber(PropName_DebuggerPort, value.DebuggerPort);
		writer.WriteNumber(PropName_IncrementalGCTimeSlice, value.IncrementalGCTimeSlice);
		if (value.RelativeDataPath != null)
		{
			writer.WriteString(PropName_RelativeDataPath, value.RelativeDataPath);
		}
		writer.WriteBoolean(PropName_DontLinkCrt, value.DontLinkCrt);
		writer.WriteBoolean(PropName_TargetBitcode, value.TargetBitcode);
		writer.WriteBoolean(PropName_EnableArmPacBti, value.EnableArmPacBti);
		writer.WritePropertyName(PropName_Configuration);
		JsonSerializer.Serialize(writer, value.Configuration, Default.BuildConfiguration);
		writer.WriteBoolean(PropName_TreatWarningsAsErrors, value.TreatWarningsAsErrors);
		if (value.CompilerFlags != null)
		{
			writer.WritePropertyName(PropName_CompilerFlags);
			StringArraySerializeHandler(writer, value.CompilerFlags);
		}
		if (value.LinkerFlags != null)
		{
			writer.WritePropertyName(PropName_LinkerFlags);
			StringArraySerializeHandler(writer, value.LinkerFlags);
		}
		if (value.LinkerFlagsFile != null)
		{
			writer.WriteString(PropName_LinkerFlagsFile, value.LinkerFlagsFile);
		}
		writer.WriteBoolean(PropName_BuildPackageForTesting, value.BuildPackageForTesting);
		if (value.TestPackageDataFiles != null)
		{
			writer.WritePropertyName(PropName_TestPackageDataFiles);
			StringArraySerializeHandler(writer, value.TestPackageDataFiles);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<EmscriptenCompilationRequest> Create_EmscriptenCompilationRequest(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<EmscriptenCompilationRequest> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(EmscriptenCompilationRequest))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<EmscriptenCompilationRequest>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<EmscriptenCompilationRequest> objectInfo = new JsonObjectInfoValues<EmscriptenCompilationRequest>
			{
				ObjectCreator = () => new EmscriptenCompilationRequest(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = EmscriptenCompilationRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void EmscriptenCompilationRequestSerializeHandler(Utf8JsonWriter writer, EmscriptenCompilationRequest? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.JsPre != null)
		{
			writer.WritePropertyName(PropName_JsPre);
			StringArraySerializeHandler(writer, value.JsPre);
		}
		if (value.JsLibraries != null)
		{
			writer.WritePropertyName(PropName_JsLibraries);
			StringArraySerializeHandler(writer, value.JsLibraries);
		}
		if (value.EmscriptenTemp != null)
		{
			writer.WriteString(PropName_EmscriptenTemp, value.EmscriptenTemp);
		}
		if (value.EmscriptenCache != null)
		{
			writer.WriteString(PropName_EmscriptenCache, value.EmscriptenCache);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<CommandLogMode> Create_CommandLogMode(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<CommandLogMode> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(CommandLogMode))) == null) ? JsonMetadataServices.CreateValueInfo<CommandLogMode>(options, JsonMetadataServices.GetEnumConverter<CommandLogMode>(options)) : JsonMetadataServices.CreateValueInfo<CommandLogMode>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private JsonTypeInfo<CompilationRequest> Create_CompilationRequest(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<CompilationRequest> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(CompilationRequest))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<CompilationRequest>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<CompilationRequest> objectInfo = new JsonObjectInfoValues<CompilationRequest>
			{
				ObjectCreator = () => new CompilationRequest(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = CompilationRequestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void CompilationRequestSerializeHandler(Utf8JsonWriter writer, CompilationRequest? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.Settings != null)
		{
			writer.WritePropertyName(PropName_Settings);
			CompilationSettingsSerializeHandler(writer, value.Settings);
		}
		if (value.Emscripten != null)
		{
			writer.WritePropertyName(PropName_Emscripten);
			EmscriptenCompilationRequestSerializeHandler(writer, value.Emscripten);
		}
		if (value.Platform != null)
		{
			writer.WriteString(PropName_Platform, value.Platform);
		}
		if (value.Architecture != null)
		{
			writer.WriteString(PropName_Architecture, value.Architecture);
		}
		if (value.Outputpath != null)
		{
			writer.WriteString(PropName_Outputpath, value.Outputpath);
		}
		writer.WriteBoolean(PropName_DontDeployBaselib, value.DontDeployBaselib);
		writer.WriteBoolean(PropName_Verbose, value.Verbose);
		if (value.ToolChainPath != null)
		{
			writer.WriteString(PropName_ToolChainPath, value.ToolChainPath);
		}
		writer.WriteBoolean(PropName_Forcerebuild, value.Forcerebuild);
		writer.WriteBoolean(PropName_DisableBeeBuilder, value.DisableBeeBuilder);
		writer.WriteBoolean(PropName_Libil2cppStatic, value.Libil2cppStatic);
		writer.WriteBoolean(PropName_DisableRuntimeLumping, value.DisableRuntimeLumping);
		if (value.Libil2cppCacheDirectory != null)
		{
			writer.WriteString(PropName_Libil2cppCacheDirectory, value.Libil2cppCacheDirectory);
		}
		writer.WriteBoolean(PropName_IncludeFileNamesInHashes, value.IncludeFileNamesInHashes);
		writer.WriteBoolean(PropName_UseDependenciesToolChain, value.UseDependenciesToolChain);
		writer.WriteNumber(PropName_BeeJobs, value.BeeJobs);
		writer.WriteBoolean(PropName_SetEnvironmentVariables, value.SetEnvironmentVariables);
		if (value.BaselibDirectory != null)
		{
			writer.WriteString(PropName_BaselibDirectory, value.BaselibDirectory);
		}
		writer.WriteBoolean(PropName_AvoidDynamicLibraryCopy, value.AvoidDynamicLibraryCopy);
		if (value.SysrootPath != null)
		{
			writer.WriteString(PropName_SysrootPath, value.SysrootPath);
		}
		if (value.AdditionalDefines != null)
		{
			writer.WritePropertyName(PropName_AdditionalDefines);
			StringArraySerializeHandler(writer, value.AdditionalDefines);
		}
		if (value.AdditionalLibraries != null)
		{
			writer.WritePropertyName(PropName_AdditionalLibraries);
			StringArraySerializeHandler(writer, value.AdditionalLibraries);
		}
		if (value.AdditionalIncludeDirectories != null)
		{
			writer.WritePropertyName(PropName_AdditionalIncludeDirectories);
			StringArraySerializeHandler(writer, value.AdditionalIncludeDirectories);
		}
		if (value.AdditionalLinkDirectories != null)
		{
			writer.WritePropertyName(PropName_AdditionalLinkDirectories);
			StringArraySerializeHandler(writer, value.AdditionalLinkDirectories);
		}
		writer.WritePropertyName(PropName_CommandLog);
		JsonSerializer.Serialize(writer, value.CommandLog, Default.CommandLogMode);
		if (value.Plugin != null)
		{
			writer.WriteString(PropName_Plugin, value.Plugin);
		}
		writer.WriteBoolean(PropName_TargetIsSimulator, value.TargetIsSimulator);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<SettingsForConversionAndCompilation> Create_SettingsForConversionAndCompilation(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<SettingsForConversionAndCompilation> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(SettingsForConversionAndCompilation))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<SettingsForConversionAndCompilation>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<SettingsForConversionAndCompilation> objectInfo = new JsonObjectInfoValues<SettingsForConversionAndCompilation>
			{
				ObjectCreator = () => new SettingsForConversionAndCompilation(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = SettingsForConversionAndCompilationSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void SettingsForConversionAndCompilationSerializeHandler(Utf8JsonWriter writer, SettingsForConversionAndCompilation? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.Dotnetprofile != null)
		{
			writer.WriteString(PropName_Dotnetprofile, value.Dotnetprofile);
		}
		writer.WriteBoolean(PropName_DevelopmentMode, value.DevelopmentMode);
		writer.WriteBoolean(PropName_EnableDebugger, value.EnableDebugger);
		writer.WriteBoolean(PropName_GenerateUsymFile, value.GenerateUsymFile);
		if (value.UsymtoolPath != null)
		{
			writer.WriteString(PropName_UsymtoolPath, value.UsymtoolPath);
		}
		writer.WriteBoolean(PropName_DebuggerOff, value.DebuggerOff);
		writer.WriteBoolean(PropName_WriteBarrierValidation, value.WriteBarrierValidation);
		writer.WriteBoolean(PropName_EnableReload, value.EnableReload);
		writer.WriteBoolean(PropName_GoogleBenchmark, value.GoogleBenchmark);
		if (value.Cachedirectory != null)
		{
			writer.WriteString(PropName_Cachedirectory, value.Cachedirectory);
		}
		writer.WriteBoolean(PropName_ProfilerReport, value.ProfilerReport);
		if (value.ProfilerOutputFile != null)
		{
			writer.WriteString(PropName_ProfilerOutputFile, value.ProfilerOutputFile);
		}
		writer.WriteBoolean(PropName_ProfilerUseTraceEvents, value.ProfilerUseTraceEvents);
		writer.WriteBoolean(PropName_PrintCommandLine, value.PrintCommandLine);
		if (value.ExternalLibIl2Cpp != null)
		{
			writer.WriteString(PropName_ExternalLibIl2Cpp, value.ExternalLibIl2Cpp);
		}
		writer.WriteBoolean(PropName_StaticLibIl2Cpp, value.StaticLibIl2Cpp);
		if (value.DataFolder != null)
		{
			writer.WriteString(PropName_DataFolder, value.DataFolder);
		}
		writer.WriteNumber(PropName_Jobs, value.Jobs);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<Il2CppCommandLineArguments> Create_Il2CppCommandLineArguments(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<Il2CppCommandLineArguments> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Il2CppCommandLineArguments))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Il2CppCommandLineArguments>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<Il2CppCommandLineArguments> objectInfo = new JsonObjectInfoValues<Il2CppCommandLineArguments>
			{
				ObjectCreator = () => new Il2CppCommandLineArguments(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = Il2CppCommandLineArgumentsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void Il2CppCommandLineArgumentsSerializeHandler(Utf8JsonWriter writer, Il2CppCommandLineArguments? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.ConversionRequest != null)
		{
			writer.WritePropertyName(PropName_ConversionRequest);
			ConversionRequestSerializeHandler(writer, value.ConversionRequest);
		}
		if (value.CompilationRequest != null)
		{
			writer.WritePropertyName(PropName_CompilationRequest);
			CompilationRequestSerializeHandler(writer, value.CompilationRequest);
		}
		if (value.SettingsForConversionAndCompilation != null)
		{
			writer.WritePropertyName(PropName_SettingsForConversionAndCompilation);
			SettingsForConversionAndCompilationSerializeHandler(writer, value.SettingsForConversionAndCompilation);
		}
		writer.WriteBoolean(PropName_ConvertToCpp, value.ConvertToCpp);
		writer.WriteBoolean(PropName_CompileCpp, value.CompileCpp);
		writer.WriteBoolean(PropName_ConvertInGraph, value.ConvertInGraph);
		if (value.CustomIl2CppRoot != null)
		{
			writer.WriteString(PropName_CustomIl2CppRoot, value.CustomIl2CppRoot);
		}
		writer.WriteEndObject();
	}

	public CommandLineJsonContext()
		: base(null)
	{
	}

	public CommandLineJsonContext(JsonSerializerOptions options)
		: base(options)
	{
	}

	private static JsonConverter? GetRuntimeProvidedCustomConverter(JsonSerializerOptions options, Type type)
	{
		IList<JsonConverter> converters = options.Converters;
		for (int i = 0; i < converters.Count; i++)
		{
			JsonConverter converter = converters[i];
			if (!converter.CanConvert(type))
			{
				continue;
			}
			if (converter is JsonConverterFactory factory)
			{
				converter = factory.CreateConverter(type, options);
				if (converter == null || converter is JsonConverterFactory)
				{
					throw new InvalidOperationException($"The converter '{factory.GetType()}' cannot return null or a JsonConverterFactory instance.");
				}
			}
			return converter;
		}
		return null;
	}

	public override JsonTypeInfo GetTypeInfo(Type type)
	{
		if (type == typeof(Il2CppCommandLineArguments))
		{
			return Il2CppCommandLineArguments;
		}
		if (type == typeof(ConversionRequest))
		{
			return ConversionRequest;
		}
		if (type == typeof(ConversionSettings))
		{
			return ConversionSettings;
		}
		if (type == typeof(bool))
		{
			return Boolean;
		}
		if (type == typeof(ProfilerOptions))
		{
			return ProfilerOptions;
		}
		if (type == typeof(string))
		{
			return String;
		}
		if (type == typeof(int))
		{
			return Int32;
		}
		if (type == typeof(ConversionMode))
		{
			return ConversionMode;
		}
		if (type == typeof(CodeGenerationOptions))
		{
			return CodeGenerationOptions;
		}
		if (type == typeof(FileGenerationOptions))
		{
			return FileGenerationOptions;
		}
		if (type == typeof(GenericsOptions))
		{
			return GenericsOptions;
		}
		if (type == typeof(Features))
		{
			return Features;
		}
		if (type == typeof(DiagnosticOptions))
		{
			return DiagnosticOptions;
		}
		if (type == typeof(TestingOptions))
		{
			return TestingOptions;
		}
		if (type == typeof(ConversionStatistics))
		{
			return ConversionStatistics;
		}
		if (type == typeof(string[]))
		{
			return StringArray;
		}
		if (type == typeof(CompilationRequest))
		{
			return CompilationRequest;
		}
		if (type == typeof(CompilationSettings))
		{
			return CompilationSettings;
		}
		if (type == typeof(BuildConfiguration))
		{
			return BuildConfiguration;
		}
		if (type == typeof(EmscriptenCompilationRequest))
		{
			return EmscriptenCompilationRequest;
		}
		if (type == typeof(CommandLogMode))
		{
			return CommandLogMode;
		}
		if (type == typeof(SettingsForConversionAndCompilation))
		{
			return SettingsForConversionAndCompilation;
		}
		return null;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(Il2CppCommandLineArguments))
		{
			return Create_Il2CppCommandLineArguments(options, makeReadOnly: false);
		}
		if (type == typeof(ConversionRequest))
		{
			return Create_ConversionRequest(options, makeReadOnly: false);
		}
		if (type == typeof(ConversionSettings))
		{
			return Create_ConversionSettings(options, makeReadOnly: false);
		}
		if (type == typeof(bool))
		{
			return Create_Boolean(options, makeReadOnly: false);
		}
		if (type == typeof(ProfilerOptions))
		{
			return Create_ProfilerOptions(options, makeReadOnly: false);
		}
		if (type == typeof(string))
		{
			return Create_String(options, makeReadOnly: false);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options, makeReadOnly: false);
		}
		if (type == typeof(ConversionMode))
		{
			return Create_ConversionMode(options, makeReadOnly: false);
		}
		if (type == typeof(CodeGenerationOptions))
		{
			return Create_CodeGenerationOptions(options, makeReadOnly: false);
		}
		if (type == typeof(FileGenerationOptions))
		{
			return Create_FileGenerationOptions(options, makeReadOnly: false);
		}
		if (type == typeof(GenericsOptions))
		{
			return Create_GenericsOptions(options, makeReadOnly: false);
		}
		if (type == typeof(Features))
		{
			return Create_Features(options, makeReadOnly: false);
		}
		if (type == typeof(DiagnosticOptions))
		{
			return Create_DiagnosticOptions(options, makeReadOnly: false);
		}
		if (type == typeof(TestingOptions))
		{
			return Create_TestingOptions(options, makeReadOnly: false);
		}
		if (type == typeof(ConversionStatistics))
		{
			return Create_ConversionStatistics(options, makeReadOnly: false);
		}
		if (type == typeof(string[]))
		{
			return Create_StringArray(options, makeReadOnly: false);
		}
		if (type == typeof(CompilationRequest))
		{
			return Create_CompilationRequest(options, makeReadOnly: false);
		}
		if (type == typeof(CompilationSettings))
		{
			return Create_CompilationSettings(options, makeReadOnly: false);
		}
		if (type == typeof(BuildConfiguration))
		{
			return Create_BuildConfiguration(options, makeReadOnly: false);
		}
		if (type == typeof(EmscriptenCompilationRequest))
		{
			return Create_EmscriptenCompilationRequest(options, makeReadOnly: false);
		}
		if (type == typeof(CommandLogMode))
		{
			return Create_CommandLogMode(options, makeReadOnly: false);
		}
		if (type == typeof(SettingsForConversionAndCompilation))
		{
			return Create_SettingsForConversionAndCompilation(options, makeReadOnly: false);
		}
		return null;
	}
}
