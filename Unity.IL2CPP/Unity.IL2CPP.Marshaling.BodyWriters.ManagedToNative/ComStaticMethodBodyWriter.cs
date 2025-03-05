using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class ComStaticMethodBodyWriter : ComMethodBodyWriter
{
	public ComStaticMethodBodyWriter(ReadOnlyContext context, MethodReference actualMethod)
		: base(context, actualMethod, GetInterfaceMethod(context, actualMethod))
	{
	}

	private static MethodReference GetInterfaceMethod(ReadOnlyContext context, MethodReference method)
	{
		TypeDefinition declaringType = method.DeclaringType.Resolve();
		if (!declaringType.IsWindowsRuntime)
		{
			throw new InvalidOperationException("Calling static methods is not supported on COM classes!");
		}
		if (declaringType.HasGenericParameters)
		{
			throw new InvalidOperationException("Calling static methods is not supported on types with generic parameters!");
		}
		if (declaringType.IsInterface)
		{
			throw new InvalidOperationException("Calling static methods is not supported on interfaces!");
		}
		return method.GetOverriddenInterfaceMethod(context, declaringType.GetStaticFactoryTypes()) ?? throw new InvalidOperationException("Could not find overridden method for " + method.FullName);
	}
}
