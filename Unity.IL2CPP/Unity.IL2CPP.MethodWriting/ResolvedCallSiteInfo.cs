using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedCallSiteInfo : IResolvedMethodSignature
{
	private readonly CallSite UnresolvedCallSite;

	public ResolvedTypeInfo ReturnType { get; }

	public ReadOnlyCollection<ResolvedParameter> Parameters { get; }

	public bool HasThis => UnresolvedCallSite.HasThis;

	public MethodCallingConvention CallingConvention => UnresolvedCallSite.CallingConvention;

	public IMethodSignature UnresolvedMethodSignature => UnresolvedCallSite;

	public ResolvedCallSiteInfo(CallSite unresolvedCallSite, ResolvedTypeInfo returnType, ReadOnlyCollection<ResolvedParameter> parameters)
	{
		UnresolvedCallSite = unresolvedCallSite;
		ReturnType = returnType;
		Parameters = parameters;
	}
}
