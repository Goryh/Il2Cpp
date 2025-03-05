using System;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

public static class InitializePhase
{
	public static void Run(AssemblyConversionContext context)
	{
		using (context.Services.TinyProfiler.Section("InitializePhase"))
		{
			TypeContext typeContext = context.Services.DataModel.Load(context.Services.TinyProfiler.TinyProfiler, new LoadSettings(context.InputData.Assemblies.Select((NPath path) => new AssemblyLoadSettings(path, path.ChangeExtension("pdb").FileExists(), exportsOnly: false)).ToArray().AsReadOnly(), new LoadParameters(context.Parameters.DisableGenericSharing, context.Parameters.DisableFullGenericSharing, context.Parameters.FullGenericSharingOnly, context.Parameters.FullGenericSharingStaticConstructors, context.Parameters.CanShareEnumTypes, freezeDefinitionsOnLoad: false, context.Parameters.EnableDebugger, context.Parameters.EnableSerialConversion, context.InputData.JobCount, applyWindowsRuntimeProjections: true, aggregateWindowsMetadata: true, supportWindowsRuntime: context.InputData.Profile.SupportsWindowsRuntime, maximumRecursiveGenericDepth: GetMaximumRecursionLimit(context, alwaysRequiresGenericSharingAnalysis: false))));
			AssemblyDefinition entryAssembly = GetEntryAssembly(context, typeContext.AssembliesOrderedByCostToProcess);
			context.StatefulServices.TypeFactory.Initialize(context.Services.DataModel.TypeContext);
			context.Results.SetInitializePhaseResults(new AssemblyConversionResults.InitializePhase(typeContext.AssembliesOrderedForPublicExposure, typeContext.AssembliesOrderedByCostToProcess, entryAssembly, new GenericLimitsResults(GetMaximumRecursionLimit(context, context.Services.DataModel.TypeContext.WindowsRuntimeAssembliesLoaded), GetVirtualMethodIterations(context, context.Services.DataModel.TypeContext.WindowsRuntimeAssembliesLoaded))));
		}
	}

	public static AssemblyDefinition GetEntryAssembly(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		AssemblyDefinition[] executableAssemblies = assemblies.Where((AssemblyDefinition a) => a.MainModule.Kind == ModuleKind.Windows || a.MainModule.Kind == ModuleKind.Console).ToArray();
		if (executableAssemblies.Length == 1)
		{
			return executableAssemblies[0];
		}
		AssemblyDefinition assemblyWithEntryPoint = null;
		if (executableAssemblies.Length != 0)
		{
			if (string.IsNullOrEmpty(context.InputData.EntryAssemblyName))
			{
				assemblyWithEntryPoint = executableAssemblies.FirstOrDefault((AssemblyDefinition a) => a.EntryPoint != null);
				if (assemblyWithEntryPoint != null)
				{
					return assemblyWithEntryPoint;
				}
			}
			assemblyWithEntryPoint = executableAssemblies.FirstOrDefault((AssemblyDefinition a) => a.Name.Name == context.InputData.EntryAssemblyName);
			if (assemblyWithEntryPoint == null)
			{
				string message = $"An entry assembly name of '{context.InputData.EntryAssemblyName}' was provided via the command line option --entry-assembly-name, but no assemblies were found with that name.{Environment.NewLine}Here are the assemblies we looked in:{Environment.NewLine}";
				AssemblyDefinition[] array = executableAssemblies;
				foreach (AssemblyDefinition assembly in array)
				{
					message = message + "\t" + assembly.FullName + Environment.NewLine;
				}
				message += "Note that the entry assembly name should _not_ have a file name extension.";
				throw new InvalidOperationException(message);
			}
		}
		return assemblyWithEntryPoint;
	}

	private static int GetMaximumRecursionLimit(AssemblyConversionContext context, bool alwaysRequiresGenericSharingAnalysis)
	{
		return GetGenericLimit(context.InputData.UserSuppliedMaximumRecursiveGenericDepth, context.Parameters.FullGenericSharingOnly, alwaysRequiresGenericSharingAnalysis, 7);
	}

	private static int GetVirtualMethodIterations(AssemblyConversionContext context, bool alwaysRequiresGenericSharingAnalysis)
	{
		return GetGenericLimit(context.InputData.UserSuppliedGenericVirtualMethodIterations, context.Parameters.FullGenericSharingOnly, alwaysRequiresGenericSharingAnalysis, 1);
	}

	private static int GetGenericLimit(int currentValue, bool fullGenericSharingOnly, bool alwaysRequiresGenericSharingAnalysis, int defaultValue)
	{
		if (currentValue > 0)
		{
			return currentValue;
		}
		if (alwaysRequiresGenericSharingAnalysis)
		{
			return defaultValue;
		}
		if (fullGenericSharingOnly)
		{
			return 0;
		}
		return defaultValue;
	}
}
