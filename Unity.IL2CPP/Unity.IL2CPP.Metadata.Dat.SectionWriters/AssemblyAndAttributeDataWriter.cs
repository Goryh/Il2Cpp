using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class AssemblyAndAttributeDataWriter : BaseManySectionsWriter
{
	public override string Name => "Assembly & Attribute Data";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		ReadOnlyDictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData> attributeCollections = context.Global.Results.PrimaryCollection.AttributeSupportData;
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		List<ReadOnlyCollectedAttributeSupportData> attributeCollectionsInModuleOrder = new List<ReadOnlyCollectedAttributeSupportData>(attributeCollections.Count);
		int customAttributeTypeTokenStart = 0;
		using (tinyProfiler.Section("Images"))
		{
			WriteImages("Images", factory, metadataCollector, attributeCollections, ref customAttributeTypeTokenStart, attributeCollectionsInModuleOrder);
		}
		using (tinyProfiler.Section("Assemblies"))
		{
			WriteAssemblies("Assemblies", factory, metadataCollector);
		}
		using (tinyProfiler.Section("Field Refs"))
		{
			WriteFieldRefs("Field Refs", factory, context.Global.Results.SecondaryCollection.FieldReferenceTable, typeResults);
		}
		using (tinyProfiler.Section("Referenced Assemblies"))
		{
			WriteReferencedAssemblies("Referenced Assemblies", factory, metadataCollector);
		}
		List<(MetadataToken, uint)> attributeDataOffsets = new List<(MetadataToken, uint)>(attributeCollectionsInModuleOrder.Sum((ReadOnlyCollectedAttributeSupportData a) => a.AttributeClasses.Count) + 1);
		using (tinyProfiler.Section("Attribute Data"))
		{
			WriteAttributeData("Attribute Data", factory, typeResults, attributeCollectionsInModuleOrder, attributeDataOffsets, metadataCollector);
		}
		using (tinyProfiler.Section("Attribute Data Ranges"))
		{
			WriteAttributeDataRanges("Attribute Data Ranges", factory, attributeDataOffsets);
		}
	}

	private void WriteAttributeDataRanges(string name, SectionFactory factory, List<(MetadataToken token, uint offset)> attributeDataOffsets)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (var dataOffset in attributeDataOffsets)
		{
			stream.WriteUInt(dataOffset.token.ToUInt32());
			stream.WriteUInt(dataOffset.offset);
		}
	}

	private void WriteAttributeData(string name, SectionFactory factory, ITypeCollectorResults typeResults, List<ReadOnlyCollectedAttributeSupportData> attributeCollectionsInModuleOrder, List<(MetadataToken token, uint offset)> attributeDataOffsets, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (ReadOnlyCollectedAttributeSupportData item in attributeCollectionsInModuleOrder)
		{
			foreach (AttributeClassCollectionData attributesOwner in item.AttributeClasses)
			{
				attributeDataOffsets.Add((attributesOwner.MetadataToken, (uint)stream.Position));
				MetadataUtils.WriteCompressedUInt32(stream, (uint)attributesOwner.AttributeData.Count);
				foreach (CustomAttributeCollectionData attrData in attributesOwner.AttributeData)
				{
					stream.WriteInt(metadataCollector.GetMethodIndex(attrData.Constructor));
				}
				foreach (CustomAttributeCollectionData attrData2 in attributesOwner.AttributeData)
				{
					MetadataUtils.WriteCompressedUInt32(stream, (uint)attrData2.Arguments.Count);
					MetadataUtils.WriteCompressedUInt32(stream, (uint)attrData2.Fields.Count);
					MetadataUtils.WriteCompressedUInt32(stream, (uint)attrData2.Properties.Count);
					foreach (CustomAttributeArgumentData argument in attrData2.Arguments)
					{
						WriteCustomAttributeArgument(typeResults, stream, argument);
					}
					foreach (CustomAttributeNamedArgumentData field in attrData2.Fields)
					{
						CustomAttributeNamedArgumentData arg = field;
						FieldDefinition fieldDefinition = (FieldDefinition)arg.FieldOrProperty;
						int fieldIndex = metadataCollector.GetFieldIndex(fieldDefinition) - metadataCollector.GetFieldIndex(fieldDefinition.DeclaringType.Fields[0]);
						WriteCustomAttributeNamedArgument(typeResults, metadataCollector, stream, attrData2, in arg, fieldIndex);
					}
					foreach (CustomAttributeNamedArgumentData property in attrData2.Properties)
					{
						CustomAttributeNamedArgumentData arg2 = property;
						PropertyDefinition propertyDefinition = (PropertyDefinition)arg2.FieldOrProperty;
						int propertyIndex = metadataCollector.GetPropertyIndex(propertyDefinition) - metadataCollector.GetPropertyIndex(propertyDefinition.DeclaringType.Properties[0]);
						WriteCustomAttributeNamedArgument(typeResults, metadataCollector, stream, attrData2, in arg2, propertyIndex);
					}
				}
			}
		}
		attributeDataOffsets.Add((MetadataToken.AssemblyZero, (uint)stream.Position));
		while (stream.Position % 4 != 0L)
		{
			stream.WriteByte(204);
		}
	}

	private void WriteReferencedAssemblies(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (AssemblyDefinition referencedAssembly in metadataCollector.GetReferencedAssemblyTable())
		{
			stream.WriteInt(metadataCollector.GetAssemblyIndex(referencedAssembly));
		}
	}

	private void WriteFieldRefs(string name, SectionFactory factory, ReadOnlyFieldReferenceTable fieldReferenceTable, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		KeyValuePair<int, int>[] array = fieldReferenceTable.Items.Select((KeyValuePair<Il2CppRuntimeFieldReference, uint> item) => new KeyValuePair<int, int>(typeResults.GetIndex(item.Key.DeclaringTypeData), item.Key.Field.FieldIndex)).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<int, int> fieldRef = array[i];
			stream.WriteInt(fieldRef.Key);
			stream.WriteInt(fieldRef.Value);
		}
	}

	private void WriteAssemblies(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (AssemblyDefinition assembly in metadataCollector.GetAssemblies())
		{
			stream.WriteInt(metadataCollector.GetModuleIndex(assembly.MainModule));
			stream.WriteUInt(assembly.MetadataToken.ToUInt32());
			int length;
			int firstIndex = metadataCollector.GetFirstIndexInReferencedAssemblyTableForAssembly(assembly, out length);
			stream.WriteInt(firstIndex);
			stream.WriteInt(length);
			stream.WriteInt(metadataCollector.GetStringIndex(assembly.Name.Name));
			stream.WriteInt(metadataCollector.GetStringIndex(assembly.Name.Culture));
			stream.WriteInt(metadataCollector.GetStringIndex(MetadataCollector.NameOfAssemblyPublicKeyData(assembly.Name)));
			stream.WriteUInt((uint)assembly.Name.HashAlgorithm);
			stream.WriteInt(assembly.Name.Hash.Length);
			stream.WriteUInt((uint)assembly.Name.Attributes);
			stream.WriteInt(assembly.Name.Version.Major);
			stream.WriteInt(assembly.Name.Version.Minor);
			stream.WriteInt(assembly.Name.Version.Build);
			stream.WriteInt(assembly.Name.Version.Revision);
			byte[] array = ((assembly.Name.PublicKeyToken.Length != 0) ? assembly.Name.PublicKeyToken : new byte[8]);
			foreach (byte b in array)
			{
				stream.WriteByte(b);
			}
		}
	}

	private void WriteImages(string name, SectionFactory factory, IMetadataCollectionResults metadataCollector, ReadOnlyDictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData> attributeCollections, ref int customAttributeTypeTokenStart, List<ReadOnlyCollectedAttributeSupportData> attributeCollectionsInModuleOrder)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (ModuleDefinition image in metadataCollector.GetModules())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(image.GetModuleFileName()));
			stream.WriteInt(metadataCollector.GetAssemblyIndex(image.Assembly));
			stream.WriteInt(metadataCollector.GetLowestTypeInfoIndexForModule(image));
			stream.WriteInt(image.Assembly.GetAllTypes().Count);
			stream.WriteInt(image.HasExportedTypes ? metadataCollector.GetLowestExportedTypeIndexForModule(image) : (-1));
			stream.WriteInt(image.ExportedTypes.Count);
			stream.WriteInt((image.Assembly.EntryPoint == null) ? (-1) : metadataCollector.GetMethodIndex(image.Assembly.EntryPoint));
			stream.WriteUInt(image.MetadataToken.ToUInt32());
			ReadOnlyCollectedAttributeSupportData attributeCollection = attributeCollections[image.Assembly];
			stream.WriteInt(customAttributeTypeTokenStart);
			stream.WriteInt(attributeCollection.AttributeClasses.Count);
			attributeCollectionsInModuleOrder.Add(attributeCollection);
			customAttributeTypeTokenStart += attributeCollection.AttributeClasses.Count;
		}
	}

	private static void WriteCustomAttributeArgument(ITypeCollectorResults types, Stream writer, in CustomAttributeArgumentData arg)
	{
		WriteArgumentTypeCode(types, writer, arg.Type);
		WriteCustomAttributeArgumentData(types, writer, arg.Data, arg.Type);
	}

	private static void WriteCustomAttributeNamedArgument(ITypeCollectorResults types, IMetadataCollectionResults metadata, Stream writer, CustomAttributeCollectionData data, in CustomAttributeNamedArgumentData arg, int memberIndex)
	{
		WriteCustomAttributeArgument(types, writer, in arg.Argument);
		if (data.AttributeType == arg.FieldOrProperty.DeclaringType)
		{
			MetadataUtils.WriteCompressedInt32(writer, memberIndex);
			return;
		}
		MetadataUtils.WriteCompressedInt32(writer, -memberIndex - 1);
		MetadataUtils.WriteCompressedUInt32(writer, (uint)metadata.GetTypeInfoIndex(arg.FieldOrProperty.DeclaringType));
	}

	private static void WriteArgumentTypeCode(ITypeCollectorResults types, Stream writer, IIl2CppRuntimeType type)
	{
		writer.WriteByte((byte)Il2CppTypeSupport.ValueFor(type.Type, useIl2CppExtensions: true));
		if (type.Type.IsEnum)
		{
			MetadataUtils.WriteCompressedInt32(writer, types.GetIndex(type));
		}
	}

	private static void WriteCustomAttributeArgumentData(ITypeCollectorResults types, Stream writer, AttributeDataItem item, IIl2CppRuntimeType type)
	{
		switch (item.Type)
		{
		case AttributeDataItemType.Null:
			MetadataUtils.WriteCompressedInt32(writer, -1);
			break;
		case AttributeDataItemType.Type:
			MetadataUtils.WriteCompressedInt32(writer, types.GetIndex(item.TypeData));
			break;
		case AttributeDataItemType.BinaryData:
			writer.Write(item.Data, 0, item.Data.Length);
			break;
		case AttributeDataItemType.TypeArray:
		case AttributeDataItemType.EnumArray:
		{
			MetadataUtils.WriteCompressedInt32(writer, item.Data.Length);
			WriteArgumentTypeCode(types, writer, ((Il2CppArrayRuntimeType)type).ElementType);
			writer.WriteByte(0);
			AttributeDataItem[] dataArray = item.DataArray;
			foreach (AttributeDataItem dataItem2 in dataArray)
			{
				WriteCustomAttributeArgumentData(types, writer, dataItem2, null);
			}
			break;
		}
		case AttributeDataItemType.ObjectArray:
		{
			MetadataUtils.WriteCompressedInt32(writer, item.Data.Length);
			writer.WriteByte(28);
			writer.WriteByte(1);
			CustomAttributeArgumentData[] nestedDataArray = item.NestedDataArray;
			for (int i = 0; i < nestedDataArray.Length; i++)
			{
				CustomAttributeArgumentData dataItem = nestedDataArray[i];
				WriteCustomAttributeArgument(types, writer, in dataItem);
			}
			break;
		}
		}
	}
}
