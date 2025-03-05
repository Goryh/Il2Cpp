using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.CppDeclarations;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public class ManagedSourceCodeWriter : InMemoryGeneratedMethodCodeWriter
{
	private readonly IDisposable _profilerSection;

	private readonly NPath _filename;

	private readonly IIndirectCallCollector _indirectCallCollector;

	private readonly IMetadataUsageCollectorWriterService _metadataUsage;

	public override NPath FileName => _filename;

	public ManagedSourceCodeWriter(SourceWritingContext context, NPath filename, IDisposable profilerSection)
		: base(context)
	{
		if (filename.HasExtension("h", "hh", "hpp"))
		{
			throw new InvalidOperationException("SourceCodeWriter can only be used to write source files");
		}
		context.Global.Collectors.Stats.RecordFileWritten(filename);
		_profilerSection = profilerSection;
		_filename = filename;
		_indirectCallCollector = context.Global.Collectors.IndirectCalls;
		_metadataUsage = context.Global.Collectors.MetadataUsage;
	}

	public override void Dispose()
	{
		try
		{
			if (!base.ErrorOccurred)
			{
				_indirectCallCollector.AddRange(base.Context, from x in base.Declarations.VirtualMethods
					where x.CallType != VirtualMethodCallType.InvokerCall && x.CallType != VirtualMethodCallType.ConstrainedInvokerCall
					select x into virtualMethodDeclarationData
					select (MethodReference)virtualMethodDeclarationData.Method, IndirectCallUsage.Virtual);
				foreach (KeyValuePair<string, MethodMetadataUsage> usage in base.MethodMetadataUsages)
				{
					_metadataUsage.Add(usage.Key, usage.Value);
				}
				using StreamWriter writer = new StreamWriter(File.Open(_filename.ToString(), FileMode.Create), Encoding.UTF8);
				CodeWriter codeWriter = new CodeWriter(base.Context, writer, owns: false);
				SourceCodeWriterUtils.WriteCommonIncludes(codeWriter, _filename);
				CppDeclarationsWriter.WriteWithInteropGuidCollection(base.Context, codeWriter, base.Declarations);
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
