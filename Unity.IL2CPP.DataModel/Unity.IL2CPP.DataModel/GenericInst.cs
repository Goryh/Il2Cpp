using System;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public class GenericInst
{
	private readonly ReadOnlyCollection<TypeReference> _arguments;

	public ReadOnlyCollection<TypeReference> Arguments => _arguments;

	public TypeReference this[int index] => _arguments[index];

	public int Length => _arguments.Count;

	public int RecursiveGenericDepth { get; }

	internal GenericInst(ReadOnlyCollection<TypeReference> arguments)
	{
		if (arguments == null)
		{
			throw new ArgumentNullException("arguments");
		}
		_arguments = arguments;
		for (int i = 0; i < _arguments.Count; i++)
		{
			RecursiveGenericDepth = Math.Max(RecursiveGenericDepth, MaximumDepthFor(_arguments[i]));
		}
	}

	private static int MaximumDepthFor(TypeReference genericArgument)
	{
		if (genericArgument is GenericInstanceType genericInstanceType)
		{
			return genericInstanceType.RecursiveGenericDepth + 1;
		}
		if (genericArgument is ArrayType arrayType)
		{
			return MaximumDepthFor(arrayType.ElementType) + 1;
		}
		return 1;
	}
}
