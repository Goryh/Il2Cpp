namespace Unity.IL2CPP.DataModel;

public abstract class Resource
{
	public string Name { get; }

	public ManifestResourceAttributes Attributes { get; }

	public abstract ResourceType ResourceType { get; }

	public bool IsPublic => (Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public;

	public bool IsPrivate => (Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private;

	protected Resource(string name, ManifestResourceAttributes attributes)
	{
		Name = name;
		Attributes = attributes;
	}
}
