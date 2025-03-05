using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class NestedTypesAndInterfacesWriter : BaseManySectionsWriter
{
	public override string Name => "Nested Types & Interfaces";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Nested Types"))
		{
			WriteNestedTypes("Nested Types", factory, metadataCollector);
		}
		using (tinyProfiler.Section("Interfaces"))
		{
			WriteInterfaces("Interfaces", factory, metadataCollector, typeResults);
		}
	}

	private void WriteInterfaces(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (IIl2CppRuntimeType @interface in metadataCollector.GetInterfaces())
		{
			stream.WriteInt(typeResults.GetIndex(@interface));
		}
	}

	private void WriteNestedTypes(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (int nestedTypeIndex in metadataCollector.GetNestedTypes())
		{
			stream.WriteInt(nestedTypeIndex);
		}
	}
}
