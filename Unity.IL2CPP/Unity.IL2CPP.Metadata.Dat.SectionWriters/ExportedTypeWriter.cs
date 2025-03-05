using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class ExportedTypeWriter : BaseSingleSectionWriter
{
	public override string Name => "Exported Types";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		foreach (TypeReference exportedType in metadataCollector.GetExportedTypes())
		{
			TypeDefinition exportedTypeDefinition = exportedType.Resolve();
			stream.WriteInt((exportedTypeDefinition != null) ? metadataCollector.GetTypeInfoIndex(exportedTypeDefinition) : (-1));
		}
	}
}
