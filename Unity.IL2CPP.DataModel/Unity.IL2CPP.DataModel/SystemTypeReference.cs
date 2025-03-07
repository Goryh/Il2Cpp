using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{Namespace}.{Name}")]
public class SystemTypeReference
{
	public string Namespace { get; }

	public string Name { get; }

	public bool IsInSystem { get; }

	public Mono.Cecil.AssemblyNameReference AssemblyNameReference { get; }

	public SystemTypeReference(string ns, string name, bool isInSystem, Mono.Cecil.AssemblyNameReference assemblyNameReference)
	{
		Namespace = ns;
		Name = name;
		IsInSystem = isInSystem;
		AssemblyNameReference = assemblyNameReference;
	}
}
