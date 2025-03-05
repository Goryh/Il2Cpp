using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class VTableWriter : BaseSingleSectionWriter
{
	public override string Name => "VTables";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		IGenericMethodCollectorResults genericMethods = context.Global.Results.PrimaryWrite.GenericMethods;
		foreach (VTableSlot vTableMethod in metadataCollector.GetVTableMethods())
		{
			uint i = MetadataUtils.GetEncodedMethodMetadataUsageIndex(vTableMethod, metadataCollector, genericMethods);
			stream.WriteUInt(i);
		}
	}
}
