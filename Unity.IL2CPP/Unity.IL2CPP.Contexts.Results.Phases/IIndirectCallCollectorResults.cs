using Unity.IL2CPP.Contexts.Components.Base;

namespace Unity.IL2CPP.Contexts.Results.Phases;

public interface IIndirectCallCollectorResults : IMetadataIndexTableResults<IndirectCallSignature>, ITableResults<IndirectCallSignature, uint>
{
}
