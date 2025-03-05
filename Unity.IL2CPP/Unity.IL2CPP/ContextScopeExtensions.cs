using System;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP;

internal static class ContextScopeExtensions
{
	public static string ForCurrentCodeGenModuleVar(this IContextScopeService scopeService)
	{
		string uniqueId = scopeService.UniqueIdentifier;
		if (uniqueId != null)
		{
			return "g_" + uniqueId + "_CodeGenModule";
		}
		return null;
	}

	public static string ForMetadataGlobalVar(this IContextScopeService scopeService, string name)
	{
		string uniqueId = scopeService.UniqueIdentifier;
		if (uniqueId != null)
		{
			return name + "_" + uniqueId;
		}
		return name;
	}

	public static string ForReloadMethodMetadataInitialized(this IContextScopeService scopeService)
	{
		if (scopeService.UniqueIdentifier != null)
		{
			throw new NotSupportedException("Assembly reloading is not supported in per-assembly mode");
		}
		return scopeService.ForMetadataGlobalVar("g_MethodMetadataInitialized");
	}
}
