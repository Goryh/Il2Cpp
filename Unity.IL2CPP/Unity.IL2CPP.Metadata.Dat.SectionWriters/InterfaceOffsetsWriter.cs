using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class InterfaceOffsetsWriter : BaseSingleSectionWriter
{
	public override string Name => "Interface Offsets";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadata = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		foreach (InterfaceOffset interfaceOffset in metadata.GetInterfaceOffsets())
		{
			stream.WriteInt(typeResults.GetIndex(interfaceOffset.RuntimeType));
			stream.WriteInt(interfaceOffset.Offset);
		}
	}
}
