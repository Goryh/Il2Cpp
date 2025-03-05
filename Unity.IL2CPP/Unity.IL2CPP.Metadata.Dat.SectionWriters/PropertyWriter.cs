using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class PropertyWriter : BaseSingleSectionWriter
{
	public override string Name => "Properties";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		foreach (PropertyDefinition property in metadataCollector.GetProperties())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(property.Name));
			stream.WriteInt((property.GetMethod != null) ? (metadataCollector.GetMethodIndex(property.GetMethod) - metadataCollector.GetMethodIndex(property.DeclaringType.Methods[0])) : (-1));
			stream.WriteInt((property.SetMethod != null) ? (metadataCollector.GetMethodIndex(property.SetMethod) - metadataCollector.GetMethodIndex(property.DeclaringType.Methods[0])) : (-1));
			stream.WriteInt((int)property.Attributes);
			stream.WriteUInt(property.MetadataToken.ToUInt32());
		}
	}
}
