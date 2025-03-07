using System;
using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{Type}")]
public class CustomAttributeArgument
{
	internal readonly Mono.Cecil.CustomAttributeArgument Definition;

	private TypeReference _type;

	public object Value { get; private set; }

	public TypeReference Type
	{
		get
		{
			if (_type == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _type;
		}
	}

	public CustomAttributeArgument(Mono.Cecil.CustomAttributeArgument definition, object value)
	{
		Definition = definition;
		Value = value;
	}

	internal void InitializeType(TypeReference type)
	{
		_type = type;
	}

	internal void TranslateValue(TypeReference value)
	{
		if (!(Value is Mono.Cecil.TypeReference))
		{
			throw new InvalidOperationException();
		}
		Value = value;
	}

	internal void TranslateValue(CustomAttributeArgument value)
	{
		if (!(Value is Mono.Cecil.CustomAttributeArgument))
		{
			throw new InvalidOperationException();
		}
		Value = value;
	}

	internal void TranslateValue(CustomAttributeArgument[] value)
	{
		if (!(Value is Mono.Cecil.CustomAttributeArgument[]))
		{
			throw new InvalidOperationException();
		}
		Value = value;
	}
}
