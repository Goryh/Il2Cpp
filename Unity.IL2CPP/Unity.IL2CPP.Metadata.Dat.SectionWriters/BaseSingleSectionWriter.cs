using System.Collections.ObjectModel;
using System.IO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public abstract class BaseSingleSectionWriter : BaseSectionWriter
{
	public override ReadOnlyCollection<DatSection> Write(SourceWritingContext context, int sectionAlignment = 0)
	{
		DatSection section = new DatSection(Name, new MemoryStream(), sectionAlignment);
		WriteToStream(context, section.Stream);
		return new DatSection[1] { section }.AsReadOnly();
	}

	protected abstract void WriteToStream(SourceWritingContext context, MemoryStream stream);
}
