using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.StringLiterals;

namespace Unity.IL2CPP.Metadata.Dat.SectionWriters;

public class StringWriter : BaseManySectionsWriter
{
	public override string Name => "String Literals";

	protected override void WriteToStreams(SourceWritingContext context, SectionFactory factory)
	{
		MemoryStream stream = factory.CreateSection("String Literals");
		MemoryStream dataStream = factory.CreateSection("String Literal Data");
		ReadOnlyStringLiteralTable stringLiteralTable = context.Global.Results.SecondaryCollection.StringLiteralTable;
		new StringLiteralWriter().Write(context, stream, dataStream, stringLiteralTable);
		dataStream.AlignTo(4);
	}
}
