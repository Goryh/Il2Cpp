using System;
using System.IO;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.CppDeclarations;

namespace Unity.IL2CPP.CodeWriters;

public class SourceCodeWriter : InMemoryCppCodeWriter
{
	private readonly IDisposable _profilerSection;

	private readonly NPath _filename;

	public SourceCodeWriter(MinimalContext context, NPath filename, IDisposable profilerSection)
		: base(context)
	{
		context.Global.Collectors.Stats.RecordFileWritten(filename);
		_profilerSection = profilerSection;
		_filename = filename;
	}

	public override void Dispose()
	{
		try
		{
			using (StreamWriter writer = new StreamWriter(File.Open(_filename.ToString(), FileMode.Create), Encoding.UTF8))
			{
				CodeWriter codeWriter = new CodeWriter(base.Context, writer, owns: false);
				SourceCodeWriterUtils.WriteCommonIncludes(codeWriter, _filename);
				CppDeclarationsWriter.Write(codeWriter, codeWriter, base.Declarations);
				base.Writer.Flush();
				base.Writer.BaseStream.Seek(0L, SeekOrigin.Begin);
				base.Writer.BaseStream.CopyTo(writer.BaseStream);
				writer.Flush();
			}
			base.Dispose();
		}
		finally
		{
			_profilerSection?.Dispose();
		}
	}
}
