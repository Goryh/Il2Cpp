using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Results.Phases;

public interface IReversePInvokeWrapperCollectorResults : IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
{
}
