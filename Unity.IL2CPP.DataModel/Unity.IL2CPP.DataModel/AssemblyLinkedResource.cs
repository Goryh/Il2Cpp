using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class AssemblyLinkedResource : Resource
{
	public override ResourceType ResourceType => ResourceType.AssemblyLinked;

	public AssemblyLinkedResource(Mono.Cecil.AssemblyLinkedResource linkedResource)
		: base(linkedResource.Name, (ManifestResourceAttributes)linkedResource.Attributes)
	{
	}
}
