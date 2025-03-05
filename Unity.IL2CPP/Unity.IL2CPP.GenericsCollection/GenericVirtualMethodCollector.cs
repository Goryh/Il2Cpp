using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.GenericsCollection;

public class GenericVirtualMethodCollector
{
	public ReadOnlyCollection<GenericInstanceMethod> Collect(PrimaryCollectionContext context, IEnumerable<TypeDefinition> types, IVTableBuilderService vTableBuilder, IImmutableGenericsCollection genericsCollection)
	{
		ITinyProfilerService tiyProfiler = context.Global.Services.TinyProfiler;
		HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods = new HashSet<TypeDefinition>();
		HashSet<TypeDefinition> genericTypeDefinitionsImplementingGenericVirtualMethods = new HashSet<TypeDefinition>();
		HashSet<TypeReference> allTypesImplementingGenericVirtualMethods = new HashSet<TypeReference>();
		using (tiyProfiler.Section("CollectTypesWithGenericVirtualMethods"))
		{
			CollectTypesWithGenericVirtualMethods(types, typeDefinitionsWithGenericVirtualMethods, genericTypeDefinitionsImplementingGenericVirtualMethods, allTypesImplementingGenericVirtualMethods);
		}
		using (tiyProfiler.Section("CollectGenericTypesImplementingGenericVirtualMethods"))
		{
			CollectGenericTypesImplementingGenericVirtualMethods(genericTypeDefinitionsImplementingGenericVirtualMethods, allTypesImplementingGenericVirtualMethods, genericsCollection.Types);
		}
		Dictionary<TypeReference, HashSet<TypeReference>> baseToDerivedMap;
		using (tiyProfiler.Section("MapBaseTypesToAllDerivedTypes"))
		{
			baseToDerivedMap = MapBaseTypesToAllDerivedTypes(context, allTypesImplementingGenericVirtualMethods, typeDefinitionsWithGenericVirtualMethods);
		}
		IEnumerable<GenericInstanceMethod> genericVirtualMethods = genericsCollection.Methods.Where((GenericInstanceMethod m) => m.Resolve().IsVirtual);
		using (tiyProfiler.Section("FindOverridenMethodsInTypes"))
		{
			return FindOverridenMethodsInTypes(context, vTableBuilder, genericVirtualMethods, baseToDerivedMap, genericsCollection);
		}
	}

	private static void CollectTypesWithGenericVirtualMethods(IEnumerable<TypeDefinition> types, HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods, HashSet<TypeDefinition> genericTypesImplementingGenericVirtualMethods, HashSet<TypeReference> typesImplementingGenericVirtualMethods)
	{
		foreach (TypeDefinition type in types)
		{
			if (!type.HasMethods || !type.Methods.Any((MethodDefinition m) => m.IsVirtual && m.HasGenericParameters))
			{
				continue;
			}
			typeDefinitionsWithGenericVirtualMethods.Add(type);
			if (!type.IsInterface)
			{
				if (type.HasGenericParameters)
				{
					genericTypesImplementingGenericVirtualMethods.Add(type);
				}
				else
				{
					typesImplementingGenericVirtualMethods.Add(type);
				}
			}
		}
	}

	private static void CollectGenericTypesImplementingGenericVirtualMethods(HashSet<TypeDefinition> genericTypesImplementingGenericVirtualMethods, HashSet<TypeReference> typesImplementingGenericVirtualMethods, IEnumerable<GenericInstanceType> genericInstanceTypes)
	{
		foreach (GenericInstanceType type in genericInstanceTypes)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (genericTypesImplementingGenericVirtualMethods.Contains(typeDefinition))
			{
				typesImplementingGenericVirtualMethods.Add(type);
			}
		}
	}

	private static Dictionary<TypeReference, HashSet<TypeReference>> MapBaseTypesToAllDerivedTypes(MinimalContext context, HashSet<TypeReference> typesImplementingGenericVirtualMethods, HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods)
	{
		Dictionary<TypeReference, HashSet<TypeReference>> typeToImplementors = new Dictionary<TypeReference, HashSet<TypeReference>>();
		foreach (TypeReference type in typesImplementingGenericVirtualMethods)
		{
			for (TypeReference baseType = type; baseType != null; baseType = baseType.GetBaseType(context))
			{
				if (type != baseType && typeDefinitionsWithGenericVirtualMethods.Contains(baseType.Resolve()))
				{
					TypeReference typeToProcess = baseType;
					if (baseType is GenericInstanceType genericInstanceType)
					{
						typeToProcess = GenericSharingAnalysis.GetSharedType(context, genericInstanceType);
					}
					if (!typeToImplementors.TryGetValue(typeToProcess, out var set))
					{
						set = new HashSet<TypeReference>();
						typeToImplementors.Add(typeToProcess, set);
					}
					set.Add(type);
				}
				foreach (TypeReference itf in baseType.GetInterfaces(context))
				{
					if (typeDefinitionsWithGenericVirtualMethods.Contains(itf.Resolve()))
					{
						if (!typeToImplementors.TryGetValue(itf, out var set2))
						{
							set2 = new HashSet<TypeReference>();
							typeToImplementors.Add(itf, set2);
						}
						set2.Add(type);
					}
				}
			}
		}
		return typeToImplementors;
	}

	private static ReadOnlyCollection<GenericInstanceMethod> FindOverridenMethodsInTypes(PrimaryCollectionContext context, IVTableBuilderService vTableBuilder, IEnumerable<GenericInstanceMethod> genericVirtualMethods, Dictionary<TypeReference, HashSet<TypeReference>> typeToImplementors, IImmutableGenericsCollection genericsCollection)
	{
		List<GenericInstanceMethod> results = new List<GenericInstanceMethod>();
		foreach (GenericInstanceMethod genericMethod in genericVirtualMethods)
		{
			if (!typeToImplementors.TryGetValue(genericMethod.DeclaringType, out var implementors))
			{
				continue;
			}
			foreach (TypeReference type in implementors)
			{
				GenericInstanceMethod overridingMethod = FindMethodInTypeThatOverrides(context, genericMethod, type, vTableBuilder);
				if (overridingMethod != null && !genericsCollection.Methods.Contains(overridingMethod))
				{
					results.Add(overridingMethod);
				}
			}
		}
		return results.AsReadOnly();
	}

	private static GenericInstanceMethod FindMethodInTypeThatOverrides(ReadOnlyContext context, GenericInstanceMethod potentiallyOverridenGenericInstanceMethod, TypeReference typeThatMightHaveAnOverrideingMethod, IVTableBuilderService vTableBuilder)
	{
		MethodDefinition genericMethodDefinition = potentiallyOverridenGenericInstanceMethod.Resolve();
		VTable vtable = vTableBuilder.VTableFor(context, typeThatMightHaveAnOverrideingMethod);
		int index = vTableBuilder.IndexFor(context, genericMethodDefinition);
		if (genericMethodDefinition.DeclaringType.IsInterface)
		{
			index += vtable.InterfaceOffsets[potentiallyOverridenGenericInstanceMethod.DeclaringType];
		}
		MethodReference targetMethodDefinition = vtable.Slots[index].Method;
		if (targetMethodDefinition == null)
		{
			return null;
		}
		if (targetMethodDefinition.DeclaringType != typeThatMightHaveAnOverrideingMethod)
		{
			return null;
		}
		return Inflater.InflateMethod(context, new GenericContext(typeThatMightHaveAnOverrideingMethod as GenericInstanceType, potentiallyOverridenGenericInstanceMethod), targetMethodDefinition.Resolve());
	}
}
