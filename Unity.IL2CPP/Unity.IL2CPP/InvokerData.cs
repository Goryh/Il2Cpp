using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP;

public struct InvokerData : IEquatable<InvokerData>
{
	public readonly bool VoidReturn;

	public readonly bool DoCallViaInvoker;

	public readonly ReadOnlyCollection<InvokerParameterData> Parameters;

	public InvokerData(bool voidReturn, bool doCallViaInvoker, ReadOnlyCollection<InvokerParameterData> parameters)
	{
		DoCallViaInvoker = doCallViaInvoker;
		VoidReturn = voidReturn;
		Parameters = parameters;
	}

	public override int GetHashCode()
	{
		return HashCodeHelper.Combine(HashCodeHelper.Combine(VoidReturn.GetHashCode(), Parameters.Count.GetHashCode()), DoCallViaInvoker.GetHashCode());
	}

	public bool Equals(InvokerData other)
	{
		if (VoidReturn == other.VoidReturn && DoCallViaInvoker == other.DoCallViaInvoker && Parameters.Count == other.Parameters.Count)
		{
			return Parameters.SequenceEqual(other.Parameters);
		}
		return false;
	}

	public static string FormatInvokerName(VirtualMethodCallType callType, int parameterCount, bool isFunc, bool doCallViaInvoker)
	{
		string callTypeString;
		switch (callType)
		{
		case VirtualMethodCallType.Interface:
			callTypeString = "Interface";
			break;
		case VirtualMethodCallType.Virtual:
			callTypeString = "Virtual";
			break;
		case VirtualMethodCallType.GenericInterface:
			callTypeString = "GenericInterface";
			break;
		case VirtualMethodCallType.GenericVirtual:
			callTypeString = "GenericVirtual";
			break;
		case VirtualMethodCallType.InvokerCall:
			callTypeString = "Invoker";
			doCallViaInvoker = false;
			break;
		case VirtualMethodCallType.ConstrainedInvokerCall:
			callTypeString = "Constrained";
			doCallViaInvoker = false;
			break;
		default:
			callTypeString = callType.ToString();
			break;
		}
		return $"{callTypeString}{(isFunc ? "Func" : "Action")}Invoker{parameterCount.ToString()}{(doCallViaInvoker ? "Invoker" : "")}";
	}
}
