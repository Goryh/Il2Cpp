using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class ComInstanceMethodBodyWriter : ComMethodBodyWriter
{
	public ComInstanceMethodBodyWriter(ReadOnlyContext context, MethodReference method)
		: base(context, method, GetInterfaceMethod(context, method))
	{
	}

	private static MethodReference GetInterfaceMethod(ReadOnlyContext context, MethodReference method)
	{
		TypeReference declaringType = method.DeclaringType;
		if (declaringType.IsInterface)
		{
			return method;
		}
		TypeReference[] staticInterfaces = declaringType.GetAllFactoryTypes(context).ToArray();
		IEnumerable<TypeReference> instanceInterfaces = from iface in declaringType.GetInterfaces(context)
			where !staticInterfaces.Any((TypeReference nonInstanceInterface) => iface == nonInstanceInterface)
			select iface;
		return method.GetOverriddenInterfaceMethod(context, instanceInterfaces) ?? throw new InvalidOperationException("Could not find overridden method for " + method.FullName);
	}
}
