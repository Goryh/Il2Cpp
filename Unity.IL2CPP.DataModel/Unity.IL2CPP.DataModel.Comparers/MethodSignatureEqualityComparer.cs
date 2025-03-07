using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Comparers;

public class MethodSignatureEqualityComparer : EqualityComparer<IMethodSignature>, IComparer<IMethodSignature>
{
	public static readonly MethodSignatureEqualityComparer Instance = new MethodSignatureEqualityComparer();

	public override bool Equals(IMethodSignature x, IMethodSignature y)
	{
		return AreEqual(x, y);
	}

	public static bool AreEqual(IMethodSignature x, IMethodSignature y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (x == y)
		{
			return true;
		}
		if (x.HasThis != y.HasThis)
		{
			return false;
		}
		if (x.ExplicitThis != y.ExplicitThis)
		{
			return false;
		}
		if (x.CallingConvention != y.CallingConvention)
		{
			return false;
		}
		if (x.Parameters.Count != y.Parameters.Count)
		{
			return false;
		}
		if (x.ReturnType != y.ReturnType)
		{
			return false;
		}
		for (int i = 0; i < x.Parameters.Count; i++)
		{
			if (!ParameterDefinitionComparer.AreEqual(x.Parameters[i], y.Parameters[i]))
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode(IMethodSignature obj)
	{
		return GetHashCodeFor(obj);
	}

	public static int GetHashCodeFor(IMethodSignature obj)
	{
		int hashCode = obj.HasThis.GetHashCode();
		hashCode = (hashCode * 23) ^ obj.ExplicitThis.GetHashCode();
		hashCode = (hashCode * 23) ^ obj.CallingConvention.GetHashCode();
		foreach (ParameterDefinition parameter in obj.Parameters)
		{
			hashCode = (hashCode * 23) ^ ParameterDefinitionComparer.GetHashCodeFor(parameter);
		}
		return (hashCode * 23) ^ obj.ReturnType.GetHashCode();
	}

	public int Compare(IMethodSignature x, IMethodSignature y)
	{
		if (x == y)
		{
			return 0;
		}
		if (y == null)
		{
			return 1;
		}
		if (x == null)
		{
			return -1;
		}
		int hasThisComparison = x.HasThis.CompareTo(y.HasThis);
		if (hasThisComparison != 0)
		{
			return hasThisComparison;
		}
		int explicitThisComparison = x.ExplicitThis.CompareTo(y.ExplicitThis);
		if (explicitThisComparison != 0)
		{
			return explicitThisComparison;
		}
		int callingConventionComparison = x.CallingConvention.CompareTo(y.CallingConvention);
		if (callingConventionComparison != 0)
		{
			return callingConventionComparison;
		}
		return x.HasParameters.CompareTo(y.HasParameters);
	}
}
