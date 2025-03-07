using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class LinkedResource : Resource
{
	public override ResourceType ResourceType => ResourceType.Linked;

	public LinkedResource(Mono.Cecil.LinkedResource linkedResource)
		: base(linkedResource.Name, (ManifestResourceAttributes)linkedResource.Attributes)
	{
	}
}
