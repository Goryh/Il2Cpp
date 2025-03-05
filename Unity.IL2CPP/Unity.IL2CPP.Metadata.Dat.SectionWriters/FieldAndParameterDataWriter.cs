using System.IO;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.Fields;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class FieldAndParameterDataWriter : BaseManySectionsWriter
{
	public override string Name => "Field & Parameter Data";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Parameter Default Values"))
		{
			WriteParameterDefaultValues("Parameter Default Values", factory, metadataCollector, typeResults);
		}
		using (tinyProfiler.Section("Field Default Values"))
		{
			WriteFieldDefaultValues("Field Default Values", factory, metadataCollector, typeResults);
		}
		using (tinyProfiler.Section("Field and Parameter Default Values Data"))
		{
			WriteFieldAndParameterDefaultValuesData("Field and Parameter Default Values Data", factory, metadataCollector);
		}
		using (tinyProfiler.Section("Field Marshaled Sizes"))
		{
			WriteFieldMarshaledSizes("Field Marshaled Sizes", factory, metadataCollector, typeResults);
		}
	}

	private void WriteFieldMarshaledSizes(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (FieldMarshaledSize marshaledSize in metadataCollector.GetFieldMarshaledSizes())
		{
			stream.WriteInt(marshaledSize.FieldIndex);
			stream.WriteInt(typeResults.GetIndex(marshaledSize.RuntimeType));
			stream.WriteInt(marshaledSize.Size);
		}
	}

	private void WriteFieldAndParameterDefaultValuesData(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream memoryStream = factory.CreateSection(name, 8);
		byte[] defaultValueData = metadataCollector.GetDefaultValueData().ToArray();
		memoryStream.Write(defaultValueData, 0, defaultValueData.Length);
		memoryStream.AlignTo(4);
	}

	private void WriteFieldDefaultValues(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (FieldDefaultValue defaultValue in metadataCollector.GetFieldDefaultValues())
		{
			stream.WriteInt(defaultValue.FieldIndex);
			stream.WriteInt(typeResults.GetIndex(defaultValue.RuntimeType));
			stream.WriteInt(defaultValue.DataIndex);
		}
	}

	private void WriteParameterDefaultValues(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (ParameterDefaultValue defaultValue in metadataCollector.GetParameterDefaultValues())
		{
			stream.WriteInt(defaultValue.ParameterIndex);
			stream.WriteInt(typeResults.GetIndex(defaultValue.DeclaringType));
			stream.WriteInt(defaultValue.DataIndex);
		}
	}
}
