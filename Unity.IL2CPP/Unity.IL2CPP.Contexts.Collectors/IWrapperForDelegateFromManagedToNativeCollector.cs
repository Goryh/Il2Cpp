using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IWrapperForDelegateFromManagedToNativeCollector
{
	void Add(SourceWritingContext context, MethodReference method);
}
