using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.Options;

namespace il2cpp.Conversion;

internal static class ContextDataFactory
{
	public static AssemblyConversionInputData CreateConversionDataFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		ConversionRequest conversionRequest = il2CppCommandLineArguments.ConversionRequest;
		ConversionSettings conversionSettings = conversionRequest.Settings;
		if (conversionRequest.Directory.Length == 0 && conversionRequest.Assembly.Length == 0)
		{
			throw new InvalidCommandLineArgumentsException("One or more assemblies must be specified using either " + OptionsFormatter.NameFor<ConversionRequest>("Directory") + " or " + OptionsFormatter.NameFor<ConversionRequest>("Assembly"));
		}
		ReadOnlyCollection<NPath> assembliesToConvert = CombineAssembliesFrom(conversionRequest.Directory.ToNPaths(), conversionRequest.Assembly.ToNPaths()).AsReadOnly();
		if (assembliesToConvert.Count == 0 && conversionRequest.Directory.Length != 0 && conversionRequest.Assembly.Length == 0)
		{
			throw new InvalidCommandLineArgumentsException("No assemblies found after searching for assemblies in the following directories:\n" + conversionRequest.Directory.AggregateWithNewLine());
		}
		return new AssemblyConversionInputData(conversionRequest.Generatedcppdir.ToNPath().MakeAbsolute(), il2CppCommandLineArguments.SettingsForConversionAndCompilation.DataFolder.ToNPath().MakeAbsolute(), conversionRequest.SymbolsFolder.ToNPath().MakeAbsolute(), il2CppCommandLineArguments.SettingsForConversionAndCompilation.DataFolder.ToNPath().MakeAbsolute().Combine("Metadata"), conversionRequest.ExecutableAssembliesFolderOnDevice, il2CppCommandLineArguments.ConversionRequest.Statistics.StatsOutputDir.ToNPath().MakeAbsolute(), conversionRequest.EntryAssemblyName, conversionRequest.ExtraTypesFile.ToNPaths().ToArray(), Profile.GetRuntimeProfile(il2CppCommandLineArguments.SettingsForConversionAndCompilation.Dotnetprofile), assembliesToConvert, conversionRequest.Settings.MaximumRecursiveGenericDepth, conversionRequest.Settings.GenericVirtualMethodIterations, conversionSettings.AssemblyMethod, il2CppCommandLineArguments.SettingsForConversionAndCompilation.Jobs, conversionRequest.DebugAssemblyName, conversionRequest.AdditionalCpp.ToNPaths().ToArray(), il2CppCommandLineArguments.DetermineDistributionDirectory());
	}

	public static AssemblyConversionParameters CreateConversionParametersFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		return new AssemblyConversionParameters(CreateCodeGenerationOptionsFromOptions(il2CppCommandLineArguments), CreateFileGenerationOptionsFromOptions(il2CppCommandLineArguments), CreateGenericsOptionsFromOptions(il2CppCommandLineArguments), il2CppCommandLineArguments.ConversionRequest.Settings.ProfilerOptions, RuntimeBackend.Big, Profile.GetRuntimeProfile(il2CppCommandLineArguments.SettingsForConversionAndCompilation.Dotnetprofile), CreateDiagnosticOptionsFromOptions(il2CppCommandLineArguments), CreateFeaturesFromOptions(il2CppCommandLineArguments), CreateTestingOptionsFromOptions(il2CppCommandLineArguments));
	}

	public static AssemblyConversionInputDataForTopLevelAccess CreateTopLevelDataFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		return new AssemblyConversionInputDataForTopLevelAccess(il2CppCommandLineArguments.ConversionRequest.Settings.ConversionMode);
	}

	private static CodeGenerationOptions CreateCodeGenerationOptionsFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		CodeGenerationOptions options = il2CppCommandLineArguments.ConversionRequest.Settings.CodeGenerationOption;
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnableStacktrace)
		{
			options |= CodeGenerationOptions.EnableStacktrace;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnableArrayBoundsCheck && !il2CppCommandLineArguments.ConversionRequest.Settings.DisableArrayBoundsCheck)
		{
			options |= CodeGenerationOptions.EnableArrayBoundsCheck;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnableDivideByZeroCheck)
		{
			options |= CodeGenerationOptions.EnableDivideByZeroCheck;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EmitNullChecks && !il2CppCommandLineArguments.ConversionRequest.Settings.DisableNullChecks)
		{
			options |= CodeGenerationOptions.EnableNullChecks;
		}
		options |= CodeGenerationOptions.EnableLazyStaticConstructors;
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EmitComments)
		{
			options |= CodeGenerationOptions.EnableComments;
		}
		return options;
	}

	private static FileGenerationOptions CreateFileGenerationOptionsFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		FileGenerationOptions options = il2CppCommandLineArguments.ConversionRequest.Settings.FileGenerationOption;
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EmitSourceMapping)
		{
			options |= FileGenerationOptions.EmitSourceMapping;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EmitMethodMap)
		{
			options |= FileGenerationOptions.EmitMethodMap;
		}
		return options;
	}

	private static GenericsOptions CreateGenericsOptionsFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		if (il2CppCommandLineArguments.ConversionRequest.Settings.DisableGenericSharing)
		{
			return GenericsOptions.None;
		}
		GenericsOptions options = il2CppCommandLineArguments.ConversionRequest.Settings.GenericsOption | GenericsOptions.EnableSharing;
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnablePrimitiveValueTypeGenericSharing)
		{
			options |= GenericsOptions.EnablePrimitiveValueTypeGenericSharing;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnablePrimitiveValueTypeGenericSharing && Profile.IsUnityAotProfile(il2CppCommandLineArguments.SettingsForConversionAndCompilation.Dotnetprofile))
		{
			options |= GenericsOptions.EnableEnumTypeSharing;
		}
		return options;
	}

	internal static DiagnosticOptions CreateDiagnosticOptionsFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		ConversionSettings settings = il2CppCommandLineArguments.ConversionRequest.Settings;
		DiagnosticOptions options = settings.DiagnosticOption;
		if (settings.EnableStats)
		{
			options |= DiagnosticOptions.EnableStats;
		}
		if (settings.NeverAttachDialog)
		{
			options |= DiagnosticOptions.NeverAttachDialog;
		}
		if (settings.EmitAttachDialog)
		{
			options |= DiagnosticOptions.EmitAttachDialog;
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.DebuggerOff)
		{
			options |= DiagnosticOptions.DebuggerOff;
		}
		if (settings.EmitReversePInvokeWrapperDebuggingHelpers)
		{
			options |= DiagnosticOptions.EmitReversePInvokeWrapperDebuggingHelpers;
		}
		return options;
	}

	internal static Features CreateFeaturesFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		Features value = il2CppCommandLineArguments.ConversionRequest.Settings.Feature;
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.EnableReload)
		{
			value |= Features.EnableReload;
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.EnableDebugger)
		{
			value |= Features.EnableDebugger;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.CodeConversionCache)
		{
			value |= Features.EnableCodeConversionCache;
		}
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnableDeepProfiler)
		{
			value |= Features.EnableDeepProfiler;
		}
		if (il2CppCommandLineArguments.ConversionRequest.EnableAnalytics)
		{
			value |= Features.EnableAnalytics;
		}
		return value;
	}

	internal static TestingOptions CreateTestingOptionsFromOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		TestingOptions options = il2CppCommandLineArguments.ConversionRequest.Settings.TestingOption;
		if (il2CppCommandLineArguments.ConversionRequest.Settings.EnableErrorMessageTest)
		{
			options |= TestingOptions.EnableErrorMessageTest;
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.GoogleBenchmark)
		{
			options |= TestingOptions.EnableGoogleBenchmark;
		}
		return options;
	}

	private static List<NPath> CombineAssembliesFrom(IEnumerable<NPath> assemblyDirectories, IEnumerable<NPath> explicitAssemblies)
	{
		HashSet<NPath> allAssemblies = new HashSet<NPath>();
		if (assemblyDirectories != null)
		{
			foreach (NPath path in assemblyDirectories.SelectMany((NPath directory) => from f in directory.Files()
				where f.HasExtension("dll", "exe", "winmd")
				select f))
			{
				allAssemblies.Add(path);
			}
		}
		if (explicitAssemblies != null)
		{
			foreach (NPath path2 in explicitAssemblies)
			{
				allAssemblies.Add(path2);
			}
		}
		return allAssemblies.ToList();
	}
}
