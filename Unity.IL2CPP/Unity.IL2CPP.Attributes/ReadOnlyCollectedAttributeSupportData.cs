using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Attributes;

public class ReadOnlyCollectedAttributeSupportData
{
	public ReadOnlyCollection<AttributeClassCollectionData> AttributeClasses { get; }

	public ReadOnlyCollectedAttributeSupportData(ReadOnlyCollection<AttributeClassCollectionData> attributeCollection)
	{
		AttributeClasses = attributeCollection;
	}
}
