using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class UnderConstructionCollection<TOurs, TSource> : IEnumerable<UnderConstructionMember<TOurs, TSource>>, IEnumerable where TOurs : MemberReference where TSource : Mono.Cecil.MemberReference
{
	private struct Enumerator : IEnumerator<UnderConstructionMember<TOurs, TSource>>, IEnumerator, IDisposable
	{
		private readonly List<TOurs> _ours;

		private readonly List<TSource> _sources;

		private readonly List<CecilSourcedAssemblyData> _data;

		private int _index;

		public UnderConstructionMember<TOurs, TSource> Current => new UnderConstructionMember<TOurs, TSource>(_ours[_index], _sources[_index], _data[_index]);

		object IEnumerator.Current => Current;

		public Enumerator(List<TOurs> ours, List<TSource> sources, List<CecilSourcedAssemblyData> data)
		{
			_ours = ours;
			_sources = sources;
			_data = data;
			_index = -1;
		}

		public bool MoveNext()
		{
			_index++;
			return _index < _ours.Count;
		}

		public void Reset()
		{
			_index = -1;
		}

		public void Dispose()
		{
		}
	}

	private readonly List<TOurs> _ours;

	private readonly List<TSource> _sources;

	private readonly List<CecilSourcedAssemblyData> _data;

	private bool _isComplete;

	public UnderConstructionCollection()
	{
		_ours = new List<TOurs>();
		_sources = new List<TSource>();
		_data = new List<CecilSourcedAssemblyData>();
	}

	public UnderConstructionCollection(int count)
	{
		_ours = new List<TOurs>(count);
		_sources = new List<TSource>(count);
		_data = new List<CecilSourcedAssemblyData>(count);
	}

	public List<TOurs> Complete()
	{
		_isComplete = true;
		return _ours;
	}

	public void Add(TOurs ours, TSource source, CecilSourcedAssemblyData data)
	{
		if (_isComplete)
		{
			throw new ArgumentException("This collection is complete.  New items cannot be added");
		}
		_ours.Add(ours);
		_sources.Add(source);
		_data.Add(data);
	}

	public IEnumerator<UnderConstructionMember<TOurs, TSource>> GetEnumerator()
	{
		return new Enumerator(_ours, _sources, _data);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
