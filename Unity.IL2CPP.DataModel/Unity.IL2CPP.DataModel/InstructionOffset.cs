using System;

namespace Unity.IL2CPP.DataModel;

public struct InstructionOffset
{
	private readonly int? _offset;

	public int Offset
	{
		get
		{
			if (!_offset.HasValue)
			{
				throw new NotSupportedException();
			}
			return _offset.Value;
		}
	}

	public bool IsEndOfMethod => !_offset.HasValue;

	public InstructionOffset(int? offset)
	{
		_offset = offset;
	}
}
