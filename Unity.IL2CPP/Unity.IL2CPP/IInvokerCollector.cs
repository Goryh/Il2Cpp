using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public interface IInvokerCollector
{
	void Add(ReadOnlyContext context, MethodReference method);
}
