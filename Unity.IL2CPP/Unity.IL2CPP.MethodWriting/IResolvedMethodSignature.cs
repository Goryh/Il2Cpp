using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public interface IResolvedMethodSignature
{
	ResolvedTypeInfo ReturnType { get; }

	ReadOnlyCollection<ResolvedParameter> Parameters { get; }

	bool HasThis { get; }

	MethodCallingConvention CallingConvention { get; }

	IMethodSignature UnresolvedMethodSignature { get; }
}
