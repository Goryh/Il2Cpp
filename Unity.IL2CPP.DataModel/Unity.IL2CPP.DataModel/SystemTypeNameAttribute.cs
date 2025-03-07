using System;

namespace Unity.IL2CPP.DataModel;

[AttributeUsage(AttributeTargets.Field)]
public class SystemTypeNameAttribute : Attribute
{
	public string Namespace { get; }

	public string Name { get; }

	public string AssemblyName { get; set; }

	public string Version { get; set; }

	public bool IsWindowsRuntime { get; set; }

	public SystemTypeNameAttribute(string name)
		: this("System", name)
	{
	}

	public SystemTypeNameAttribute(string @namespace, string name)
	{
		Namespace = @namespace;
		Name = name;
	}
}
