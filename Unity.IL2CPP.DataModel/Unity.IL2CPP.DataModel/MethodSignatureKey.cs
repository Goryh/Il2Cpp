using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Comparers;

namespace Unity.IL2CPP.DataModel;

public class MethodSignatureKey : IMethodSignature, IEquatable<MethodSignatureKey>
{
	public bool HasThis { get; }

	public bool ExplicitThis { get; }

	public bool HasParameters => Parameters.Count > 0;

	public MethodCallingConvention CallingConvention { get; }

	public TypeReference ReturnType { get; }

	public ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public MethodSignatureKey(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis)
	{
		ReturnType = returnType;
		Parameters = parameters;
		CallingConvention = callingConvention;
		HasThis = hasThis;
		ExplicitThis = explicitThis;
	}

	public bool Equals(MethodSignatureKey other)
	{
		return MethodSignatureEqualityComparer.AreEqual(this, other);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((MethodSignatureKey)obj);
	}

	public override int GetHashCode()
	{
		return MethodSignatureEqualityComparer.GetHashCodeFor(this);
	}
}
