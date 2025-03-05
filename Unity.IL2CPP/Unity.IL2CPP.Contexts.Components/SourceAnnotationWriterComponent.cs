using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components;

public class SourceAnnotationWriterComponent : StatefulComponentBase<ISourceAnnotationWriter, object, SourceAnnotationWriterComponent>, ISourceAnnotationWriter
{
	private class NotAvailable : ISourceAnnotationWriter
	{
		public void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint)
		{
			throw new NotSupportedException();
		}
	}

	private readonly Dictionary<NPath, string[]> _cachedFiles;

	public SourceAnnotationWriterComponent()
	{
		_cachedFiles = new Dictionary<NPath, string[]>();
	}

	private SourceAnnotationWriterComponent(Dictionary<NPath, string[]> existingData)
	{
		_cachedFiles = new Dictionary<NPath, string[]>(existingData);
	}

	public void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint)
	{
		if (!writer.Context.Global.Parameters.EmitSourceMapping && !writer.Context.Global.Parameters.EmitComments)
		{
			return;
		}
		string[] fileLines = GetFileLines(sequencePoint.Document.Url);
		if (fileLines == null)
		{
			return;
		}
		int startLine = sequencePoint.StartLine;
		int endLine = sequencePoint.EndLine;
		if (startLine < 1 || startLine > fileLines.Length)
		{
			return;
		}
		if (endLine == -1)
		{
			endLine = startLine;
		}
		startLine--;
		endLine--;
		if (fileLines.Length <= sequencePoint.EndLine)
		{
			return;
		}
		ArrayView<string> annotationLines = new ArrayView<string>(fileLines, startLine, endLine - startLine + 1);
		int minimumIndentation = int.MaxValue;
		for (int i = 0; i < annotationLines.Length; i++)
		{
			string line = annotationLines[i];
			if (!string.IsNullOrWhiteSpace(line))
			{
				int indentation;
				for (indentation = 0; indentation < line.Length && line[indentation] == ' '; indentation++)
				{
				}
				if (minimumIndentation > indentation)
				{
					minimumIndentation = indentation;
				}
			}
		}
		for (int j = 0; j < annotationLines.Length; j++)
		{
			string line2 = annotationLines[j];
			if (line2.Length >= minimumIndentation)
			{
				line2 = line2.Substring(minimumIndentation);
			}
			line2 = line2.TrimEnd();
			if (writer.Context.Global.Parameters.EmitSourceMapping)
			{
				writer.WriteLine($"//<source_info:{sequencePoint.Document.Url.ToString(SlashMode.Forward)}:{startLine + j + 1}>");
			}
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine(line2);
			}
		}
	}

	private string[] GetFileLines(NPath path)
	{
		string[] fileLines = null;
		if (_cachedFiles.TryGetValue(path, out fileLines))
		{
			return fileLines;
		}
		try
		{
			if (path.FileExists())
			{
				fileLines = File.ReadAllLines(path.ToString());
				int lineCount = fileLines.Length;
				for (int i = 0; i < lineCount; i++)
				{
					fileLines[i] = fileLines[i].Replace("\t", "    ").TrimEnd();
					if (fileLines[i].EndsWith("\\"))
					{
						fileLines[i] += ".";
					}
				}
			}
		}
		catch
		{
		}
		_cachedFiles.Add(path, fileLines);
		return fileLines;
	}

	protected override void DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendCollection(builder, "_cachedFiles", _cachedFiles.Keys.ToSortedCollectionBy((NPath p) => p.ToString()));
	}

	protected override void HandleMergeForAdd(SourceAnnotationWriterComponent forked)
	{
		foreach (KeyValuePair<NPath, string[]> item in forked._cachedFiles)
		{
			if (!_cachedFiles.ContainsKey(item.Key))
			{
				_cachedFiles.Add(item.Key, item.Value);
			}
		}
	}

	protected override void HandleMergeForMergeValues(SourceAnnotationWriterComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotSupportedException();
	}

	protected override void SyncPooledInstanceWithParent(SourceAnnotationWriterComponent parent)
	{
		throw new NotSupportedException();
	}

	protected override SourceAnnotationWriterComponent CreateEmptyInstance()
	{
		return new SourceAnnotationWriterComponent();
	}

	protected override SourceAnnotationWriterComponent CreateCopyInstance()
	{
		return new SourceAnnotationWriterComponent(_cachedFiles);
	}

	protected override SourceAnnotationWriterComponent CreatePooledInstance()
	{
		throw new NotSupportedException();
	}

	protected override SourceAnnotationWriterComponent ThisAsFull()
	{
		return this;
	}

	protected override object ThisAsRead()
	{
		return this;
	}

	protected override ISourceAnnotationWriter GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override object GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}
}
