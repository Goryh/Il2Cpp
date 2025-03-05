using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Attributes;

public readonly struct AttributeClassCollectionData
{
	public readonly MetadataToken MetadataToken;

	public readonly ReadOnlyCollection<CustomAttributeCollectionData> AttributeData;

	public AttributeClassCollectionData(MetadataToken metadataToken, ReadOnlyCollection<CustomAttributeCollectionData> attributeData)
	{
		AttributeData = attributeData;
		MetadataToken = metadataToken;
	}
}
