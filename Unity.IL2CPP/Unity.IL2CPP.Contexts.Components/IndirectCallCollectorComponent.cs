using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components;

public class IndirectCallCollectorComponent : ItemsWithMetadataIndexCollector<IndirectCallSignature, IIndirectCallCollectorResults, IIndirectCallCollector, IndirectCallCollectorComponent>, IIndirectCallCollector
{
	private class Results : MetadataIndexTableResults<IndirectCallSignature>, IIndirectCallCollectorResults, IMetadataIndexTableResults<IndirectCallSignature>, ITableResults<IndirectCallSignature, uint>
	{
		public Results(ReadOnlyCollection<IndirectCallSignature> sortedItems, ReadOnlyDictionary<IndirectCallSignature, uint> table, ReadOnlyCollection<KeyValuePair<IndirectCallSignature, uint>> sortedTable)
			: base(sortedItems, table, sortedTable)
		{
		}
	}

	private class NotAvailable : IIndirectCallCollector
	{
		public void Add(SourceWritingContext context, MethodReference method, IndirectCallUsage callUsage, bool skipFirstArg)
		{
			throw new NotSupportedException();
		}

		public void Add(SourceWritingContext context, TypeReference returnType, IReadOnlyList<TypeReference> parameterTypes, IndirectCallUsage callUsage)
		{
			throw new NotSupportedException();
		}

		public void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods, IndirectCallUsage callUsage)
		{
			throw new NotSupportedException();
		}
	}

	protected override IIndirectCallCollectorResults CreateResultObject(ReadOnlyCollection<IndirectCallSignature> sortedItems, ReadOnlyDictionary<IndirectCallSignature, uint> table, ReadOnlyCollection<KeyValuePair<IndirectCallSignature, uint>> sortedTable)
	{
		return new Results(sortedItems, table, sortedTable);
	}

	public void Add(SourceWritingContext context, MethodReference method, IndirectCallUsage callUsage, bool skipFirstArg = false)
	{
		Add(MethodToSignature(context, method.GetResolvedReturnType(context), (from p in method.GetResolvedParameters(context)
			select p.ParameterType).ToList(), callUsage, skipFirstArg));
	}

	public void Add(SourceWritingContext context, TypeReference returnType, IReadOnlyList<TypeReference> parameterTypes, IndirectCallUsage callUsage)
	{
		Add(MethodToSignature(context, returnType, parameterTypes, callUsage, skipFirstArg: false));
	}

	public void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods, IndirectCallUsage callUsage)
	{
		foreach (MethodReference method in methods)
		{
			Add(context, method, callUsage);
		}
	}

	private void Add(IndirectCallSignature item)
	{
		if (!item.Signature.Any((IIl2CppRuntimeType s) => s.Type.ContainsFullySharedGenericTypes))
		{
			if (item.Signature.Any((IIl2CppRuntimeType s) => s.Type.ContainsGenericParameter))
			{
				throw new InvalidOperationException("Signature contains generated parameters [" + item.Signature.AggregateWithComma() + "]");
			}
			base.AddInternal(item);
		}
	}

	private static IndirectCallSignature MethodToSignature(SourceWritingContext context, TypeReference returnType, IReadOnlyList<TypeReference> parameterTypes, IndirectCallUsage callUsage, bool skipFirstArg)
	{
		int parameterOffset = ((!skipFirstArg || parameterTypes.Count <= 0) ? 1 : 0);
		IIl2CppRuntimeType[] data = new IIl2CppRuntimeType[parameterTypes.Count + parameterOffset];
		data[0] = context.Global.Collectors.Types.Add(TypeFor(context, returnType));
		for (int i = (skipFirstArg ? 1 : 0); i < parameterTypes.Count; i++)
		{
			data[i + parameterOffset] = context.Global.Collectors.Types.Add(TypeFor(context, parameterTypes[i]));
		}
		return new IndirectCallSignature(data, callUsage);
	}

	private static TypeReference TypeFor(ReadOnlyContext context, TypeReference type)
	{
		if (type.IsByReference || !type.IsValueType)
		{
			return context.Global.Services.TypeProvider.SystemObject;
		}
		if (type.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable)
		{
			return context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference;
		}
		if (type.IsEnum)
		{
			type = type.GetUnderlyingEnumType();
		}
		if (type.MetadataType == MetadataType.Boolean)
		{
			return context.Global.Services.TypeProvider.SystemByte;
		}
		if (type.MetadataType == MetadataType.Char)
		{
			return context.Global.Services.TypeProvider.SystemUInt16;
		}
		if (type is GenericInstanceType genericInstanceType)
		{
			return genericInstanceType.GetCollapsedSignatureType(context);
		}
		return type;
	}

	protected override ReadOnlyCollection<IndirectCallSignature> SortItems(IEnumerable<IndirectCallSignature> items)
	{
		ReadOnlyCollection<IndirectCallSignature> results = items.ToSortedCollection(new IndirectCallSignatureComparer());
		if (results.Count == 0)
		{
			return results;
		}
		List<IndirectCallSignature> filteredResults = new List<IndirectCallSignature>(results.Count);
		IIl2CppRuntimeType[] lastSig = results[0].Signature;
		IndirectCallUsage lastUsage = results[0].Usage;
		for (int i = 1; i < results.Count; i++)
		{
			if (Il2CppRuntimeTypeArrayEqualityComparer.AreEqual(lastSig, results[i].Signature))
			{
				lastUsage |= results[i].Usage;
				continue;
			}
			filteredResults.Add(new IndirectCallSignature(lastSig, lastUsage));
			lastSig = results[i].Signature;
			lastUsage = results[i].Usage;
		}
		filteredResults.Add(new IndirectCallSignature(lastSig, lastUsage));
		return filteredResults.AsReadOnly();
	}

	protected override IndirectCallCollectorComponent CreateEmptyInstance()
	{
		return new IndirectCallCollectorComponent();
	}

	protected override IndirectCallCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override IIndirectCallCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IIndirectCallCollector writer, out object reader, out IndirectCallCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IIndirectCallCollector writer, out object reader, out IndirectCallCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IIndirectCallCollector writer, out object reader, out IndirectCallCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IIndirectCallCollector writer, out object reader, out IndirectCallCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}
}
