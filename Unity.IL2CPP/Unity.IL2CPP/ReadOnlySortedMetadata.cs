using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public class ReadOnlySortedMetadata
{
	public readonly ReadOnlyCollection<MethodReference> Methods;

	public readonly ReadOnlyCollection<IGrouping<TypeReference, IIl2CppRuntimeType>> GroupedTypes;

	public ReadOnlySortedMetadata(ReadOnlyCollection<MethodReference> methods, ReadOnlyCollection<IGrouping<TypeReference, IIl2CppRuntimeType>> groupedTypes)
	{
		Methods = methods;
		GroupedTypes = groupedTypes;
	}
}
