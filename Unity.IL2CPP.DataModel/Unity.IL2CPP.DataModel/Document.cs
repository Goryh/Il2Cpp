using NiceIO;

namespace Unity.IL2CPP.DataModel;

public class Document
{
	public NPath Url { get; }

	public byte[] Hash { get; }

	public Document(string url, byte[] hash)
	{
		Url = url.ToNPath();
		Hash = hash;
	}
}
