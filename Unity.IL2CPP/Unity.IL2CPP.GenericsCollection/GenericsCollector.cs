using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Visitor;

namespace Unity.IL2CPP.GenericsCollection;

public static class GenericsCollector
{
	public static SimpleGenericsCollector Collect(PrimaryCollectionContext context, TypeDefinition type)
	{
		SimpleGenericsCollector simple = new SimpleGenericsCollector();
		GenericContextFreeVisitor visitor = new GenericContextFreeVisitor(context, simple);
		type.Accept(visitor);
		IterateToCompletion(context, simple, simple.Types, simple.Methods);
		return simple;
	}

	public static void IterateToCompletion(PrimaryCollectionContext context, SimpleGenericsCollector generics, IEnumerable<GenericInstanceType> typesToProcess, IEnumerable<GenericInstanceMethod> methodsToProcess)
	{
		List<GenericInstanceType> types = typesToProcess.ToList();
		List<GenericInstanceMethod> methods = methodsToProcess.ToList();
		while (types.Count > 0 || methods.Count > 0)
		{
			SimpleGenericsCollector newItems = IterateOnce(context, generics, types, methods);
			generics.Merge(newItems);
			types.Clear();
			types.AddRange(newItems.Types);
			methods.Clear();
			methods.AddRange(newItems.Methods);
		}
	}

	public static SimpleGenericsCollector IterateOnce(PrimaryCollectionContext context, IImmutableGenericsCollection generics, IEnumerable<GenericInstanceType> typesToProcess, IEnumerable<GenericInstanceMethod> methodsToProcess)
	{
		SimpleGenericsCollector newItems = new SimpleGenericsCollector();
		ChangeTrackingGenericsCollector trackingCollection = new ChangeTrackingGenericsCollector(generics, newItems);
		foreach (GenericInstanceType type in typesToProcess)
		{
			trackingCollection.AddType(type);
			type.Resolve().Accept(new GenericContextAwareVisitor(context, trackingCollection, new GenericContext(type, null)));
		}
		foreach (GenericInstanceMethod method in methodsToProcess)
		{
			trackingCollection.AddMethod(method);
			method.Resolve().Accept(new GenericContextAwareVisitor(context, trackingCollection, new GenericContext(method.DeclaringType as GenericInstanceType, method)));
		}
		return newItems;
	}

	internal static ReadOnlyCollection<GenericInstanceMethod> CollectGenericVirtualMethods(PrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assembliesOrderedByDependency, IImmutableGenericsCollection genericsCollection)
	{
		GenericVirtualMethodCollector genericVirtualMethodCollector = new GenericVirtualMethodCollector();
		TypeDefinition[] allTypeDefinitions = assembliesOrderedByDependency.SelectMany((AssemblyDefinition a) => a.GetAllTypes()).ToArray();
		return genericVirtualMethodCollector.Collect(context, allTypeDefinitions, context.Global.Services.VTable, genericsCollection);
	}

	internal static SimpleGenericsCollector CollectExtraTypes(PrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, IImmutableGenericsCollection results, out ReadOnlyHashSet<TypeReference> extraTypes)
	{
		extraTypes = AddExtraTypes(context, assemblies);
		return VisitExtraTypes(results, context, extraTypes);
	}

	private static SimpleGenericsCollector VisitExtraTypes(IImmutableGenericsCollection results, PrimaryCollectionContext collectionContext, IEnumerable<TypeReference> extraTypes)
	{
		SimpleGenericsCollector delta = new SimpleGenericsCollector();
		GenericContextFreeVisitor visitor = new GenericContextFreeVisitor(collectionContext, new ChangeTrackingGenericsCollector(results, delta));
		foreach (TypeReference extraType in extraTypes)
		{
			if (!(extraType is GenericInstanceType genericInstanceType))
			{
				if (!(extraType is PointerType pointerType))
				{
					if (extraType is ArrayType arrayType)
					{
						arrayType.Accept(visitor);
					}
				}
				else
				{
					pointerType.Accept(visitor);
				}
			}
			else
			{
				genericInstanceType.Accept(visitor);
			}
		}
		return delta;
	}

	private static ReadOnlyHashSet<TypeReference> AddExtraTypes(PrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		HashSet<TypeReference> extraTypes = new HashSet<TypeReference>();
		ExtraTypesSupport extraTypesSupport = new ExtraTypesSupport(assemblies, context.Global.Services.TypeFactory);
		foreach (string typeName in ExtraTypesSupport.BuildExtraTypesList(context.Global.InputData.ExtraTypesFiles))
		{
			TypeNameParseInfo typeNameInfo = TypeNameParser.Parse(typeName);
			if (typeNameInfo == null)
			{
				ConsoleOutput.Info.WriteLine("WARNING: Cannot parse type name {0} from the extra types list. Skipping.", typeName);
				continue;
			}
			if (!extraTypesSupport.TryAddType(typeNameInfo, out var type))
			{
				ConsoleOutput.Info.WriteLine("WARNING: Cannot add extra type {0}. Skipping.", typeName);
			}
			extraTypes.Add(type);
		}
		return extraTypes.AsReadOnly();
	}
}
