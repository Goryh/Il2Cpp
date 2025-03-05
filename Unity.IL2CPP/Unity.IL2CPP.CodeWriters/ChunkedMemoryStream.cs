using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public class ChunkedMemoryStream : Stream
{
	private readonly List<byte[]> _buffers = new List<byte[]>();

	private byte[] _currentBuffer;

	private int _currentBufferIndex;

	private int _currentBufferOffset;

	private int _length;

	private bool _canWrite = true;

	private readonly IChunkedMemoryStreamBufferProvider _bufferProvider;

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => _canWrite;

	public override long Length => _length;

	public override long Position
	{
		get
		{
			return _currentBufferIndex * _bufferProvider.BufferSize + _currentBufferOffset;
		}
		set
		{
			Seek(value, SeekOrigin.Begin);
		}
	}

	public ChunkedMemoryStream(ReadOnlyContext context)
		: this(context.Global.Services.Factory.ChunkedMemoryStreamProvider)
	{
	}

	public ChunkedMemoryStream(IChunkedMemoryStreamBufferProvider bufferProvider)
	{
		_bufferProvider = bufferProvider;
		_buffers.Add(_bufferProvider.Get());
		_currentBuffer = _buffers[0];
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int totalRead = 0;
		while (totalRead < count && Position < Length)
		{
			int bufferRemaining = (int)Math.Min(_bufferProvider.BufferSize - _currentBufferOffset, Length - Position);
			int readAmount = Math.Min(count - totalRead, bufferRemaining);
			if (readAmount > 0)
			{
				Buffer.BlockCopy(_currentBuffer, _currentBufferOffset, buffer, offset + totalRead, readAmount);
				totalRead += readAmount;
				_currentBufferOffset += readAmount;
			}
			if (totalRead < count && _currentBufferIndex < _buffers.Count - 1)
			{
				_currentBufferIndex++;
				_currentBufferOffset = 0;
				_currentBuffer = _buffers[_currentBufferIndex];
			}
		}
		return totalRead;
	}

	private unsafe void CopySpanToCurrentBuffer(ReadOnlySpan<byte> buffer, int srcOffset, int writeAmount)
	{
		fixed (byte* dst = _currentBuffer)
		{
			fixed (byte* src = buffer)
			{
				Unsafe.CopyBlock(dst + _currentBufferOffset, src + srcOffset, (uint)writeAmount);
			}
		}
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		int count = buffer.Length;
		if ((long)_length + (long)count > int.MaxValue)
		{
			throw new OverflowException();
		}
		if (!_canWrite)
		{
			throw new InvalidOperationException("This stream is closed for writing");
		}
		int totalWritten = 0;
		while (totalWritten < count)
		{
			int writeAmount = Math.Min(_bufferProvider.BufferSize - _currentBufferOffset, count - totalWritten);
			if (writeAmount > 0)
			{
				CopySpanToCurrentBuffer(buffer, totalWritten, writeAmount);
				totalWritten += writeAmount;
				_currentBufferOffset += writeAmount;
			}
			if (totalWritten < count)
			{
				if (_currentBufferIndex == _buffers.Count - 1)
				{
					_buffers.Add(_bufferProvider.Get());
				}
				_currentBufferIndex++;
				_currentBuffer = _buffers[_currentBufferIndex];
				_currentBufferOffset = 0;
			}
		}
		_length += totalWritten;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if ((long)_length + (long)count > int.MaxValue)
		{
			throw new OverflowException();
		}
		if (!_canWrite)
		{
			throw new InvalidOperationException("This stream is closed for writing");
		}
		int totalWritten = 0;
		while (totalWritten < count)
		{
			int writeAmount = Math.Min(_bufferProvider.BufferSize - _currentBufferOffset, count - totalWritten);
			if (writeAmount > 0)
			{
				Buffer.BlockCopy(buffer, offset + totalWritten, _currentBuffer, _currentBufferOffset, writeAmount);
				totalWritten += writeAmount;
				_currentBufferOffset += writeAmount;
			}
			if (totalWritten < count)
			{
				if (_currentBufferIndex == _buffers.Count - 1)
				{
					_buffers.Add(_bufferProvider.Get());
				}
				_currentBufferIndex++;
				_currentBuffer = _buffers[_currentBufferIndex];
				_currentBufferOffset = 0;
			}
		}
		_length += totalWritten;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		if (origin != 0)
		{
			throw new NotSupportedException($"Only {0} is supported");
		}
		if (offset < 0 || offset > _length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		_canWrite = false;
		_currentBufferIndex = (int)((offset - 1) / _bufferProvider.BufferSize);
		_currentBufferOffset = (int)((offset - 1) % _bufferProvider.BufferSize) + 1;
		if (_currentBufferOffset == _bufferProvider.BufferSize && _currentBufferIndex < _buffers.Count - 1)
		{
			_currentBufferIndex++;
			_currentBufferOffset = 0;
		}
		_currentBuffer = _buffers[_currentBufferIndex];
		return offset;
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		while (Position < Length)
		{
			int bufferRemaining = (int)Math.Min(_bufferProvider.BufferSize - _currentBufferOffset, Length - Position);
			destination.Write(_currentBuffer, _currentBufferOffset, bufferRemaining);
			Seek(_currentBufferIndex * _bufferProvider.BufferSize + _currentBufferOffset + bufferRemaining, SeekOrigin.Begin);
		}
	}

	public override void SetLength(long value)
	{
		if (value != 0L)
		{
			throw new NotSupportedException();
		}
		Seek(0L, SeekOrigin.Begin);
		_canWrite = true;
		_length = 0;
		_currentBuffer = _buffers[0];
		_currentBufferIndex = 0;
		_currentBufferOffset = 0;
	}

	public override void Flush()
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (byte[] buffer in _buffers)
			{
				_bufferProvider.Return(buffer);
			}
		}
		base.Dispose(disposing);
	}

	public byte[] ToArray()
	{
		if (Length < _bufferProvider.BufferSize)
		{
			if (Length == 0L)
			{
				return Array.Empty<byte>();
			}
			byte[] newArray = new byte[_length];
			Buffer.BlockCopy(_currentBuffer, 0, newArray, 0, _length);
			return newArray;
		}
		long savePos = Position;
		MemoryStream ms = new MemoryStream(_length);
		Seek(0L, SeekOrigin.Begin);
		CopyTo(ms);
		Seek(savePos, SeekOrigin.Begin);
		return ms.GetBuffer();
	}

	public byte[] GetBuffer()
	{
		if (Length < _bufferProvider.BufferSize)
		{
			return _currentBuffer;
		}
		return ToArray();
	}
}
