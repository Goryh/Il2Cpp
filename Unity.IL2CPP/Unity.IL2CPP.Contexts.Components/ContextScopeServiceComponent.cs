using System;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Components;

public class ContextScopeServiceComponent : ReusedServiceComponentBase<IContextScopeService, ContextScopeServiceComponent>, IContextScopeService
{
	private readonly Func<TypeReference, bool> _includeTypeDefinition;

	public string UniqueIdentifier { get; }

	public ContextScopeServiceComponent()
		: this((TypeReference _) => true, null)
	{
	}

	public ContextScopeServiceComponent(Func<TypeReference, bool> includeTypeDefinition, string assemblyNameClean)
	{
		_includeTypeDefinition = includeTypeDefinition;
		UniqueIdentifier = assemblyNameClean;
	}

	public bool IncludeTypeDefinitionInContext(TypeReference type)
	{
		return _includeTypeDefinition(type);
	}

	protected override ContextScopeServiceComponent ThisAsFull()
	{
		return this;
	}

	protected override IContextScopeService ThisAsRead()
	{
		return this;
	}
}
