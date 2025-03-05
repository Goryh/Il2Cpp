using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public abstract class BaseManySectionsWriter : BaseSectionWriter
{
	public class SectionFactory
	{
		private readonly List<DatSection> _sections = new List<DatSection>();

		public ReadOnlyCollection<DatSection> Complete()
		{
			return _sections.AsReadOnly();
		}

		public MemoryStream CreateSection(string name, int alignment = 0)
		{
			DatSection section = new DatSection(name, new MemoryStream(), alignment);
			_sections.Add(section);
			return section.Stream;
		}
	}

	public override ReadOnlyCollection<DatSection> Write(SourceWritingContext context, int alignment = 0)
	{
		SectionFactory sections = new SectionFactory();
		WriteToStreams(context, sections);
		return sections.Complete();
	}

	protected abstract void WriteToStreams(SourceWritingContext context, SectionFactory factory);
}
