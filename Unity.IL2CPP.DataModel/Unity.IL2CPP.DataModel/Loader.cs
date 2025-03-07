using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.DataModel;

public static class Loader
{
	public static DataModelBuilder Load(TinyProfiler2 tinyProfiler, NPath directory, LoadParameters parameters, bool loadAllSymbols)
	{
		return Load(tinyProfiler, ManagedAssemblyFilesIn(directory), parameters, loadAllSymbols);
	}

	public static DataModelBuilder Load(TinyProfiler2 tinyProfiler, NPath directory, LoadParameters parameters, Func<NPath, AssemblyLoadSettings> createAssemblySettings)
	{
		List<AssemblyLoadSettings> assemblySettings = new List<AssemblyLoadSettings>();
		foreach (NPath path in ManagedAssemblyFilesIn(directory))
		{
			assemblySettings.Add(createAssemblySettings(path));
		}
		return Load(tinyProfiler, new LoadSettings(assemblySettings.AsReadOnly(), parameters));
	}

	public static DataModelBuilder Load(TinyProfiler2 tinyProfiler, LoadSettings settings)
	{
		DataModelBuilder dataModelBuilder = new DataModelBuilder(tinyProfiler, settings);
		dataModelBuilder.Build();
		return dataModelBuilder;
	}

	public static DataModelBuilder Load(TinyProfiler2 tinyProfiler, IEnumerable<NPath> assemblies, LoadParameters parameters, bool loadAllSymbols)
	{
		return Load(tinyProfiler, new LoadSettings(assemblies.Select((NPath path) => new AssemblyLoadSettings(path, loadAllSymbols, exportsOnly: false)).ToArray().AsReadOnly(), parameters));
	}

	private static IEnumerable<NPath> ManagedAssemblyFilesIn(NPath directory)
	{
		return from f in directory.Files()
			where f.ExtensionWithDot == ".exe" || f.ExtensionWithDot == ".dll" || f.ExtensionWithDot == ".winmd"
			select f;
	}
}
