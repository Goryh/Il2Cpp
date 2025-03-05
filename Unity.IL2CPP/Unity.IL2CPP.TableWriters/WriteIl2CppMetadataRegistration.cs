using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public class WriteIl2CppMetadataRegistration : BasicWriterBase
{
	private readonly ReadOnlyCollection<TableInfo> _metadataInitializers;

	public WriteIl2CppMetadataRegistration(ReadOnlyCollection<TableInfo> metadataInitializers)
	{
		_metadataInitializers = metadataInitializers;
	}

	protected override void WriteFile(SourceWritingContext context)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppMetadataRegistration.c");
		writer.AddCodeGenMetadataIncludes();
		foreach (TableInfo initializer in _metadataInitializers.Where((TableInfo i) => i.Count != 0))
		{
			string @extern = (initializer.ExternTable ? "extern " : string.Empty);
			ICppCodeStream cppCodeStream = writer;
			cppCodeStream.WriteLine($"{@extern}{initializer.Type} {initializer.Name}[];");
		}
		writer.WriteStructInitializer("const Il2CppMetadataRegistration", MetadataUtils.RegistrationTableName(context), _metadataInitializers.SelectMany((TableInfo table) => new string[2]
		{
			table.Count.ToString(CultureInfo.InvariantCulture),
			table.Name
		}), externStruct: true);
	}
}
