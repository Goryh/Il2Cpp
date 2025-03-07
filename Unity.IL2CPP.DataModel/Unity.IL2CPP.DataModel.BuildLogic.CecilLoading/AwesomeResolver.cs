using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

internal class AwesomeResolver : Unity.Cecil.Awesome.IAssemblyResolver, Mono.Cecil.IAssemblyResolver, IDisposable
{
	private AssemblyCache _assemblyCache;

	public event EventHandler<string> SearchDirectoryAdded;

	public event EventHandler<Mono.Cecil.AssemblyDefinition> AssemblyCached;

	public AwesomeResolver(AssemblyCache assemblyCache)
	{
		_assemblyCache = assemblyCache;
	}

	public void Dispose()
	{
	}

	public Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name)
	{
		return _assemblyCache.Get(name);
	}

	public Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name, ReaderParameters parameters)
	{
		return Resolve(name);
	}

	public IEnumerable<string> GetSearchDirectories()
	{
		throw new NotSupportedException();
	}

	public bool IsAssemblyCached(Mono.Cecil.AssemblyNameReference assemblyName)
	{
		return _assemblyCache.IsCached(assemblyName);
	}
}
