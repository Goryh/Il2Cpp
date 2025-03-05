using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.WindowsRuntime;

internal class InterfaceNativeToManagedAdapterGenerator
{
	public static Dictionary<TypeDefinition, TypeDefinition> Generate(MinimalContext context, EditContext editContext, IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> clrToWindowsRuntimeProjections, Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> adapterMethodBodyWriters)
	{
		Dictionary<TypeDefinition, TypeDefinition> result = new Dictionary<TypeDefinition, TypeDefinition>();
		Dictionary<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedInterfaces = new Dictionary<TypeDefinition, TypeDefinition>();
		CollectProjectedInterfaces(clrToWindowsRuntimeProjections, clrToWindowsRuntimeProjectedInterfaces);
		foreach (KeyValuePair<TypeDefinition, TypeDefinition> projection in clrToWindowsRuntimeProjectedInterfaces)
		{
			if (!adapterMethodBodyWriters.TryGetValue(projection.Key, out var writers))
			{
				writers = new Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>();
			}
			TypeDefinition adapterClass = CreateAdapterClass(context, editContext, projection.Key, projection.Value, writers);
			result.Add(projection.Key, adapterClass);
			context.Global.Collectors.Stats.RecordNativeToManagedInterfaceAdapter();
		}
		return result;
	}

	private static void CollectProjectedInterfaces(IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> clrToWindowsRuntimeProjections, Dictionary<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedInterfaces)
	{
		foreach (KeyValuePair<TypeDefinition, TypeDefinition> projection in clrToWindowsRuntimeProjections)
		{
			if (projection.Key.IsInterface)
			{
				clrToWindowsRuntimeProjectedInterfaces.Add(projection.Key, projection.Value);
			}
		}
		KeyValuePair<TypeDefinition, TypeDefinition>[] array = clrToWindowsRuntimeProjectedInterfaces.ToArray();
		foreach (KeyValuePair<TypeDefinition, TypeDefinition> projection2 in array)
		{
			CollectProjectedInterfacesRecursively(projection2.Key, clrToWindowsRuntimeProjectedInterfaces);
		}
	}

	private static void CollectProjectedInterfacesRecursively(TypeDefinition clrInterface, Dictionary<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedInterfaces)
	{
		foreach (InterfaceImplementation @interface in clrInterface.Interfaces)
		{
			TypeDefinition interfaceType = @interface.InterfaceType.Resolve();
			if (!clrToWindowsRuntimeProjectedInterfaces.ContainsKey(interfaceType))
			{
				if (interfaceType.Name != "IEnumerable")
				{
					clrToWindowsRuntimeProjectedInterfaces.Add(interfaceType, null);
				}
				CollectProjectedInterfacesRecursively(interfaceType, clrToWindowsRuntimeProjectedInterfaces);
			}
		}
	}

	private static TypeDefinition CreateAdapterClass(MinimalContext context, EditContext editContext, TypeDefinition clrInterface, TypeDefinition windowsRuntimeInterface, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter> adapterMethodBodyWriters)
	{
		TypeDefinition typeDefinition = editContext.BuildClass("System.Runtime.InteropServices.WindowsRuntime", context.Global.Services.Naming.ForWindowsRuntimeAdapterTypeName(windowsRuntimeInterface, clrInterface), TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit).CloneGenericParameters(clrInterface).Complete();
		TypeReference clrInterfaceInstance = ((!clrInterface.HasGenericParameters) ? ((TypeReference)clrInterface) : ((TypeReference)context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(clrInterface, typeDefinition.GenericParameters)));
		TypeResolver resolver = context.Global.Services.TypeFactory.ResolverFor(clrInterfaceInstance);
		IEnumerable<TypeReference> enumerable = new TypeReference[1] { clrInterfaceInstance }.Union(adapterMethodBodyWriters.Select((KeyValuePair<MethodDefinition, InterfaceAdapterMethodBodyWriter> p) => resolver.Resolve(p.Key.DeclaringType))).Distinct();
		List<MethodDefinition> allGeneratedMethods = new List<MethodDefinition>();
		foreach (TypeReference iface in enumerable)
		{
			editContext.AddInterfaceImplementationToType(typeDefinition, iface, out var generatedMethods);
			allGeneratedMethods.AddRange(generatedMethods);
		}
		foreach (MethodDefinition method in allGeneratedMethods)
		{
			MethodReference overriddenMethod = method.Overrides[0];
			editContext.ChangeAttributes(method, method.Attributes & ~MethodAttributes.Abstract);
			if (adapterMethodBodyWriters.TryGetValue(overriddenMethod.Resolve(), out var methodBodyWriter))
			{
				methodBodyWriter(method);
			}
			else
			{
				WriteThrowNotSupportedException(context, method.Body.GetILProcessor());
			}
			method.Body.OptimizeMacros();
		}
		return typeDefinition;
	}

	private static void WriteThrowNotSupportedException(MinimalContext context, ILProcessor ilProcessor)
	{
		MethodDefinition exceptionConstructor = context.Global.Services.TypeProvider.GetSystemType(SystemType.NotSupportedException).Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		string exceptionMessage = "Cannot call method '" + ilProcessor.Body.Method.FullName + "'. IL2CPP does not yet support calling this projected method.";
		ilProcessor.Emit(OpCodes.Ldstr, exceptionMessage);
		ilProcessor.Emit(OpCodes.Newobj, exceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
	}
}
