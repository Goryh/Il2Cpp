using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class AssemblyDefinitionDictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<AssemblyDefinition, TValue>>
{
	public int Compare(KeyValuePair<AssemblyDefinition, TValue> x, KeyValuePair<AssemblyDefinition, TValue> y)
	{
		return string.Compare(x.Key.Name.Name, y.Key.Name.Name, StringComparison.Ordinal);
	}
}
