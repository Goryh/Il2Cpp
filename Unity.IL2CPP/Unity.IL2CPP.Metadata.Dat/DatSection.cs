using System;
using System.IO;

namespace Unity.IL2CPP.Metadata.Dat;

public class DatSection : IDisposable
{
	public readonly string Name;

	public readonly MemoryStream Stream;

	public readonly int SectionAlignment;

	public DatSection(string name, MemoryStream stream, int sectionAlignment)
	{
		SectionAlignment = sectionAlignment;
		Name = name;
		Stream = stream;
	}

	public void Dispose()
	{
		Stream.Dispose();
	}
}
