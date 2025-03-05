using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class ParameterWriter : BaseSingleSectionWriter
{
	public override string Name => "Parameters";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		foreach (var (parameter, parameterInfo) in metadataCollector.GetParameters())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(parameter.Name));
			stream.WriteUInt(parameter.MetadataToken.ToUInt32());
			stream.WriteInt(typeResults.GetIndex(parameterInfo.ParameterType));
		}
	}
}
