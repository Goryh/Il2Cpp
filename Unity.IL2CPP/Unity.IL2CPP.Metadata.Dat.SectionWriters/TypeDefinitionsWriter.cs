using System;
using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class TypeDefinitionsWriter : BaseSingleSectionWriter
{
	protected enum PackingSize
	{
		Zero,
		One,
		Two,
		Four,
		Eight,
		Sixteen,
		ThirtyTwo,
		SixtyFour,
		OneHundredTwentyEight
	}

	public override string Name => "Type Definitions";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		IVTableBuilderService vTableBuilder = context.Global.Services.VTable;
		foreach (KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> typeInfo2 in metadataCollector.GetTypeInfos())
		{
			typeInfo2.Deconstruct(out var key, out var value);
			TypeDefinition type = key;
			MetadataTypeDefinitionInfo typeInfo = value;
			int vtableMethodCount = 0;
			int interfaceOffsetsCount = 0;
			if (!type.IsInterface || type.IsComOrWindowsRuntimeType() || context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(type) != null)
			{
				VTable vTable = vTableBuilder.VTableFor(context, type);
				vtableMethodCount = vTable.Slots.Count;
				interfaceOffsetsCount = vTable.InterfaceOffsets.Count;
			}
			int methodCount = type.Methods.Count;
			int methodIndex = ((methodCount > 0) ? metadataCollector.GetMethodIndex(type.Methods[0]) : (-1));
			stream.WriteInt(metadataCollector.GetStringIndex(type.Name));
			stream.WriteInt(metadataCollector.GetStringIndex(type.Namespace));
			stream.WriteInt(typeResults.GetIndex(typeInfo.RuntimeType));
			stream.WriteInt((typeInfo.DeclaringRuntimeType != null) ? typeResults.GetIndex(typeInfo.DeclaringRuntimeType) : (-1));
			stream.WriteInt((typeInfo.BaseRuntimeType != null) ? typeResults.GetIndex(typeInfo.BaseRuntimeType) : (-1));
			stream.WriteInt((typeInfo.ElementRuntimeType != null) ? typeResults.GetIndex(typeInfo.ElementRuntimeType) : (-1));
			stream.WriteInt(metadataCollector.GetGenericContainerIndex(type));
			stream.WriteUInt((uint)type.Attributes);
			stream.WriteInt(type.HasFields ? metadataCollector.GetFieldIndex(type.Fields[0]) : (-1));
			stream.WriteInt(methodIndex);
			stream.WriteInt(type.HasEvents ? metadataCollector.GetEventIndex(type.Events[0]) : (-1));
			stream.WriteInt(type.HasProperties ? metadataCollector.GetPropertyIndex(type.Properties[0]) : (-1));
			stream.WriteInt(type.HasNestedTypes ? metadataCollector.GetNestedTypesStartIndex(type) : (-1));
			stream.WriteInt(type.HasInterfaces ? metadataCollector.GetInterfacesStartIndex(type) : (-1));
			stream.WriteInt(metadataCollector.GetVTableMethodsStartIndex(type));
			stream.WriteInt((interfaceOffsetsCount > 0) ? metadataCollector.GetInterfaceOffsetsStartIndex(type) : (-1));
			stream.WriteIntAsUShort(methodCount);
			stream.WriteIntAsUShort(type.Properties.Count);
			stream.WriteIntAsUShort(type.Fields.Count);
			stream.WriteIntAsUShort(type.Events.Count);
			stream.WriteIntAsUShort(type.NestedTypes.Count);
			stream.WriteIntAsUShort(vtableMethodCount);
			stream.WriteIntAsUShort(type.Interfaces.Count);
			stream.WriteIntAsUShort(interfaceOffsetsCount);
			int bitfield = 0;
			bitfield |= (type.IsValueType ? 1 : 0);
			bitfield |= (int)((type.IsEnum ? 1u : 0u) << 1);
			bitfield |= (int)((type.HasFinalizer() ? 1u : 0u) << 2);
			bitfield |= (int)((type.HasStaticConstructor ? 1u : 0u) << 3);
			bitfield |= (int)((MarshalingUtils.IsBlittable(context, type, null, MarshalType.PInvoke, useUnicodeCharset: false) ? 1u : 0u) << 4);
			bitfield |= (int)((type.IsComOrWindowsRuntimeType() ? 1u : 0u) << 5);
			int packingSize = TypeDefinitionWriter.AlignmentPackingSizeFor(type);
			if (packingSize != -1)
			{
				bitfield |= (int)ConvertPackingSizeToCompressedEnum(packingSize) << 6;
			}
			bitfield |= (int)(((type.PackingSize == -1) ? 1u : 0u) << 10);
			bitfield |= (int)(((type.ClassSize == -1) ? 1u : 0u) << 11);
			bitfield |= (int)((type.IsByRefLike ? 1u : 0u) << 16);
			short specified = type.PackingSize;
			if (specified != -1)
			{
				bitfield |= (int)ConvertPackingSizeToCompressedEnum(specified) << 12;
			}
			stream.WriteInt(bitfield);
			stream.WriteUInt(type.MetadataToken.ToUInt32());
		}
	}

	protected static PackingSize ConvertPackingSizeToCompressedEnum(int packingSize)
	{
		return packingSize switch
		{
			0 => PackingSize.Zero, 
			1 => PackingSize.One, 
			2 => PackingSize.Two, 
			4 => PackingSize.Four, 
			8 => PackingSize.Eight, 
			16 => PackingSize.Sixteen, 
			32 => PackingSize.ThirtyTwo, 
			64 => PackingSize.SixtyFour, 
			128 => PackingSize.OneHundredTwentyEight, 
			_ => throw new InvalidOperationException($"The packing size of {packingSize} is not valid. Valid values are 0, 1, 2, 4, 8, 16, 32, 64, or 128."), 
		};
	}
}
