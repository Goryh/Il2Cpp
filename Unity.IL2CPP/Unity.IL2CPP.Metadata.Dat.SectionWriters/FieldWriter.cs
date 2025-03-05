using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class FieldWriter : BaseSingleSectionWriter
{
	public override string Name => "Fields";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		foreach (var (field, fieldInfo) in metadataCollector.GetFields())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(field.Name));
			stream.WriteInt(typeResults.GetIndex(fieldInfo.FieldType));
			stream.WriteUInt(field.MetadataToken.ToUInt32());
		}
	}
}
