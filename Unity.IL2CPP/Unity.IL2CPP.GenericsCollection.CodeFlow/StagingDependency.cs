using System;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal struct StagingDependency<T> : IEquatable<StagingDependency<T>> where T : IEquatable<T>
{
	public readonly T Dependency;

	public readonly int ReferrerIndex;

	public bool IsNeeded;

	public StagingDependency(T dependency, int referrerIndex)
	{
		Dependency = dependency;
		ReferrerIndex = referrerIndex;
		IsNeeded = false;
	}

	public bool Equals(StagingDependency<T> other)
	{
		return Dependency.Equals(other);
	}

	public override int GetHashCode()
	{
		return Dependency.GetHashCode();
	}
}
