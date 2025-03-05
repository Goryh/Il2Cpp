using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NiceIO;
using Unity.Api.Attributes;
using Unity.IL2CPP.Api;
using Unity.Options;

namespace il2cpp;

public static class Il2CppOptionParser
{
	public static void ParseArguments(string[] args, out bool continueToRun, out ExitCode exitCode, out Il2CppCommandLineArguments il2cppCommandLineArguments, out List<NPath> foundAssemblies)
	{
		il2cppCommandLineArguments = new Il2CppCommandLineArguments();
		exitCode = ExitCode.Success;
		if (OptionsParser.HelpRequested(args) || args.Length == 0)
		{
			OptionsParser.DisplayHelp<Unity.Api.Attributes.HelpDetailsAttribute, Unity.Api.Attributes.HideFromHelpAttribute>(OptionObjectsGraph.ExtractObjectsFromGraph(il2cppCommandLineArguments).ToArray(), (Unity.Api.Attributes.HelpDetailsAttribute attr) => attr.Summary, (Unity.Api.Attributes.HelpDetailsAttribute attr) => attr.CustomValueDescription);
			continueToRun = false;
			foundAssemblies = new List<NPath>();
			return;
		}
		string[] deprecatedOptions = new string[1] { "copy-level=" };
		List<string> remaining = (from r in ParseIntoObjectGraph(args, il2cppCommandLineArguments)
			where !deprecatedOptions.Any(r.Contains)
			select r).ToList();
		remaining = CollectFoundAssembliesFromRemainingArguments(Environment.CurrentDirectory.ToNPath(), remaining, out foundAssemblies).ToList();
		if (remaining.Count > 0)
		{
			Console.WriteLine("Either unknown arguments were used or one or more assemblies could not be found : ");
			foreach (string remain in remaining)
			{
				Console.WriteLine("\t {0}", remain);
			}
			continueToRun = false;
			exitCode = ExitCode.UnknownArgument;
		}
		else
		{
			continueToRun = true;
		}
	}

	public static void ParseArgumentsStep2(Il2CppCommandLineArguments il2cppCommandLineArguments, List<NPath> foundAssemblies)
	{
		SetConversionOptionsBasedOnOptions(il2cppCommandLineArguments);
		SetupOtherArguments(il2cppCommandLineArguments, foundAssemblies);
		if (il2cppCommandLineArguments.CompileCpp)
		{
			ValidateBuildingArguments(il2cppCommandLineArguments);
		}
	}

	public static string[] ParseIntoObjectGraph(string[] args, object root)
	{
		return CommandLineParsing.ParseIntoObjectGraph(args, root, OptionsParser.PrepareInstances);
	}

	private static string[] PreventCompilerAndLinkerFlagsCommaSplitting(FieldInfo info, string s)
	{
		if ((info.Name == "CompilerFlags" || info.Name == "LinkerFlags") && s.Contains(','))
		{
			return new string[1] { s };
		}
		return null;
	}

	public static void SetConversionOptionsBasedOnOptions(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		ConversionSettings settings = il2CppCommandLineArguments.ConversionRequest.Settings;
		if (settings.ConversionMode != ConversionMode.FullPerAssemblyInProcess && settings.ConversionMode != ConversionMode.PartialPerAssemblyInProcess)
		{
			settings.CodeGenerationOption |= CodeGenerationOptions.EnableInlining;
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.GenerateUsymFile)
		{
			settings.EmitSourceMapping = true;
		}
	}

