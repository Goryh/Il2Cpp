using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public static class SystemTypeName
{
	public static SystemTypeReference[] GetSystemTypeNames()
	{
		return (from f in typeof(SystemType).GetFields(BindingFlags.Static | BindingFlags.Public)
			orderby f.GetValue(null)
			select f).Select(delegate(FieldInfo f)
		{
			SystemTypeNameAttribute customAttribute = f.GetCustomAttribute<SystemTypeNameAttribute>();
			if (customAttribute == null)
			{
				return new SystemTypeReference("System", f.GetValue(null).ToString(), isInSystem: true, null);
			}
			return (customAttribute.AssemblyName == null) ? new SystemTypeReference(customAttribute.Namespace, customAttribute.Name, isInSystem: true, null) : new SystemTypeReference(customAttribute.Namespace, customAttribute.Name, isInSystem: false, new Mono.Cecil.AssemblyNameReference(customAttribute.AssemblyName, Version.Parse(customAttribute.Version))
			{
				IsWindowsRuntime = customAttribute.IsWindowsRuntime
			});
		}).ToArray();
	}
}
