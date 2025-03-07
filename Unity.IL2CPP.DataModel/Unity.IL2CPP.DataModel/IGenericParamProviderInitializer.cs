using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

internal interface IGenericParamProviderInitializer
{
	void InitializeGenericParameters(ReadOnlyCollection<GenericParameter> genericParameters);
}
