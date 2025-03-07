using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

internal class CecilAssemblyLoader
{
	public static LoadedAssemblyContext LoadDeferred(TinyProfiler2 tinyProfiler, ReadOnlyCollection<AssemblyLoadSettings> allAssemblyPaths, LoadParameters parameters)
	{
		AssemblyCache assemblyCache = new AssemblyCache();
		AwesomeResolver awesomeResolver = new AwesomeResolver(assemblyCache);
		ExportedTypeResolver exportedTypeResolver = new ExportedTypeResolver();
		NewWindowsRuntimeAwareMetadataResolver metadataResolver = new NewWindowsRuntimeAwareMetadataResolver(awesomeResolver, exportedTypeResolver);
		ReaderParameters dllReaderParametersWithoutSymbols = CreateReaderParameters(parameters.ApplyWindowsRuntimeProjections, awesomeResolver, metadataResolver);
		SplitWindowsRuntimeAssemblies((from asm in ParallelHelpers.Map(EstablishSizeOrdering(allAssemblyPaths), delegate(AssemblyLoadSettings asmSettings)
			{
				try
				{
					using (tinyProfiler.Section(asmSettings.Path.FileName))
					{
						return Load(asmSettings.Path, dllReaderParametersWithoutSymbols, asmSettings.LoadSymbols);
					}
				}
				catch (BadImageFormatException)
				{
					return (Mono.Cecil.AssemblyDefinition)null;
				}
			}, parameters.EnableSerial)
			where asm != null
			select asm).ToArray(), parameters.AggregateWindowsMetadata, out var normalAssemblies, out var windowsRuntimeAssemblies);
		bool windowsRuntimeAssembliesLoaded = windowsRuntimeAssemblies.Count > 0;
		WindowsRuntimeLoadContext windowsRuntimeLoadContext;
		if (windowsRuntimeAssembliesLoaded)
		{
			Mono.Cecil.AssemblyDefinition metadataAssembly = CreateWindowsRuntimeMetadataAssembly(dllReaderParametersWithoutSymbols.AssemblyResolver, dllReaderParametersWithoutSymbols.MetadataResolver, dllReaderParametersWithoutSymbols.MetadataImporterProvider, dllReaderParametersWithoutSymbols.ReflectionImporterProvider);
			normalAssemblies.Add(metadataAssembly);
			windowsRuntimeLoadContext = new WindowsRuntimeLoadContext(metadataAssembly, windowsRuntimeAssemblies.AsReadOnly());
		}
		else
		{
			windowsRuntimeLoadContext = new WindowsRuntimeLoadContext();
		}
		assemblyCache.Populate(normalAssemblies);
		if (windowsRuntimeAssembliesLoaded)
		{
			using (tinyProfiler.Section("WinRT Metadata Assembly"))
			{
				WindowsRuntimeLoading.MergeAssembliesIntoMetadataAssemblies(windowsRuntimeLoadContext, parameters, metadataResolver);
			}
		}
		return new LoadedAssemblyContext(normalAssemblies.AsReadOnly(), windowsRuntimeLoadContext, assemblyCache, metadataResolver, awesomeResolver, exportedTypeResolver, windowsRuntimeAssembliesLoaded);
	}

	public static void ImmediateRead(UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> assembly)
	{
		try
		{
			assembly.Source.MainModule.ImmediateRead();
			bool loadSymbols = assembly.Source.MainModule.HasSymbols;
			foreach (Mono.Cecil.TypeDefinition type in assembly.Source.AllDefinedTypes())
			{
				if (!type.HasMethods)
				{
					continue;
				}
				foreach (Mono.Cecil.MethodDefinition method in type.Methods)
				{
					if (method.HasBody)
					{
						_ = method.Body;
					}
					if (loadSymbols)
					{
						_ = method.DebugInformation;
					}
				}
			}
		}
		catch (Exception inner)
		{
			throw new BadImageFormatException($"Failed to finish loading assembly `{assembly.Source.Name}` from {assembly.Source.MainModule.FileName}.  The assembly is likely invalid in some way", inner);
		}
	}

	private static ReaderParameters CreateReaderParameters(bool applyWindowsRuntimeProjections, AwesomeResolver awesomeResolver, NewWindowsRuntimeAwareMetadataResolver metadataResolver)
	{
		return new ReaderParameters
		{
			AssemblyResolver = awesomeResolver,
			MetadataResolver = metadataResolver,
			ReadSymbols = false,
			SymbolReaderProvider = null,
			ApplyWindowsRuntimeProjections = applyWindowsRuntimeProjections,
			ReadingMode = ReadingMode.Deferred
		};
	}

