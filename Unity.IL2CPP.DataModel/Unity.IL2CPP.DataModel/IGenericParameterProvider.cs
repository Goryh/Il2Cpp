using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public interface IGenericParameterProvider
{
	bool HasGenericParameters { get; }

	bool IsDefinition { get; }

	ReadOnlyCollection<GenericParameter> GenericParameters { get; }

	GenericParameterType GenericParameterType { get; }

	MetadataToken MetadataToken { get; }
}
