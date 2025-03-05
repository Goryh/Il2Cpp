using System.IO;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;

public class WriteLineMapping : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Line Mapping";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EmitSourceMapping;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		EmitLineMappingFile(context.AsReadOnly(), context.Results.PrimaryWrite.Symbols, context.InputData.SymbolsFolder);
	}

	public static void EmitLineMappingFile(GlobalReadOnlyContext context, ISymbolsCollectorResults symbolsResults, NPath outputPath)
	{
		NPath filePath = context.Services.PathFactory.GetFilePath(FileCategory.Other, outputPath.Combine("LineNumberMappings.json"));
		Directory.CreateDirectory(outputPath.ToString());
		using (StreamWriter lineMappingsFileWriter = new StreamWriter(filePath.ToString()))
		{
			symbolsResults.SerializeToJson(lineMappingsFileWriter);
		}
		File.WriteAllText(context.Services.PathFactory.GetFilePath(FileCategory.Other, outputPath.Combine("il2cppFileRoot.txt")), outputPath.Parent);
	}
}
