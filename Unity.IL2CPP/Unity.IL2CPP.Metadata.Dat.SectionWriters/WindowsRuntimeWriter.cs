using System;
using System.IO;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class WindowsRuntimeWriter : BaseManySectionsWriter
{
	public override string Name => "Windows Runtime";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		ITypeCollectorResults typeResults = context.Global.PrimaryWriteResults.Types;
		MetadataStringsCollector windowsRuntimeNames = new MetadataStringsCollector();
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Windows Runtime Type Names"))
		{
			WriteTypeNames("Windows Runtime Type Names", factory, context, windowsRuntimeNames, typeResults);
		}
		using (tinyProfiler.Section("Windows Runtime Strings"))
		{
			WriteStrings("Windows Runtime Strings", factory, windowsRuntimeNames);
		}
	}

	private void WriteStrings(string name, SectionFactory factory, MetadataStringsCollector windowsRuntimeNames)
	{
		MemoryStream memoryStream = factory.CreateSection(name);
		byte[] stringData = windowsRuntimeNames.GetStringData().ToArray();
		memoryStream.Write(stringData, 0, stringData.Length);
		memoryStream.AlignTo(4);
	}

	private void WriteTypeNames(string name, SectionFactory factory, SourceWritingContext context, MetadataStringsCollector windowsRuntimeNames, ITypeCollectorResults typeResults)
	{
		MemoryStream stream = factory.CreateSection(name);
		foreach (Tuple<IIl2CppRuntimeType, string> type in context.Global.Results.PrimaryCollection.WindowsRuntimeTypeWithNames)
		{
			stream.WriteInt(windowsRuntimeNames.AddString(type.Item2));
			stream.WriteInt(typeResults.GetIndex(type.Item1));
		}
	}
}
