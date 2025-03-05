using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public abstract class BaseCodeWriter : ICodeWriter, IDirectWriter, ICodeWriterInterpolatedStringHandlerCallbacks, IDirectWriterInterpolatedStringHandlerCallbacks
{
	private readonly ReadOnlyContext _context;

	private int _indent;

	private bool _shouldIndent;

	private string _indentString = string.Empty;

	protected bool _interpolationIndentNeeded = true;

	private static readonly ReadOnlyCollection<string> IndentStrings = new string[10] { "", "\t", "\t\t", "\t\t\t", "\t\t\t\t", "\t\t\t\t\t", "\t\t\t\t\t\t", "\t\t\t\t\t\t\t", "\t\t\t\t\t\t\t\t", "\t\t\t\t\t\t\t\t\t" }.AsReadOnly();

	public ReadOnlyContext Context => _context;

	public int IndentationLevel => _indent;

	public abstract bool Empty { get; }

	protected BaseCodeWriter(ReadOnlyContext context)
	{
		_context = context;
	}

	public void WriteLine()
	{
		WriteInternal('\n');
		_shouldIndent = true;
	}

	public void WriteLine(string block)
	{
		WriteIndented(block);
		WriteInternal('\n');
		_shouldIndent = true;
	}

	void IDirectWriterInterpolatedStringHandlerCallbacks.AppendFormatted<T>(T value)
	{
		DirectAppendFormatted(value);
	}

	protected abstract void DirectAppendFormatted<T>(T value);

	void IDirectWriterInterpolatedStringHandlerCallbacks.AppendLiteral(string value)
	{
		WriteInternal(value);
	}

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendFormattedUnindented(ReadOnlySpan<char> value)
	{
		WriteInternal(value);
	}

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendFormattedUnindented<T>(T value)
	{
		AppendFormattedUnindented(value);
	}

	protected abstract void AppendFormattedUnindented<T>(T value);

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendLiteralUnindented(string value)
	{
		WriteInternal(value);
	}

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendFormatted<T>(T value)
	{
		AppendFormatted(value);
	}

	protected abstract void AppendFormatted<T>(T value);

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendLiteral(string value)
	{
		AppendLiteral(value);
	}

	protected void AppendLiteral(string value)
	{
		if (_interpolationIndentNeeded)
		{
			WriteIndented(string.Empty);
			_interpolationIndentNeeded = false;
		}
		WriteInternal(value);
	}

	protected abstract void WriteInternal(ReadOnlySpan<char> s);

	void ICodeWriterInterpolatedStringHandlerCallbacks.AppendFormatted(ReadOnlySpan<char> value)
	{
		if (_interpolationIndentNeeded)
		{
			WriteIndented(string.Empty);
			_interpolationIndentNeeded = false;
		}
		WriteInternal(value);
	}

	void IDirectWriterInterpolatedStringHandlerCallbacks.AppendFormatted(ReadOnlySpan<char> value)
	{
		WriteInternal(value);
	}

	public void WriteLine(ref CodeWriterInterpolatedStringHandler handler)
	{
		WriteInternal('\n');
		_shouldIndent = true;
		_interpolationIndentNeeded = true;
	}

	public void WriteStatement(ref CodeWriterInterpolatedStringHandler handler)
	{
		WriteInternal(";\n");
		_shouldIndent = true;
		_interpolationIndentNeeded = true;
	}

	public void Write(ref CodeWriterInterpolatedStringHandler handler)
	{
		_interpolationIndentNeeded = true;
	}

	public void WriteCommentedLine(string block)
	{
		if (!ShouldEmitComments())
		{
			throw new InvalidOperationException(CommentErrorMessage());
		}
		WriteLine(Emit.Comment(block));
	}

	public void WriteComment(string block)
	{
		if (!ShouldEmitComments())
		{
			throw new InvalidOperationException(CommentErrorMessage());
		}
		WriteLine(Emit.Comment(block));
	}

	private static string CommentErrorMessage()
	{
		return "In order to prevent unnecessary string formatting this methods should not be called when EmitComments and EmitSourceMapping are false";
	}

	private bool ShouldEmitComments()
	{
		if (!_context.Global.Parameters.EmitComments)
		{
			return _context.Global.Parameters.EmitSourceMapping;
		}
		return true;
	}

	public void WriteStatement(string block)
	{
		WriteIndented(block);
		WriteInternal(";\n");
		_shouldIndent = true;
	}

	public void Write(string block)
	{
		WriteIndented(block);
	}

	public void Write(char block)
	{
		WriteIndented(block);
	}

	public void WriteUnindented(string block)
	{
		WriteInternal(block);
		WriteInternal('\n');
	}

	public void WriteUnindented(ref CodeWriterUnindentedInterpolatedStringHandler handler)
	{
		WriteInternal('\n');
	}

	public void Indent(int count = 1)
	{
		_indent += count;
		if (_indent < IndentStrings.Count)
		{
			_indentString = IndentStrings[_indent];
		}
		else
		{
			_indentString = new string('\t', _indent);
		}
		_shouldIndent = _indent > 0;
	}

	public void Dedent(int count = 1)
	{
		if (count > _indent)
		{
			throw new ArgumentException("Cannot dedent CppCodeWriter more than it was indented.", "count");
		}
		_indent -= count;
		if (_indent < IndentStrings.Count)
		{
			_indentString = IndentStrings[_indent];
		}
		else
		{
			_indentString = new string('\t', _indent);
		}
	}

	public void BeginBlock()
	{
		WriteLine("{");
		Indent();
	}

	public void BeginBlock(string comment)
	{
		Write('{');
		if (_context.Global.Parameters.EmitComments)
		{
			WriteCommentedLine(comment);
		}
		else
		{
			WriteLine();
		}
		Indent();
	}

	public void EndBlock(bool semicolon = false)
	{
		Dedent();
		if (semicolon)
		{
			WriteLine("};");
		}
		else
		{
			WriteLine("}");
		}
	}

	public void EndBlock(string comment, bool semicolon = false)
	{
		Dedent();
		Write('}');
		if (semicolon)
		{
			Write(';');
		}
		if (_context.Global.Parameters.EmitComments)
		{
			WriteCommentedLine(comment);
		}
		else
		{
			WriteLine();
		}
	}

	public void WriteAssignStatement(ref CodeWriterAssignInterpolatedStringHandler left, ref CodeWriterInterpolatedStringHandler right)
	{
		WriteInternal(";\n");
		_shouldIndent = true;
		_interpolationIndentNeeded = true;
	}

	public void WriteAssignStatement(ref CodeWriterAssignInterpolatedStringHandler left, string right)
	{
		WriteInternal(right);
		WriteInternal(";\n");
		_shouldIndent = true;
		_interpolationIndentNeeded = true;
	}

	public void WriteAssignStatement(string left, ref CodeWriterRightAssignInterpolatedStringHandler right)
	{
		WriteInternal(";\n");
		_shouldIndent = true;
		_interpolationIndentNeeded = true;
	}

	protected void WriteIndented(string s)
	{
		if (_shouldIndent)
		{
			WriteInternal(_indentString);
			_shouldIndent = false;
		}
		WriteInternal(s);
	}

	private void WriteIndented(char s)
	{
		if (_shouldIndent)
		{
			WriteInternal(_indentString);
			_shouldIndent = false;
		}
		WriteInternal(s);
	}

	public abstract void Flush();

	public void Write(ICodeBuilder other)
	{
		Write(other.ToCodeStringValue());
	}

	protected abstract void WriteInternal(char s);

	void IDirectWriter.WriteLine()
	{
		WriteInternal('\n');
	}

	void IDirectWriter.WriteLine(string text)
	{
		WriteInternal(text);
		WriteInternal('\n');
	}

	void IDirectWriter.Write(string text)
	{
		WriteInternal(text);
	}

	void IDirectWriter.Write(char text)
	{
		WriteInternal(text);
	}

	void IDirectWriter.Write(ref IDirectWriterInterpolatedStringHandler handler)
	{
	}

	void IDirectWriter.WriteLine(ref IDirectWriterInterpolatedStringHandler handler)
	{
		WriteInternal('\n');
	}
}
