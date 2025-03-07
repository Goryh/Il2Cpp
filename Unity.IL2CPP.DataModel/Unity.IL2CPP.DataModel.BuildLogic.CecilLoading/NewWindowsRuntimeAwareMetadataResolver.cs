using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

public class NewWindowsRuntimeAwareMetadataResolver : BaseWindowsRuntimeAwareMetadataResolver
{
	private ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> _loadedWinmds;

	private Mono.Cecil.AssemblyDefinition _metadataAssembly;

	public NewWindowsRuntimeAwareMetadataResolver(Unity.Cecil.Awesome.IAssemblyResolver assemblyResolver, ExportedTypeResolver exportedTypeResolver)
		: base(assemblyResolver, exportedTypeResolver)
	{
	}

	public void PopulateWinmds(IEnumerable<Mono.Cecil.AssemblyDefinition> windowsRuntimeAssemblies)
	{
		_loadedWinmds = windowsRuntimeAssemblies.Where((Mono.Cecil.AssemblyDefinition asm) => asm.Name.IsWindowsRuntime).ToArray().AsReadOnly();
	}

	public void MetadataAssemblyMergeComplete(Mono.Cecil.AssemblyDefinition metadataAssembly)
	{
		_metadataAssembly = metadataAssembly;
	}

	protected override Mono.Cecil.TypeDefinition FindTypeInUnknownWinmd(Mono.Cecil.TypeReference type)
	{
		if (_metadataAssembly != null)
		{
			return FindTypeInUnknownWinmdInMetadataAssembly(type);
		}
		if (_loadedWinmds == null)
		{
			throw new WinmdResolutionException("No winmds were registered.  Can't resolve types in unknown winmd files");
		}
		return FindTypeInUnknownWinmdInLoadedWinmds(type);
	}

	private Mono.Cecil.TypeDefinition FindTypeInUnknownWinmdInMetadataAssembly(Mono.Cecil.TypeReference type)
	{
		return GetType(_metadataAssembly.MainModule, type);
	}

	private Mono.Cecil.TypeDefinition FindTypeInUnknownWinmdInLoadedWinmds(Mono.Cecil.TypeReference type)
	{
		foreach (Mono.Cecil.AssemblyDefinition winmd in _loadedWinmds)
		{
			Mono.Cecil.TypeDefinition typeDef = GetType(winmd.MainModule, type);
			if (typeDef != null)
			{
				return typeDef;
			}
		}
		throw new InvalidOperationException($"Unresolved type : {type}");
	}
}
