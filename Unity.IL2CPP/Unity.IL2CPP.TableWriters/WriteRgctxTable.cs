using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public class WriteRgctxTable : BasicWriterBase
{
	private readonly ReadOnlyCollection<AssemblyCodeMetadata> _unorderedCodeMetadata;

	public WriteRgctxTable(ReadOnlyCollection<AssemblyCodeMetadata> unorderedCodeMetadata)
	{
		_unorderedCodeMetadata = unorderedCodeMetadata;
	}

	private static ReadOnlyCollection<RgctxEntryName> AggregateAllRgctxEntryNames(IEnumerable<ReadOnlyCollection<RgctxEntryName>> rgctxIndexNameSets)
	{
		HashSet<RgctxEntryName> rgctxIndexNames = new HashSet<RgctxEntryName>(new RgctxEntryNameComparer());
		foreach (ReadOnlyCollection<RgctxEntryName> assemblyRgctxIndexNames in rgctxIndexNameSets)
		{
			rgctxIndexNames.UnionWith(assemblyRgctxIndexNames);
		}
		return rgctxIndexNames.ToSortedCollection(new RgctxEntryNameComparer());
	}

	protected override void WriteFile(SourceWritingContext context)
	{
		ReadOnlyCollection<RgctxEntryName> rgctxEntryNames = AggregateAllRgctxEntryNames(_unorderedCodeMetadata.Select((AssemblyCodeMetadata d) => d.RgctxEntryNames));
		if (!rgctxEntryNames.Any())
		{
			return;
		}
		using IGeneratedCodeStream writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppRgctxTable.c");
		ITypeCollectorResults types = context.Global.PrimaryWriteResults.Types;
		IGenericMethodCollectorResults genericMethods = context.Global.Results.PrimaryWrite.GenericMethods;
		IMetadataCollectionResults metadata = context.Global.Results.PrimaryCollection.Metadata;
		foreach (RgctxEntryName rgctx in rgctxEntryNames)
		{
			switch (rgctx.Entry.Type)
			{
			case RGCTXType.Type:
			case RGCTXType.Class:
			case RGCTXType.Array:
			{
				IGeneratedCodeStream generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"const uint32_t {rgctx.Name} = {types.GetIndex(rgctx.Entry.RuntimeType)};");
				break;
			}
			case RGCTXType.Method:
			{
				IGeneratedCodeStream generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"const uint32_t {rgctx.Name} = {genericMethods.GetIndex(rgctx.Entry.MethodReference)};");
				break;
			}
			case RGCTXType.Constrained:
			{
				IGeneratedCodeStream generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"const Il2CppRGCTXConstrainedData {rgctx.Name} = {{{types.GetIndex(rgctx.Entry.RuntimeType)},{MetadataUtils.GetEncodedMethodMetadataUsageIndex(rgctx.Entry.MethodReference, metadata, genericMethods)}}};");
				break;
			}
			default:
				throw new InvalidOperationException($"Attempt to get metadata token for invalid ${"RGCTXType"} {rgctx.Entry.Type}");
			}
		}
	}
}
