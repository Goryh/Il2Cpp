using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Unity.IL2CPP.Metadata;

internal class MetadataStringsCollector
{
	private readonly Dictionary<string, int> _strings = new Dictionary<string, int>();

	private readonly List<byte> _stringData = new List<byte>();

	public int AddString(string str)
	{
		if (_strings.TryGetValue(str, out var index))
		{
			return index;
		}
		index = AddBytesRaw(Encoding.UTF8.GetBytes(str));
		_strings.Add(str, index);
		return index;
	}

	public int AddBytes(string nameofData, byte[] data)
	{
		if (_strings.TryGetValue(nameofData, out var index))
		{
			return index;
		}
		index = AddBytesRaw(data);
		_strings.Add(nameofData, index);
		return index;
	}

	private int AddBytesRaw(byte[] data)
	{
		int count = _stringData.Count;
		_stringData.AddRange(data);
		_stringData.Add(0);
		return count;
	}

	public ReadOnlyCollection<byte> GetStringData()
	{
		return _stringData.AsReadOnly();
	}

	public int GetStringIndex(string str)
	{
		return _strings[str];
	}
}
