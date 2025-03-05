using System.IO;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class UnresolvedVirtualCallWriter : BaseManySectionsWriter
{
	public override string Name => "Unresolved Virtual Calls";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		ITypeCollectorResults typeResults = context.Global.Results.PrimaryWrite.Types;
		UnresolvedIndirectCallsTableInfo virtualCallTables = context.Global.Results.SecondaryWritePart3.UnresolvedIndirectCallsTableInfo;
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Unresolved Virtual Call Parameter Types"))
		{
			WriteUnresolvedVirtualCallParameterTypes("Unresolved Virtual Call Parameter Types", factory, virtualCallTables, typeResults);
		}
		using (tinyProfiler.Section("Unresolved Virtual Call Parameter Ranges"))
		{
			WriteUnresolvedVirtualCallParameterRanges("Unresolved Virtual Call Parameter Ranges", factory, virtualCallTables);
		}
	}

	private void WriteUnresolvedVirtualCallParameterRanges(string name, SectionFactory factory, UnresolvedIndirectCallsTableInfo virtualCallTables)
	{
		MemoryStream stream = factory.CreateSection(name);
		int start = 0;
		foreach (IndirectCallSignature signature in virtualCallTables.SignatureTypes)
		{
			stream.WriteInt(start);
			stream.WriteInt(signature.Signature.Length);
			start += signature.Signature.Length;
		}
	}

	private void WriteUnresolvedVirtualCallParameterTypes(string name, SectionFactory factory, UnresolvedIndirectCallsTableInfo virtualCallTables, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (IIl2CppRuntimeType signatureType in virtualCallTables.SignatureTypes.SelectMany((IndirectCallSignature s) => s.Signature))
		{
			stream.WriteInt(typeResults.GetIndex(signatureType));
		}
	}
}
