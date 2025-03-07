using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

internal class AssemblyCache
{
	private ReadOnlyDictionary<string, Mono.Cecil.AssemblyDefinition> _assemblies;

	public Mono.Cecil.AssemblyDefinition Get(Mono.Cecil.AssemblyNameReference name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (!_assemblies.TryGetValue(name.Name, out var asm))
		{
			throw new MissingAssemblyException(name.Name + " was not resolved up front");
		}
		return asm;
	}

	public bool IsCached(Mono.Cecil.AssemblyNameReference assemblyName)
	{
		return _assemblies.ContainsKey(assemblyName.Name);
	}

	public void Populate(IEnumerable<Mono.Cecil.AssemblyDefinition> assemblies)
	{
		Dictionary<string, Mono.Cecil.AssemblyDefinition> dict = new Dictionary<string, Mono.Cecil.AssemblyDefinition>();
		foreach (Mono.Cecil.AssemblyDefinition asm in assemblies)
		{
			dict.Add(asm.Name.Name, asm);
		}
		_assemblies = dict.AsReadOnly();
	}
}
