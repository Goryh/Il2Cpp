using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Attributes;

internal static class AttributeProviderIterator
{
	public static IEnumerable<ICustomAttributeProvider> Iterate(AssemblyDefinition assembly)
	{
		ICustomAttributeProvider data = Filter(assembly);
		if (data != null)
		{
			yield return data;
		}
		foreach (TypeDefinition type in assembly.GetAllTypes())
		{
			data = Filter(type);
			if (data != null)
			{
				yield return data;
			}
			foreach (FieldDefinition field in type.Fields)
			{
				data = Filter(field);
				if (data != null)
				{
					yield return data;
				}
			}
			foreach (MethodDefinition method in type.Methods)
			{
				data = Filter(method);
				if (data != null)
				{
					yield return data;
				}
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					data = Filter(parameter);
					if (data != null)
					{
						yield return data;
					}
				}
				data = Filter(method.MethodReturnType);
				if (data != null)
				{
					yield return data;
				}
			}
			foreach (PropertyDefinition property in type.Properties)
			{
				data = Filter(property);
				if (data != null)
				{
					yield return data;
				}
			}
			foreach (EventDefinition @event in type.Events)
			{
				data = Filter(@event);
				if (data != null)
				{
					yield return data;
				}
			}
		}
	}

	private static ICustomAttributeProvider Filter(ICustomAttributeProvider customAttributeProvider)
	{
		if (customAttributeProvider.CustomAttributes.Count > 0)
		{
			return customAttributeProvider;
		}
		return null;
	}
}
