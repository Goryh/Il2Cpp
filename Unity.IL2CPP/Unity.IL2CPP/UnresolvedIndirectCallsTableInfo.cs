using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP;

public struct UnresolvedIndirectCallsTableInfo
{
	public TableInfo VirtualMethodPointersInfo;

	public TableInfo InstanceMethodPointersInfo;

	public TableInfo StaticMethodPointersInfo;

	public ReadOnlyCollection<IndirectCallSignature> SignatureTypes;
}
