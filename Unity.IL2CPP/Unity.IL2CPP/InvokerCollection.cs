using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public class InvokerCollection : IInvokerCollector
{
	private class InvokerSignatureEqualityComparer : EqualityComparer<InvokerSignature>
	{
		private readonly TypeReferenceArrayEqualityComparer typeReferenceArrayComparer = new TypeReferenceArrayEqualityComparer();

		public override bool Equals(InvokerSignature x, InvokerSignature y)
		{
			if (x.HasThis == y.HasThis)
			{
				return typeReferenceArrayComparer.Equals(x.ReducedParameterTypes, y.ReducedParameterTypes);
			}
			return false;
		}

		public override int GetHashCode(InvokerSignature obj)
		{
			return HashCodeHelper.Combine(obj.HasThis.GetHashCode(), typeReferenceArrayComparer.GetHashCode(obj.ReducedParameterTypes));
		}
	}

	private class InvokerSignatureComparer : Comparer<InvokerSignature>
	{
		public override int Compare(InvokerSignature x, InvokerSignature y)
		{
			if (x.HasThis)
			{
				if (!y.HasThis)
				{
					return -1;
				}
			}
			else if (y.HasThis)
			{
				return 1;
			}
			return x.ReducedParameterTypes.Compare(y.ReducedParameterTypes);
		}
	}

	private readonly HashSet<InvokerSignature> _runtimeInvokerData = new HashSet<InvokerSignature>(new InvokerSignatureEqualityComparer());

	private bool _complete;

	public void Add(InvokerCollection other)
	{
		_runtimeInvokerData.UnionWith(other._runtimeInvokerData);
	}

	public void Add(ReadOnlyContext context, MethodReference method)
	{
		if (_complete)
		{
			throw new InvalidOperationException("This collection has already been completed");
		}
		InvokerSignature data = InvokerSignature.Create(context, method);
		_runtimeInvokerData.Add(data);
	}

	public ReadOnlyInvokerCollection Complete()
	{
		_complete = true;
		List<InvokerSignature> invokers = _runtimeInvokerData.ToList();
		invokers.Sort(new InvokerSignatureComparer());
		Dictionary<InvokerSignature, int> results = new Dictionary<InvokerSignature, int>(new InvokerSignatureEqualityComparer());
		foreach (InvokerSignature item in invokers)
		{
			results.Add(item, results.Count);
		}
		return new ReadOnlyInvokerCollection(results, invokers.AsReadOnly());
	}

	internal static string NameForInvoker(ReadOnlyContext context, InvokerSignature data)
	{
		INamingService naming = context.Global.Services.Naming;
		StringBuilder sb = new StringBuilder();
		sb.Append(context.Global.Services.ContextScope.ForMetadataGlobalVar("RuntimeInvoker_"));
		sb.Append(data.HasThis);
		sb.Append(naming.ForType(data.ReducedParameterTypes[0]));
		for (int i = 1; i < data.ReducedParameterTypes.Length; i++)
		{
			sb.Append("_");
			sb.Append(naming.ForType(data.ReducedParameterTypes[i]));
		}
		return NamingUtils.ValueOrHashIfTooLong(sb, "RuntimeInvoker_");
	}
}
