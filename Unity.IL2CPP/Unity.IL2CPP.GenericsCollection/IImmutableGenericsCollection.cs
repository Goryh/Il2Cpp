using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection;

public interface IImmutableGenericsCollection
{
	ReadOnlyHashSet<GenericInstanceType> Types { get; }

	ReadOnlyHashSet<GenericInstanceType> TypeDeclarations { get; }

	ReadOnlyHashSet<GenericInstanceMethod> Methods { get; }

	ReadOnlyHashSet<ArrayType> Arrays { get; }
}
