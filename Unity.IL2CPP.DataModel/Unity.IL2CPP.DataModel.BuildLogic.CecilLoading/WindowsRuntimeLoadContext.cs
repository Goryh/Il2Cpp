using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

public class WindowsRuntimeLoadContext : IDisposable
{
	public readonly Mono.Cecil.AssemblyDefinition MetadataAssembly;

	public readonly ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> WindowsRuntimeAssemblies;

	public WindowsRuntimeLoadContext()
		: this(null, null)
	{
	}

	public WindowsRuntimeLoadContext(Mono.Cecil.AssemblyDefinition metadataAssembly, ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> windowsRuntimeAssemblies)
	{
		MetadataAssembly = metadataAssembly;
		WindowsRuntimeAssemblies = windowsRuntimeAssemblies;
	}

	public void Dispose()
	{
		if (WindowsRuntimeAssemblies == null)
		{
			return;
		}
		foreach (Mono.Cecil.AssemblyDefinition windowsRuntimeAssembly in WindowsRuntimeAssemblies)
		{
			windowsRuntimeAssembly.Dispose();
		}
	}
}
