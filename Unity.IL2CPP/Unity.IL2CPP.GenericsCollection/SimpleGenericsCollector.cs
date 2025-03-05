using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection;

public class SimpleGenericsCollector : IGenericsCollector, IImmutableGenericsCollection
{
	private readonly HashSet<GenericInstanceType> _types = new HashSet<GenericInstanceType>();

	private readonly HashSet<GenericInstanceType> _typeDeclarations = new HashSet<GenericInstanceType>();

	private readonly HashSet<GenericInstanceMethod> _methods = new HashSet<GenericInstanceMethod>();

	private readonly HashSet<ArrayType> _visitedArrays = new HashSet<ArrayType>();

	public ReadOnlyHashSet<GenericInstanceType> Types => _types.AsReadOnly();

	public ReadOnlyHashSet<GenericInstanceType> TypeDeclarations => _typeDeclarations.AsReadOnly();

	public ReadOnlyHashSet<GenericInstanceMethod> Methods => _methods.AsReadOnly();

	public ReadOnlyHashSet<ArrayType> Arrays => _visitedArrays.AsReadOnly();

	public void Merge(SimpleGenericsCollector other)
	{
		foreach (GenericInstanceType type in other._types)
		{
			AddType(type);
		}
		foreach (GenericInstanceType type2 in other._typeDeclarations)
		{
			AddTypeDeclaration(type2);
		}
		foreach (GenericInstanceMethod method in other._methods)
		{
			AddMethod(method);
		}
		foreach (ArrayType array in other._visitedArrays)
		{
			AddArray(array);
		}
	}

	public bool AddArray(ArrayType type)
	{
		return _visitedArrays.Add(type);
	}

	public bool AddTypeDeclaration(GenericInstanceType type)
	{
		return _typeDeclarations.Add(type);
	}

	public bool AddType(GenericInstanceType type)
	{
		return _types.Add(type);
	}

	public bool AddMethod(GenericInstanceMethod method)
	{
		return _methods.Add(method);
	}

	public IEnumerable<GenericInstanceType> GetTypes()
	{
		return _types;
	}

	public IEnumerable<GenericInstanceMethod> GetMethods()
	{
		return _methods;
	}
}
