using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public struct InvokerParameterData : IEquatable<InvokerParameterData>
{
	public readonly bool SpecializeAsPointerType;

	private InvokerParameterData(bool specializeAsPointerType)
	{
		SpecializeAsPointerType = specializeAsPointerType;
	}

	public bool Equals(InvokerParameterData other)
	{
		return SpecializeAsPointerType == other.SpecializeAsPointerType;
	}

	public override bool Equals(object obj)
	{
		if (obj is InvokerParameterData other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return SpecializeAsPointerType.GetHashCode();
	}

	public static ReadOnlyCollection<InvokerParameterData> FromParameterList(ReadOnlyContext context, IEnumerable<TypeReference> parameterTypes, bool doCallViaInvoker)
	{
		if (doCallViaInvoker)
		{
			List<InvokerParameterData> invokerParamList = new List<InvokerParameterData>();
			foreach (TypeReference parameterType in parameterTypes)
			{
				RuntimeStorageKind runtimeStorage = parameterType.GetRuntimeStorage(context);
				invokerParamList.Add(new InvokerParameterData(runtimeStorage != RuntimeStorageKind.ValueType));
			}
			return invokerParamList.AsReadOnly();
		}
		return new InvokerParameterData[parameterTypes.Count()].AsReadOnly();
	}
}
