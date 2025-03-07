using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class EmbeddedResource : Resource
{
	private readonly Mono.Cecil.EmbeddedResource _embeddedResource;

	public override ResourceType ResourceType => ResourceType.Embedded;

	public EmbeddedResource(Mono.Cecil.EmbeddedResource embeddedResource)
		: base(embeddedResource.Name, (ManifestResourceAttributes)embeddedResource.Attributes)
	{
		_embeddedResource = embeddedResource;
	}

	public byte[] GetResourceData()
	{
		return _embeddedResource.GetResourceData();
	}
}
