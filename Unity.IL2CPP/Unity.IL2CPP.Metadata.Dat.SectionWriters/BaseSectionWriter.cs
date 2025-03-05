using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public abstract class BaseSectionWriter
{
	public abstract string Name { get; }

	public abstract ReadOnlyCollection<DatSection> Write(SourceWritingContext context, int sectionAlignment = 0);
}
