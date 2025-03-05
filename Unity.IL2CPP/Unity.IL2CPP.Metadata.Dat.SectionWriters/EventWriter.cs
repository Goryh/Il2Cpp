using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class EventWriter : BaseSingleSectionWriter
{
	public override string Name => "Events";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		foreach (var (@event, eventInfo) in metadataCollector.GetEvents())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(@event.Name));
			stream.WriteInt(typeResults.GetIndex(eventInfo.EventType));
			stream.WriteInt((@event.AddMethod != null) ? (metadataCollector.GetMethodIndex(@event.AddMethod) - metadataCollector.GetMethodIndex(@event.DeclaringType.Methods[0])) : (-1));
			stream.WriteInt((@event.RemoveMethod != null) ? (metadataCollector.GetMethodIndex(@event.RemoveMethod) - metadataCollector.GetMethodIndex(@event.DeclaringType.Methods[0])) : (-1));
			stream.WriteInt((@event.InvokeMethod != null) ? (metadataCollector.GetMethodIndex(@event.InvokeMethod) - metadataCollector.GetMethodIndex(@event.DeclaringType.Methods[0])) : (-1));
			stream.WriteUInt(@event.MetadataToken.ToUInt32());
		}
	}
}
