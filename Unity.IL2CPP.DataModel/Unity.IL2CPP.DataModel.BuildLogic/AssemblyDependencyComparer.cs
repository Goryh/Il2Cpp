using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class AssemblyDependencyComparer : BaseAssemblyDependencyComparer<AssemblyDefinition>
{
	public AssemblyDependencyComparer(IEnumerable<AssemblyDefinition> assemblies)
	{
		InitializeMaxDepths(assemblies);
	}

	protected override string GetAssemblyName(AssemblyDefinition assembly)
	{
		return assembly.Name.Name;
	}

	protected override IEnumerable<AssemblyDefinition> GetReferencedAssembliesFor(AssemblyDefinition assembly)
	{
		return assembly.References;
	}
}