	private static void ValidateBuildingArguments(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		string[] additionalLibraries = il2CppCommandLineArguments.CompilationRequest.AdditionalLibraries;
		foreach (string lib in additionalLibraries)
		{
			try
			{
				if ((File.GetAttributes(lib) & FileAttributes.Directory) != 0)
				{
					throw new ArgumentException("Cannot specify directory \"" + lib + "\" as an additional library file.", "--additional-libraries");
				}
			}
			catch (FileNotFoundException innerException)
			{
				throw new ArgumentException("Non-existent file \"" + lib + "\" specified as an additional library file.", "--additional-libraries", innerException);
			}
			catch (DirectoryNotFoundException innerException2)
			{
				throw new ArgumentException("Non-existent directory \"" + lib + "\" specified as an additional library file.  Cannot specify a directory as an additional library.", "--additional-libraries", innerException2);
			}
			catch (ArgumentException)
			{
				throw;
			}
			catch (Exception innerException3)
			{
				throw new ArgumentException("Unknown error with additional library parameter \"" + lib + "\".", "--additional-libraries", innerException3);
			}
		}
	}

	private static void SetupOtherArguments(Il2CppCommandLineArguments il2CppCommandLineArguments, List<NPath> foundAssemblies)
	{
		ConversionRequest conversionRequest = il2CppCommandLineArguments.ConversionRequest;
		conversionRequest.Assembly = (from p in conversionRequest.Assembly.ToNPaths().Concat(foundAssemblies)
			select p.MakeAbsolute()).ToStringPaths().ToArray();
		if (!il2CppCommandLineArguments.CompileCpp && !il2CppCommandLineArguments.ConvertToCpp)
		{
			il2CppCommandLineArguments.CompileCpp = true;
			il2CppCommandLineArguments.ConvertToCpp = true;
		}
		if (conversionRequest.Generatedcppdir == null)
		{
			conversionRequest.Generatedcppdir = NPath.CreateTempDirectory("il2cpp_generatedcpp");
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.DataFolder == null)
		{
			il2CppCommandLineArguments.SettingsForConversionAndCompilation.DataFolder = conversionRequest.Generatedcppdir.ToNPath().Combine("Data");
		}
		if (conversionRequest.SymbolsFolder == null)
		{
			conversionRequest.SymbolsFolder = conversionRequest.Generatedcppdir.ToNPath().Combine("Symbols");
		}
		if (conversionRequest.ExtraTypesFile == null)
		{
			conversionRequest.ExtraTypesFile = new string[0];
		}
		if (conversionRequest.Statistics.StatsOutputDir == null)
		{
			conversionRequest.Statistics.StatsOutputDir = conversionRequest.Generatedcppdir;
		}
		else
		{
			StatisticsGenerator.DetermineAndSetupOutputDirectory(il2CppCommandLineArguments);
		}
		if (il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerOutputFile == null && il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerReport)
		{
			string extension = (il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerUseTraceEvents ? "traceevents" : "json");
			il2CppCommandLineArguments.SettingsForConversionAndCompilation.ProfilerOutputFile = conversionRequest.Statistics.StatsOutputDir.ToNPath().Combine("profile." + extension).ToString();
		}
		if (il2CppCommandLineArguments.CompilationRequest.BeeJobs <= 0)
		{
			il2CppCommandLineArguments.CompilationRequest.BeeJobs = il2CppCommandLineArguments.SettingsForConversionAndCompilation.Jobs;
		}
	}

	public static IEnumerable<string> CollectFoundAssembliesFromRemainingArguments(NPath currentDirectory, IEnumerable<string> remainingArguments, out List<NPath> foundAssemblies)
	{
		List<NPath> found = new List<NPath>();
		List<string> stillRemaining = new List<string>();
		foreach (string arg in remainingArguments)
		{
			try
			{
				if (Path.IsPathRooted(arg))
				{
					if (File.Exists(arg))
					{
						found.Add(arg.ToNPath());
					}
					else
					{
						stillRemaining.Add(arg);
					}
					continue;
				}
				NPath possibleRelativePath = currentDirectory.Combine(arg);
				if (possibleRelativePath.Exists())
				{
					found.Add(possibleRelativePath);
				}
				else
				{
					stillRemaining.Add(arg);
				}
			}
			catch (ArgumentException)
			{
				stillRemaining.Add(arg);
			}
		}
		foundAssemblies = found;
		return stillRemaining;
	}
}
