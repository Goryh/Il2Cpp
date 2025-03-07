namespace Unity.IL2CPP.DataModel;

public static class CustomAttributeProviderExtensions
{
	public static bool HasAttribute(this ICustomAttributeProvider customAttributeProvider, string @namespace, string name)
	{
		for (int i = 0; i < customAttributeProvider.CustomAttributes.Count; i++)
		{
			if (customAttributeProvider.CustomAttributes[i].AttributeType.Name == name && customAttributeProvider.CustomAttributes[i].AttributeType.Namespace == @namespace)
			{
				return true;
			}
		}
		return false;
	}
}
