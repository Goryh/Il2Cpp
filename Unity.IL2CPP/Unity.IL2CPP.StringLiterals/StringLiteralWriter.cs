using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.StringLiterals;

public class StringLiteralWriter
{
	public void Write(SourceWritingContext context, Stream stringLiteralStream, Stream stringLiteralDataStream, ReadOnlyStringLiteralTable stringLiteralCollection)
	{
		ReadOnlyCollection<KeyValuePair<string, uint>> stringLiterals = stringLiteralCollection.Items;
		int[] dataIndices = new int[stringLiterals.Count];
		List<byte> bytes = new List<byte>();
		for (int index = 0; index < stringLiterals.Count; index++)
		{
			dataIndices[index] = bytes.Count;
			string stringLiteral = stringLiterals[index].Key;
			bytes.AddRange(Encoding.UTF8.GetBytes(stringLiteral));
			context.Global.Collectors.Stats.RecordStringLiteral(stringLiteral);
		}
		byte[] stringLiteralBytes = new byte[stringLiterals.Count * 8];
		for (int i = 0; i < stringLiterals.Count; i++)
		{
			string stringLiteral2 = stringLiterals[i].Key;
			ToBytes(Encoding.UTF8.GetByteCount(stringLiteral2), stringLiteralBytes, i * 8);
			ToBytes(dataIndices[i], stringLiteralBytes, i * 8 + 4);
		}
		stringLiteralStream.Write(stringLiteralBytes, 0, stringLiteralBytes.Length);
		stringLiteralDataStream.Write(bytes.ToArray(), 0, bytes.Count);
	}

	private static void ToBytes(int value, byte[] bytes, int offset)
	{
		ToBytes((uint)value, bytes, offset);
	}

	private static void ToBytes(uint value, byte[] bytes, int offset)
	{
		bytes[offset] = (byte)(value & 0xFF);
		bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
		bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
		bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
	}
}
