using System.IO;
using System.Linq;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class MetadataStringWriter : BaseSingleSectionWriter
{
	public override string Name => "Metadata Strings";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		byte[] stringData = context.Global.PrimaryCollectionResults.Metadata.GetStringData().ToArray();
		stream.Write(stringData, 0, stringData.Length);
		stream.AlignTo(4);
	}
}
