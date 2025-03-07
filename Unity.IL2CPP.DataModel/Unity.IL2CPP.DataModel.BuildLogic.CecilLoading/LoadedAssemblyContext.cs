using System;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

internal class LoadedAssemblyContext : IDisposable
{
	public readonly ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> Assemblies;

	public readonly WindowsRuntimeLoadContext WindowsRuntimeLoadContext;

	private readonly AwesomeResolver _awesomeResolver;

	private readonly ExportedTypeResolver _exportedTypeResolver;

	private readonly NewWindowsRuntimeAwareMetadataResolver _metadataResolver;

	private readonly AssemblyCache _assemblyCache;

	public readonly bool WindowsRuntimeAssembliesLoaded;

	public LoadedAssemblyContext(ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> normalAssemblies, WindowsRuntimeLoadContext windowsRuntimeLoadContext, AssemblyCache assemblyCache, NewWindowsRuntimeAwareMetadataResolver metadataResolver, AwesomeResolver awesomeResolver, ExportedTypeResolver exportedTypeResolver, bool windowsRuntimeAssembliesLoaded)
	{
		Assemblies = normalAssemblies;
		_assemblyCache = assemblyCache;
		_awesomeResolver = awesomeResolver;
		_metadataResolver = metadataResolver;
		_exportedTypeResolver = exportedTypeResolver;
		WindowsRuntimeLoadContext = windowsRuntimeLoadContext;
		WindowsRuntimeAssembliesLoaded = windowsRuntimeAssembliesLoaded;
	}

	public void Dispose()
	{
		foreach (Mono.Cecil.AssemblyDefinition assembly in Assemblies)
		{
			assembly.Dispose();
		}
		WindowsRuntimeLoadContext.Dispose();
	}
}
