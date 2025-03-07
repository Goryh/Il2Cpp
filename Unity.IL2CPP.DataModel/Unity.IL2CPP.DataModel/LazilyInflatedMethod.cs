using System.Diagnostics;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{InflatedMethod}")]
public struct LazilyInflatedMethod
{
	public readonly TypeReference DeclaringType;

	public readonly MethodDefinition Definition;

	private readonly TypeResolver _resolver;

	private MethodReference _inflatedMethod;

	public MethodReference InflatedMethod
	{
		get
		{
			if (_inflatedMethod == null)
			{
				_inflatedMethod = _resolver.Resolve(Definition);
			}
			return _inflatedMethod;
		}
	}

	public string Name => Definition.Name;

	public bool HasThis => Definition.HasThis;

	internal LazilyInflatedMethod(TypeReference declaringType, MethodDefinition definition, TypeResolver typeResolver)
	{
		DeclaringType = declaringType;
		Definition = definition;
		_resolver = typeResolver;
		_inflatedMethod = null;
	}

	internal LazilyInflatedMethod(MethodReference inflatedMethod)
	{
		DeclaringType = inflatedMethod.DeclaringType;
		Definition = inflatedMethod.Resolve();
		_inflatedMethod = inflatedMethod;
		_resolver = null;
	}
}
