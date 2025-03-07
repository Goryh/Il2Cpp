using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public interface ICustomAttributeProvider : IMetadataTokenProvider
{
	ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }
}
