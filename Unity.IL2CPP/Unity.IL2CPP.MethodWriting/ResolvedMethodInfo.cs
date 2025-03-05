using System.Collections.ObjectModel;
using System.Diagnostics;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

[DebuggerDisplay("{UnresovledMethodReference.FullName} - {ResolvedMethodReference.FullName} ")]
public class ResolvedMethodInfo : IResolvedMethodSignature
{
	public readonly MethodReference UnresovledMethodReference;

	public readonly MethodReference ResolvedMethodReference;

	public ResolvedTypeInfo DeclaringType { get; }

	public ResolvedTypeInfo ReturnType { get; }

	public ReadOnlyCollection<ResolvedParameter> Parameters { get; }

	public string FullName => ResolvedMethodReference.FullName;

	public string Name => ResolvedMethodReference.Name;

	public bool HasThis => UnresovledMethodReference.HasThis;

	public MethodCallingConvention CallingConvention => UnresovledMethodReference.CallingConvention;

	public IMethodSignature UnresolvedMethodSignature => UnresovledMethodReference;

	public bool IsGenericInstance => ResolvedMethodReference.IsGenericInstance;

	public bool IsVirtual => UnresovledMethodReference.Resolve().IsVirtual;

	public bool IsUnmanagedCallersOnly => ResolvedMethodReference.IsUnmanagedCallersOnly;

	public ResolvedMethodInfo(MethodReference unresovledMethodReference, MethodReference resolvedMethodReference, ResolvedTypeInfo declaringType, ResolvedTypeInfo returnType, ReadOnlyCollection<ResolvedParameter> parameters)
	{
		UnresovledMethodReference = unresovledMethodReference;
		ResolvedMethodReference = resolvedMethodReference;
		DeclaringType = declaringType;
		ReturnType = returnType;
		Parameters = parameters;
	}

	public bool IsStatic()
	{
		return UnresovledMethodReference.IsStatic;
	}

	public override string ToString()
	{
		return ResolvedMethodReference.ToString();
	}
}
