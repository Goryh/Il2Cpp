using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using il2cpp.Compilation;
using il2cpp.Conversion;
using il2cpp.EditorIntegration;
using JetBrains.Profiler.Api;
using NiceIO;
using Unity.Api.Attributes;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Common;
using Unity.Options;
using Unity.TinyProfiler;

namespace il2cpp;

public class Program
{
	public const string ProcessID_IL2CPPInGraph = "il2cpp_in_graph";

	public static int Main(string[] args)
	{
		Thread.CurrentThread.Name = "Main thread";
		try
		{
			return (int)Run(args, setInvariantCulture: false, throwExceptions: false);
		}
		catch (Exception value)
		{
			Console.Error.WriteLine($"Unhandled exception: {value}");
			return -1;
		}
	}

	public static ExitCode Run(string[] args, bool setInvariantCulture, bool throwExceptions = true)
	{
		using TinyProfiler2 tinyProfiler = new TinyProfiler2();
		Il2CppCommandLineArguments il2CppCommandLineArguments = null;
		try
		{
			using (tinyProfiler.AnalyticsSection("il2cpp.exe"))
			{
				bool continueToRun;
				ExitCode exitCode;
				List<NPath> foundAssemblies;
				using (tinyProfiler.Section("ParseArguments"))
				{
					Il2CppOptionParser.ParseArguments(args, out continueToRun, out exitCode, out il2CppCommandLineArguments, out foundAssemblies);
				}
				using (tinyProfiler.Section("RegisterRuntimeEventListeners"))
				{
					tinyProfiler.RegisterRuntimeEventListeners();
				}
				if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.PrintCommandLine)
				{
					ConsoleOutput.Info.WriteLine(Process.GetCurrentProcess().MainModule.FileName + " " + args.AggregateWith(" "));
				}
				if (setInvariantCulture)
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				}
				if (il2CppCommandLineArguments.ConversionRequest.DebugEnableAttach)
				{
					DebugAttacher.AttachToCurrentProcess(il2CppCommandLineArguments.ConversionRequest.DebugRiderInstallPath, il2CppCommandLineArguments.ConversionRequest.DebugSolutionPath);
				}
				Il2CppOptionParser.ParseArgumentsStep2(il2CppCommandLineArguments, foundAssemblies);
				if (il2CppCommandLineArguments.ConversionRequest.DotMemoryCollectAllocations)
				{
					MemoryProfiler.CollectAllocations(enable: true);
				}
				if (!continueToRun)
				{
					return exitCode;
				}
				if (RunDotTraceProfilingIfEnabled(il2CppCommandLineArguments, out var result))
				{
					return result;
				}
				if (RunDotMemoryProfilingIfEnabled(il2CppCommandLineArguments, out result))
				{
					return result;
				}
				return DoRun(tinyProfiler, args, il2CppCommandLineArguments, throwExceptions);
			}
		}
		finally
		{
			using (tinyProfiler.Section("UnregisterRuntimeEventListeners"))
			{
				tinyProfiler.UnregisterRuntimeEventListeners();
			}
			if (il2CppCommandLineArguments != null)
			{
				NPath profilerOutputFile = il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerOutputFile?.ToNPath();
				if (profilerOutputFile != null)
				{
					string name;
					int sortIndex;
					if (!profilerOutputFile.FileName.EndsWith("traceevents"))
					{
						name = "il2cpp_outer";
						sortIndex = -1;
					}
					else
					{
						name = "il2cpp_in_graph";
						sortIndex = 2;
					}
					profilerOutputFile = profilerOutputFile.MakeAbsolute();
					profilerOutputFile.Parent.EnsureDirectoryExists();
					tinyProfiler.WriteAndFinish(profilerOutputFile, name, sortIndex);
				}
			}
		}
	}

	private static bool RunDotTraceProfilingIfEnabled(Il2CppCommandLineArguments il2CppCommandLineArguments, out ExitCode exitCode)
	{
		if (il2CppCommandLineArguments.ConversionRequest.EnableDotTrace && !il2CppCommandLineArguments.ConvertInGraph)
		{
			string selfFlagName = OptionsFormatter.NameFor<ConversionRequest>("EnableDotTrace");
			bool num = Assembly.GetEntryAssembly() == Assembly.GetExecutingAssembly();
			string arguments = (from a in OptionObjectsGraph.ToCommandLine(il2CppCommandLineArguments)
				where a != selfFlagName
				select a).AggregateWithSpace();
			NPath executable;
			if (!num)
			{
				executable = NetCoreInstall.DotNetExe;
				arguments = $"{AssemblyPathTools.GetLibraryPathWithSameTFMAsCurrentProcess("il2cpp")} {arguments}";
			}
			else
			{
				executable = DotTraceProfiler.AttemptToGetExecutableOrLibraryPathForDotNetRanApplication("il2cpp.exe");
				if (executable.ExtensionWithDot == ".dll")
				{
					arguments = $"{executable} {arguments}";
					executable = NetCoreInstall.DotNetExe;
				}
			}
			NPath outputPath = il2CppCommandLineArguments.ConversionRequest.DotTraceOutputPath?.ToNPath() ?? il2CppCommandLineArguments.ConversionRequest.Generatedcppdir.ToNPath().Parent.EnsureDirectoryExists().Combine("dotTrace.dtt");
			if (outputPath.IsRelative)
			{
				outputPath = il2CppCommandLineArguments.ConversionRequest.Generatedcppdir.ToNPath().Combine(outputPath);
			}
			bool success = DotTraceProfiler.Run(outputPath, il2CppCommandLineArguments.ConversionRequest.DotTraceProfilingType, executable, arguments);
			exitCode = ((!success) ? ExitCode.UnexpectedError : ExitCode.Success);
			return true;
		}
		exitCode = ExitCode.Success;
		return false;
	}

	private static bool RunDotMemoryProfilingIfEnabled(Il2CppCommandLineArguments il2CppCommandLineArguments, out ExitCode exitCode)
	{
		if (il2CppCommandLineArguments.ConversionRequest.EnableDotMemory && !il2CppCommandLineArguments.ConvertInGraph)
		{
			string selfFlagName = OptionsFormatter.NameFor<ConversionRequest>("EnableDotMemory");
			bool num = Assembly.GetEntryAssembly() == Assembly.GetExecutingAssembly();
			string arguments = (from a in OptionObjectsGraph.ToCommandLine(il2CppCommandLineArguments)
				where a != selfFlagName
				select a).AggregateWithSpace();
			NPath executable;
			if (!num)
			{
				executable = NetCoreInstall.DotNetExe;
				arguments = $"{AssemblyPathTools.GetLibraryPathWithSameTFMAsCurrentProcess("il2cpp")} {arguments}";
			}
			else
			{
				executable = DotTraceProfiler.AttemptToGetExecutableOrLibraryPathForDotNetRanApplication("il2cpp.exe");
				if (executable.ExtensionWithDot == ".dll")
				{
					NPath exePath = executable.ChangeExtension(PlatformUtils.IsWindows() ? "exe" : string.Empty);
					if (exePath.FileExists())
					{
						executable = exePath;
					}
					else
					{
						arguments = $"{executable} {arguments}";
						executable = NetCoreInstall.DotNetExe;
					}
				}
			}
			NPath outputPath = il2CppCommandLineArguments.ConversionRequest.DotMemoryOutputPath?.ToNPath() ?? il2CppCommandLineArguments.ConversionRequest.Generatedcppdir.ToNPath().Combine("dotMemory.dmw");
			if (outputPath.IsRelative)
			{
				outputPath = il2CppCommandLineArguments.ConversionRequest.Generatedcppdir.ToNPath().Combine(outputPath);
			}
			bool success = DotTraceProfiler.RunMemory(outputPath, executable, arguments);
			exitCode = ((!success) ? ExitCode.UnexpectedError : ExitCode.Success);
			return true;
		}
		exitCode = ExitCode.Success;
		return false;
	}

	private static ExitCode DoRun(TinyProfiler2 tinyProfiler, string[] args, Il2CppCommandLineArguments il2CppCommandLineArguments, bool throwExceptions)
	{
		ConversionRequest conversionRequest = il2CppCommandLineArguments.ConversionRequest;
		NPath absoluteGeneratedCppDirectory = conversionRequest.Generatedcppdir.ToNPath().MakeAbsolute().EnsureDirectoryExists();
		using (Il2CppEditorDataGenerator editorDataGenerator = new Il2CppEditorDataGenerator(args, absoluteGeneratedCppDirectory ?? ((NPath)Directory.GetCurrentDirectory())))
		{
			try
			{
				ConversionResults conversionResults = null;
				bool willImmediatelyConvert = il2CppCommandLineArguments.ConvertToCpp && !il2CppCommandLineArguments.ConvertInGraph;
				try
				{
					if (willImmediatelyConvert)
					{
						conversionResults = ConversionDriver.Run(tinyProfiler, il2CppCommandLineArguments);
						if (conversionResults.LoggedMessages != null)
						{
							editorDataGenerator.LogFromMessages(conversionResults.LoggedMessages);
						}
						EmitStats(conversionResults, conversionRequest, il2CppCommandLineArguments);
						if (!il2CppCommandLineArguments.CompileCpp)
						{
							return ExitCode.Success;
						}
					}
					PostProcessBuildingOptions(il2CppCommandLineArguments);
					CompilationDriver.Run(tinyProfiler, il2CppCommandLineArguments);
				}
				finally
				{
					using (tinyProfiler.Section("Write Analytics"))
					{
						AnalyticsWriter.Write(conversionRequest.Generatedcppdir.ToNPath().MakeAbsolute(), conversionResults?.AnalyticsTable, il2CppCommandLineArguments);
					}
				}
			}
			catch (Exception ex)
			{
				editorDataGenerator.LogException(ex);
				if (throwExceptions)
				{
					throw;
				}
				return ExceptionToExitCode(ex);
			}
		}
		return ExitCode.Success;
	}

	private static void EmitStats(ConversionResults conversionResults, ConversionRequest conversionRequest, Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		if (conversionRequest.Settings.EnableStats)
		{
			StatisticsGenerator.WriteStatsLog(ConsoleOutput.Info.Stdout, conversionResults?.Stats);
			StatisticsGenerator.Generate(il2CppCommandLineArguments, conversionResults?.Stats);
		}
	}

	private static void PostProcessBuildingOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.WriteBarrierValidation && il2CppCommandLineArguments.CompilationRequest.Settings.IncrementalGCTimeSlice == 0)
		{
			throw new ArgumentException("WriteBarrierValidation requires a non 0 value for IncrementalGCTimeSlice");
		}
	}

	private static ExitCode ExceptionToExitCode(Exception ex)
	{
		if (!(ex is UserMessageException))
		{
			if (!(ex is PathTooLongException))
			{
				if (ex is BuilderFailedException)
				{
					return ExitCode.BuilderError;
				}
				return ExitCode.UnexpectedError;
			}
			return ExitCode.PathTooLong;
		}
		return ExitCode.UserErrorMessage;
	}
}
