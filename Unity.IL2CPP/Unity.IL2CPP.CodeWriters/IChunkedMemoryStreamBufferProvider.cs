namespace Unity.IL2CPP.CodeWriters;

public interface IChunkedMemoryStreamBufferProvider
{
	int BufferSize { get; }

	byte[] Get();

	void Return(byte[] buffer);
}
