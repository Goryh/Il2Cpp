using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection;

public class ChangeTrackingGenericsCollector : IGenericsCollector
{
	private readonly IImmutableGenericsCollection _immutableGenerics;

	private readonly IGenericsCollector _generics;

	public ChangeTrackingGenericsCollector(IImmutableGenericsCollection immutableGenerics, IGenericsCollector generics)
	{
		_immutableGenerics = immutableGenerics;
		_generics = generics;
	}

	public bool AddArray(ArrayType type)
	{
		if (!_immutableGenerics.Arrays.Contains(type))
		{
			return _generics.AddArray(type);
		}
		return false;
	}

	public bool AddTypeDeclaration(GenericInstanceType type)
	{
		if (!_immutableGenerics.TypeDeclarations.Contains(type))
		{
			return _generics.AddTypeDeclaration(type);
		}
		return false;
	}

	public bool AddType(GenericInstanceType type)
	{
		if (!_immutableGenerics.Types.Contains(type))
		{
			return _generics.AddType(type);
		}
		return false;
	}

	public bool AddMethod(GenericInstanceMethod method)
	{
		if (!_immutableGenerics.Methods.Contains(method))
		{
			return _generics.AddMethod(method);
		}
		return false;
	}
}
