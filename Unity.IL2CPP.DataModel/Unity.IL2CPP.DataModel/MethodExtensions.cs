namespace Unity.IL2CPP.DataModel;

public static class MethodExtensions
{
	public static LazilyInflatedMethod ToFakeLazilyInflatedMethod(this MethodReference method)
	{
		return new LazilyInflatedMethod(method);
	}

	public static bool IsSharedMethod(this MethodReference method, ITypeFactoryProvider provider)
	{
		return method.IsSharedMethod(provider.TypeFactory);
	}

	public static MethodReference GetSharedMethod(this MethodReference method, ITypeFactoryProvider provider)
	{
		return method.GetSharedMethod(provider.TypeFactory);
	}

	public static bool HasFullGenericSharingSignature(this MethodReference method, ITypeFactoryProvider provider)
	{
		return method.HasFullGenericSharingSignature(provider.TypeFactory);
	}

	public static bool CanShare(this MethodReference method, ITypeFactoryProvider provider)
	{
		return method.CanShare(provider.TypeFactory);
	}

	public static MethodReference GetSharedMethodIfSharableOtherwiseSelf(this MethodReference method, ITypeFactoryProvider provider)
	{
		if (method.CanShare(provider.TypeFactory))
		{
			return method.GetSharedMethod(provider.TypeFactory);
		}
		return method;
	}

	public static int CodeSize(this GenericInstanceMethod method)
	{
		MethodDefinition resolved = method.Resolve();
		if (resolved == null)
		{
			return 0;
		}
		if (!resolved.HasBody)
		{
			return 0;
		}
		return resolved.Body.CodeSize;
	}
}
