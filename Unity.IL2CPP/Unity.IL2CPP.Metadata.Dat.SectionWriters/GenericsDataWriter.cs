using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class GenericsDataWriter : BaseManySectionsWriter
{
	public override string Name => "Generics Data";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Generic Parameters"))
		{
			WriteGenericParameters("Generic Parameters", factory, metadataCollector);
		}
		using (tinyProfiler.Section("Generic Parameter Constraints"))
		{
			WriteGenericParameterConstraints("Generic Parameter Constraints", factory, metadataCollector, typeResults);
		}
		using (tinyProfiler.Section("Generic Containers"))
		{
			WriteGenericContainers("Generic Containers", factory, metadataCollector);
		}
	}

	private void WriteGenericContainers(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (IGenericParameterProvider provider in metadataCollector.GetGenericContainers())
		{
			stream.WriteInt((provider.GenericParameterType == GenericParameterType.Method) ? metadataCollector.GetMethodIndex((MethodDefinition)provider) : metadataCollector.GetTypeInfoIndex((TypeDefinition)provider));
			stream.WriteInt(provider.GenericParameters.Count);
			stream.WriteInt((provider.GenericParameterType == GenericParameterType.Method) ? 1 : 0);
			stream.WriteInt(metadataCollector.GetGenericParameterIndex(provider.GenericParameters[0]));
		}
	}

	private void WriteGenericParameterConstraints(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (IIl2CppRuntimeType constraintTypeIndex in metadataCollector.GetGenericParameterConstraints())
		{
			stream.WriteInt(typeResults.GetIndex(constraintTypeIndex));
		}
	}

	private void WriteGenericParameters(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (GenericParameter gp in metadataCollector.GetGenericParameters())
		{
			stream.WriteInt(metadataCollector.GetGenericContainerIndex(gp.Owner));
			stream.WriteInt(metadataCollector.GetStringIndex(gp.Name));
			stream.WriteShort((short)((gp.Constraints.Count > 0) ? metadataCollector.GetGenericParameterConstraintsStartIndex(gp) : 0));
			stream.WriteShort((short)gp.Constraints.Count);
			stream.WriteUShort((ushort)gp.Position);
			stream.WriteUShort((ushort)gp.Attributes);
		}
	}
}
