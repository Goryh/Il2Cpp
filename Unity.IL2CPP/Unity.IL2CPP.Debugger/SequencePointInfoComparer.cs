using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Debugger;

public class SequencePointInfoComparer : IEqualityComparer<SequencePointInfo>
{
	public bool Equals(SequencePointInfo x, SequencePointInfo y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		MethodDefinition method = x.Method;
		MethodDefinition yMethod = y.Method;
		if (method == yMethod && x.Kind == y.Kind && x.SourceFile == y.SourceFile && SourceFileHashEqual(x.SourceFileHash, y.SourceFileHash) && x.StartLine == y.StartLine && x.EndLine == y.EndLine && x.StartColumn == y.StartColumn && x.EndColumn == y.EndColumn)
		{
			return x.IlOffset == y.IlOffset;
		}
		return false;
	}

	private bool SourceFileHashEqual(byte[] x, byte[] y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		return x.SequenceEqual(y);
	}

	public int GetHashCode(SequencePointInfo obj)
	{
		int hash = obj.Method.GetHashCode();
		hash = HashCodeHelper.Combine(hash, (int)obj.Kind);
		hash = HashCodeHelper.Combine(hash, obj.SourceFile.GetStableHashCode());
		if (obj.SourceFileHash != null)
		{
			byte[] sourceFileHash = obj.SourceFileHash;
			foreach (byte b in sourceFileHash)
			{
				hash = HashCodeHelper.Combine(hash, b);
			}
		}
		hash = HashCodeHelper.Combine(hash, obj.StartLine);
		hash = HashCodeHelper.Combine(hash, obj.EndLine);
		hash = HashCodeHelper.Combine(hash, obj.StartColumn);
		hash = HashCodeHelper.Combine(hash, obj.EndColumn);
		return HashCodeHelper.Combine(hash, obj.IlOffset);
	}
}