	private static Mono.Cecil.AssemblyDefinition Load(NPath path, ReaderParameters readerParametersWithoutSymbols, bool loadSymbols)
	{
		try
		{
			return Mono.Cecil.AssemblyDefinition.ReadAssembly(path.ToString(), ChooseReaderParameters(path, loadSymbols, readerParametersWithoutSymbols));
		}
		catch (Exception ex)
		{
			if (ex is FileNotFoundException || ex is MonoSymbolFileException || ex is InvalidOperationException)
			{
				return Mono.Cecil.AssemblyDefinition.ReadAssembly(path, ChooseReaderParameters(path, loadSymbols: false, readerParametersWithoutSymbols));
			}
			throw new BadImageFormatException($"The assembly at {path} cannot be loaded.", ex);
		}
	}

	private static ReadOnlyCollection<AssemblyLoadSettings> EstablishSizeOrdering(IEnumerable<AssemblyLoadSettings> assemblies)
	{
		return assemblies.OrderBy((AssemblyLoadSettings asm) => new FileInfo(asm.Path).Length * -1).ToArray().AsReadOnly();
	}

	private static void SplitWindowsRuntimeAssemblies(Mono.Cecil.AssemblyDefinition[] allAssemblies, bool aggregateWindowsMetadata, out List<Mono.Cecil.AssemblyDefinition> normalAssemblies, out List<Mono.Cecil.AssemblyDefinition> windowsRuntimeAssemblies)
	{
		if (!aggregateWindowsMetadata || allAssemblies.All((Mono.Cecil.AssemblyDefinition asm) => asm.MainModule.MetadataKind != Mono.Cecil.MetadataKind.WindowsMetadata))
		{
			normalAssemblies = allAssemblies.ToList();
			windowsRuntimeAssemblies = new List<Mono.Cecil.AssemblyDefinition>();
			return;
		}
		List<Mono.Cecil.AssemblyDefinition> normal = new List<Mono.Cecil.AssemblyDefinition>();
		List<Mono.Cecil.AssemblyDefinition> winrt = new List<Mono.Cecil.AssemblyDefinition>();
		foreach (Mono.Cecil.AssemblyDefinition assembly in allAssemblies)
		{
			if (assembly.MainModule.MetadataKind == Mono.Cecil.MetadataKind.WindowsMetadata)
			{
				winrt.Add(assembly);
			}
			else
			{
				normal.Add(assembly);
			}
		}
		normalAssemblies = normal;
		windowsRuntimeAssemblies = winrt;
	}

	private static Mono.Cecil.AssemblyDefinition CreateWindowsRuntimeMetadataAssembly(Mono.Cecil.IAssemblyResolver resolver, IMetadataResolver metadataResolver, IMetadataImporterProvider metadataImporterProvider, IReflectionImporterProvider reflectionImporterProvider)
	{
		AssemblyNameDefinition assemblyName = new AssemblyNameDefinition("WindowsRuntimeMetadata", new Version(255, 255, 255, 255))
		{
			Culture = "",
			IsWindowsRuntime = true
		};
		ModuleParameters moduleParameters = new ModuleParameters
		{
			AssemblyResolver = resolver,
			MetadataImporterProvider = metadataImporterProvider,
			MetadataResolver = metadataResolver,
			ReflectionImporterProvider = reflectionImporterProvider,
			Runtime = TargetRuntime.Net_4_0
		};
		return Mono.Cecil.AssemblyDefinition.CreateAssembly(assemblyName, "WindowsRuntimeMetadata", moduleParameters);
	}

	private static ReaderParameters ChooseReaderParameters(string path, bool loadSymbols, ReaderParameters readerParametersWithoutSymbols)
	{
		if (loadSymbols)
		{
			return CloneReaderParametersWithSymbols(readerParametersWithoutSymbols);
		}
		return readerParametersWithoutSymbols;
	}

	private static ReaderParameters CloneReaderParametersWithSymbols(ReaderParameters original)
	{
		ReaderParameters readerParameters = CloneReaderParameters(original);
		readerParameters.ReadSymbols = true;
		readerParameters.SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false);
		return readerParameters;
	}

	private static ReaderParameters CloneReaderParameters(ReaderParameters original)
	{
		return new ReaderParameters
		{
			ApplyWindowsRuntimeProjections = original.ApplyWindowsRuntimeProjections,
			AssemblyResolver = original.AssemblyResolver,
			MetadataImporterProvider = original.MetadataImporterProvider,
			MetadataResolver = original.MetadataResolver,
			ReadingMode = original.ReadingMode,
			ReadSymbols = original.ReadSymbols,
			ReflectionImporterProvider = original.ReflectionImporterProvider,
			SymbolReaderProvider = original.SymbolReaderProvider,
			SymbolStream = original.SymbolStream
		};
	}
}
