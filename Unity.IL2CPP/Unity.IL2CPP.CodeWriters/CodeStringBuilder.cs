using System;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public class CodeStringBuilder : BaseCodeWriter, ICodeBuilder
{
	private readonly StringBuilder _stringBuilder;

	public override bool Empty => _stringBuilder.Length == 0;

	public CodeStringBuilder(ReadOnlyContext context, StringBuilder builder)
		: base(context)
	{
		_stringBuilder = builder;
	}

	protected override void AppendFormattedUnindented<T>(T value)
	{
		new StringBuilder.AppendInterpolatedStringHandler(0, 0, _stringBuilder).AppendFormatted(value);
	}

	protected override void AppendFormatted<T>(T value)
	{
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 0, _stringBuilder);
		if (_interpolationIndentNeeded)
		{
			WriteIndented(string.Empty);
			_interpolationIndentNeeded = false;
		}
		handler.AppendFormatted(value);
	}

	protected override void DirectAppendFormatted<T>(T value)
	{
		new StringBuilder.AppendInterpolatedStringHandler(0, 0, _stringBuilder).AppendFormatted(value);
	}

	protected override void WriteInternal(ReadOnlySpan<char> s)
	{
		_stringBuilder.Append(s);
	}

	public override void Flush()
	{
	}

	protected override void WriteInternal(char s)
	{
		_stringBuilder.Append(s);
	}

	public override string ToString()
	{
		throw new NotSupportedException("For now I'm not sure if we want to allow this.  It could lead to CppDeclarations being lost.  Use ToCodeStringValue or ToGeneratedCodeStringValue instead");
	}

	public virtual string ToCodeStringValue()
	{
		return GetStringBuilderValue();
	}

	protected string GetStringBuilderValue()
	{
		return _stringBuilder.ToString();
	}
}
