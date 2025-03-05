using System;
using System.IO;
using System.Text.Json;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.Common;
using Unity.TinyProfiler;

namespace il2cpp.Compilation;

public class CompilationDriver
{
	private static bool UseInProcessCompile => false;

	public static void Run(TinyProfiler2 tinyProfiler, Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		if (UseInProcessCompile)
		{
			RunInProcess(tinyProfiler, il2CppCommandLineArguments);
		}
		else
		{
			RunOutOfProcess(tinyProfiler, il2CppCommandLineArguments);
		}
	}

	private static void RunInProcess(TinyProfiler2 tinyProfiler, Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		throw new NotSupportedException("Using il2cpp-compile in process is not supported in this build of il2cpp");
	}

	private static void RunOutOfProcess(TinyProfiler2 tinyProfiler, Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		NPath executable = LocateCompileExecutable();
		NPath dataFilePath;
		using (tinyProfiler.Section("Write Compile Data"))
		{
			dataFilePath = WriteCompileData(il2CppCommandLineArguments);
		}
		string il2cppInvocationString = DetermineIL2CPPInvocationString();
		string arguments = $"{dataFilePath.InQuotes()} {il2cppInvocationString} {FindDirectoryHoldingIl2Cpp().InQuotes()} {il2CppCommandLineArguments.DetermineDistributionDirectory().InQuotes()}";
		Shell.ExecuteResult result = Shell.ExecuteWithLiveOutput(new Shell.ExecuteArgs
		{
			Executable = executable,
			Arguments = arguments
		});
		if (!string.IsNullOrEmpty(il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerOutputFile))
		{
			NPath compileTraceEventsPath = DetermineCompileProfilerOutputPath(il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerOutputFile.ToNPath());
			if (compileTraceEventsPath.FileExists())
			{
				tinyProfiler.AddExternalTraceEventsFile(compileTraceEventsPath);
			}
		}
		if (result.ExitCode != 0)
		{
			throw new BuilderFailedException("Build failed :\n " + result.StdOut + "\n" + result.StdErr);
		}
	}

	private static NPath DetermineCompileProfilerOutputPath(NPath conversionProfilerOutputPath)
	{
		return conversionProfilerOutputPath.Parent.Combine("il2cpp-compile.traceevents");
	}

	private static NPath FindDirectoryHoldingIl2Cpp()
	{
		string executableName = (PlatformUtils.IsWindows() ? "il2cpp.exe" : "il2cpp");
		NPath candidate = AppContext.BaseDirectory.ToNPath().Combine(executableName);
		if (candidate.FileExists())
		{
			return candidate.Parent;
		}
		throw new FileNotFoundException($"The directory {AppContext.BaseDirectory} was expected to contain {executableName} but it did not");
	}

	private static NPath WriteCompileData(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		NPath outputPath = il2CppCommandLineArguments.ConversionRequest.Generatedcppdir.ToNPath().MakeAbsolute().Combine("compile-data.json");
		using FileStream stream = new FileStream(outputPath, FileMode.Create);
		using Utf8JsonWriter writer = new Utf8JsonWriter((Stream)stream, new JsonWriterOptions
		{
			Indented = true
		});
		JsonSerializer.Serialize(writer, il2CppCommandLineArguments, typeof(Il2CppCommandLineArguments), CommandLineJsonContext.Default);
		return outputPath;
	}

	private static NPath LocateCompileExecutable()
	{
		string name = "il2cpp-compile";
		NPath candidate = AppContext.BaseDirectory.ToNPath().Combine(PlatformUtils.IsWindows() ? (name + ".exe") : name);
		if (candidate.FileExists())
		{
			return candidate;
		}
		candidate = AssemblyPathTools.GetPublishedExecutablePath(name);
		candidate = AssemblyPathTools.GetExecutablePath(name);
		if (candidate.FileExists())
		{
			return candidate;
		}
		throw new FileNotFoundException(name + " does not exist next to il2cpp in " + AppContext.BaseDirectory);
	}

	private static string DetermineIL2CPPInvocationString()
	{
		NPath il2cppAssembly = AppContext.BaseDirectory.ToNPath().Combine("il2cpp.dll");
		NPath nativeBinary = il2cppAssembly.ChangeExtension(PlatformUtils.IsWindows() ? "exe" : "");
		if (!nativeBinary.FileExists())
		{
			return NetCoreInstall.DotNetExe.InQuotes(SlashMode.Native) + " exec " + il2cppAssembly.InQuotes();
		}
		return nativeBinary.InQuotes(SlashMode.Native);
	}
}
