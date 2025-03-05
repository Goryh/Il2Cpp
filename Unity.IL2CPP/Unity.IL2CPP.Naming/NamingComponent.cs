using System;
using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Naming;

public class NamingComponent : ServiceComponentBase<INamingService, NamingComponent>, INamingService, IDumpableState
{
	private readonly Dictionary<string, string> _stringLiteralCache;

	private readonly List<(string, string)> _newItemsAddedToStringLiteralCache = new List<(string, string)>();

	private readonly bool _trackNewItemsForMerging;

	private readonly HashCodeCache<string> _stringLiteralHashCache;

	public NamingComponent()
	{
		_trackNewItemsForMerging = false;
		_stringLiteralCache = new Dictionary<string, string>();
		_stringLiteralHashCache = new HashCodeCache<string>(SemiUniqueStableTokenGenerator.GenerateFor);
	}

	private NamingComponent(Dictionary<string, string> existingStringLiterals)
	{
		_trackNewItemsForMerging = true;
		_stringLiteralCache = new Dictionary<string, string>(existingStringLiterals);
		_stringLiteralHashCache = new HashCodeCache<string>(SemiUniqueStableTokenGenerator.GenerateFor);
	}

	public string ForStringLiteralIdentifier(string literal)
	{
		if (_stringLiteralCache.TryGetValue(literal, out var value))
		{
			return value;
		}
		string name = "_stringLiteral" + GenerateUniqueStringLiteralPostFix(literal);
		_stringLiteralCache[literal] = name;
		if (_trackNewItemsForMerging)
		{
			_newItemsAddedToStringLiteralCache.Add((literal, name));
		}
		return name;
	}

	private string GenerateUniqueStringLiteralPostFix(string literal)
	{
		return _stringLiteralHashCache.GetUniqueHash(literal);
	}

	void IDumpableState.DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendValue(builder, "_stringLiteralCache.Count", _stringLiteralCache.Count);
		CollectorStateDumper.AppendValue(builder, "_stringLiteralHashCache.Count", _stringLiteralHashCache.Count);
	}

	protected override NamingComponent CreateCopyInstance()
	{
		return new NamingComponent(_stringLiteralCache);
	}

	protected override NamingComponent CreateEmptyInstance()
	{
		return new NamingComponent();
	}

	protected override NamingComponent CreatePooledInstance()
	{
		return new NamingComponent(_stringLiteralCache);
	}

	protected override NamingComponent ThisAsFull()
	{
		return this;
	}

	protected override INamingService ThisAsRead()
	{
		return this;
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotImplementedException();
	}

	protected override void SyncPooledInstanceWithParent(NamingComponent parent)
	{
		throw new NotImplementedException();
	}

	protected override void HandleMergeForAdd(NamingComponent forked)
	{
		foreach (var item in ItemsForMerging(forked._stringLiteralCache, forked._newItemsAddedToStringLiteralCache, forked._trackNewItemsForMerging))
		{
			if (_trackNewItemsForMerging && !_stringLiteralCache.ContainsKey(item.Item1))
			{
				_newItemsAddedToStringLiteralCache.Add(item);
			}
			_stringLiteralCache[item.Item1] = item.Item2;
		}
	}

	private static IEnumerable<(TKey, TValue)> ItemsForMerging<TKey, TValue>(Dictionary<TKey, TValue> cache, List<(TKey, TValue)> newItems, bool instanceIsUsingTrackNewItemsForMerging)
	{
		if (instanceIsUsingTrackNewItemsForMerging)
		{
			foreach (var newItem in newItems)
			{
				yield return newItem;
			}
			yield break;
		}
		foreach (KeyValuePair<TKey, TValue> item in cache)
		{
			yield return (item.Key, item.Value);
		}
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out object writer, out INamingService reader, out NamingComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Empty);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out object writer, out INamingService reader, out NamingComponent full)
	{
		ReadOnlyForkWithMergeAbility(in data, out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out object writer, out INamingService reader, out NamingComponent full)
	{
		ReadOnlyForkWithMergeAbility(in data, out writer, out reader, out full, ForkMode.Empty, MergeMode.Add);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out object writer, out INamingService reader, out NamingComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Copy);
	}
}
