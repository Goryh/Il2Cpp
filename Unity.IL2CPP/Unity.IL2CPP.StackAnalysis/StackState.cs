using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public class StackState
{
	private Stack<Entry> _entries = new Stack<Entry>();

	public Stack<Entry> Entries => _entries;

	public bool IsEmpty => _entries.Count == 0;

	public void Merge(StackState other)
	{
		List<Entry> selfEntries = new List<Entry>(_entries);
		List<Entry> otherEntries = new List<Entry>(other.Entries);
		while (selfEntries.Count < otherEntries.Count)
		{
			selfEntries.Add(new Entry());
		}
		for (int index = 0; index < otherEntries.Count; index++)
		{
			Entry otherEntry = otherEntries[index];
			Entry selfEntry = selfEntries[index];
			if (selfEntry.Types.Count == 1 && otherEntry.Types.Count == 1)
			{
				ResolvedTypeInfo x = selfEntry.Types.First();
				ResolvedTypeInfo otherType = otherEntry.Types.First();
				if (!ResolvedTypeEqualityComparer.AreEqual(x, otherType))
				{
					if (selfEntry.NullValue)
					{
						selfEntry.NullValue = otherEntry.NullValue;
						selfEntry.Types.Clear();
						selfEntry.Types.Add(otherType);
						continue;
					}
					if (otherEntry.NullValue)
					{
						continue;
					}
				}
			}
			selfEntry.NullValue |= otherEntry.NullValue;
			foreach (ResolvedTypeInfo otherType2 in otherEntry.Types)
			{
				selfEntry.Types.Add(otherType2);
			}
		}
		selfEntries.Reverse();
		_entries = new Stack<Entry>(selfEntries);
	}

	public StackState Clone()
	{
		StackState cloned = new StackState();
		foreach (Entry entry in _entries.Reverse())
		{
			cloned.Entries.Push(entry.Clone());
		}
		return cloned;
	}
}
