using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class MethodWriter : BaseSingleSectionWriter
{
	public override string Name => "Methods";

	protected override void WriteToStream(SourceWritingContext context, MemoryStream stream)
	{
		IMetadataCollectionResults metadataCollector = context.Global.PrimaryCollectionResults.Metadata;
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		IVTableBuilderService vTableBuilder = context.Global.Services.VTable;
		foreach (var (method, methodInfo) in metadataCollector.GetMethods())
		{
			stream.WriteInt(metadataCollector.GetStringIndex(method.Name));
			stream.WriteInt(metadataCollector.GetTypeInfoIndex(method.DeclaringType));
			stream.WriteInt(typeResults.GetIndex(methodInfo.ReturnType));
			stream.WriteUInt(method.MethodReturnType.MetadataToken.ToUInt32());
			stream.WriteInt(method.HasParameters ? metadataCollector.GetParameterIndex(method.Parameters[0]) : (-1));
			stream.WriteInt(metadataCollector.GetGenericContainerIndex(method));
			stream.WriteUInt(method.MetadataToken.ToUInt32());
			stream.WriteUShort((ushort)method.Attributes);
			ushort implAttributes = (ushort)method.ImplAttributes;
			if (method.IsUnmanagedCallersOnly)
			{
				implAttributes |= 0x8000;
			}
			stream.WriteUShort(implAttributes);
			stream.WriteUShort(method.IsStripped ? ushort.MaxValue : ((ushort)vTableBuilder.IndexFor(context, method)));
			stream.WriteUShort((ushort)method.Parameters.Count);
		}
	}
}
