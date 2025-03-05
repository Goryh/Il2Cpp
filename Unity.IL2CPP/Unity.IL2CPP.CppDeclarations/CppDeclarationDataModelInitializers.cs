using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.InjectedInitialize;

namespace Unity.IL2CPP.CppDeclarations;

internal static class CppDeclarationDataModelInitializers
{
	public static CppDeclarationsData GetCppDeclarations(ReadOnlyContext context, ITypeReferenceInjectedInitialize type)
	{
		return (CppDeclarationsData)type.GetCppDeclarationsData(context, BuildCacheData);
	}

	public static int GetCppDeclarationsDepth(ReadOnlyContext context, ITypeReferenceInjectedInitialize type)
	{
		return type.GetCppDeclarationsDepth(context, GetDepth);
	}

	public static ReadOnlyCollection<CppDeclarationsData> GetCppDeclarationsDependencies(ReadOnlyContext context, ITypeReferenceInjectedInitialize type)
	{
		return (ReadOnlyCollection<CppDeclarationsData>)type.GetCppDeclarationsDependencies(context, GetDependencies);
	}

	private static object BuildCacheData(ReadOnlyContext context, TypeReference type)
	{
		TypeReference[] typesRequiringInteropGuids;
		CppDeclarationsInstance instanceDeclarations = BuildDeclarationsInstance(context, type, TypeDefinitionWriter.FieldType.Instance, out typesRequiringInteropGuids);
		TypeReference[] typesRequiringInteropGuids2;
		CppDeclarationsInstance staticDeclarations = BuildDeclarationsInstance(context, type, TypeDefinitionWriter.FieldType.Static, out typesRequiringInteropGuids2);
		CppDeclarationsInstance threadStaticDeclarations = BuildDeclarationsInstance(context, type, TypeDefinitionWriter.FieldType.ThreadStatic, out typesRequiringInteropGuids2);
		return new CppDeclarationsData(type, instanceDeclarations, staticDeclarations, threadStaticDeclarations, typesRequiringInteropGuids?.AsReadOnly());
	}

	private static CppDeclarationsInstance BuildDeclarationsInstance(ReadOnlyContext context, TypeReference type, TypeDefinitionWriter.FieldType fieldType, out TypeReference[] typesRequiringInteropGuids)
	{
		using InMemoryReadOnlyContextCodeWriter writer = new InMemoryReadOnlyContextCodeWriter(context);
		SourceWriter.WriteTypeDefinition(context, writer, type, fieldType, out typesRequiringInteropGuids);
		string definition = writer.GetSourceCodeString();
		if (string.IsNullOrEmpty(definition))
		{
			return CppDeclarationsInstance.Empty;
		}
		return new CppDeclarationsInstance(writer.Declarations, definition);
	}

	private static int GetDepth(ReadOnlyContext context, TypeReference type)
	{
		return GetDepthRecursive(context, type, new HashSet<TypeReference>());
	}

	private static int GetDepthRecursive(ReadOnlyContext context, TypeReference type, HashSet<TypeReference> visitedTypes)
	{
		if (visitedTypes.Contains(type))
		{
			return 0;
		}
		int depth = 0;
		try
		{
			visitedTypes.Add(type);
			foreach (TypeReference typeIncludes in type.GetCppDeclarations(context).Instance.Declarations.TypeIncludes)
			{
				depth = Math.Max(depth, 1 + GetDepthRecursive(context, typeIncludes, visitedTypes));
			}
			return depth;
		}
		finally
		{
			visitedTypes.Remove(type);
		}
	}

	private static ReadOnlyCollection<CppDeclarationsData> GetDependencies(ReadOnlyContext context, TypeReference type)
	{
		CppDeclarationsData root = type.GetCppDeclarations(context);
		HashSet<TypeReference> collectedItems = new HashSet<TypeReference>();
		int previousCount = -1;
		List<CppDeclarationsData> dependencies = new List<CppDeclarationsData>();
		List<CppDeclarationsData> itemsToProcess = new List<CppDeclarationsData>();
		ProcessDependencies(context, type, root.Instance.Declarations.TypeIncludes, collectedItems, itemsToProcess, dependencies);
		ProcessDependencies(context, type, root.Static.Declarations.TypeIncludes, collectedItems, itemsToProcess, dependencies);
		ProcessDependencies(context, type, root.ThreadStatic.Declarations.TypeIncludes, collectedItems, itemsToProcess, dependencies);
		while (previousCount != collectedItems.Count)
		{
			previousCount = collectedItems.Count;
			CppDeclarationsData[] array = itemsToProcess.ToArray();
			itemsToProcess.Clear();
			CppDeclarationsData[] array2 = array;
			foreach (CppDeclarationsData item in array2)
			{
				ProcessDependencies(context, type, item.Instance.Declarations.TypeIncludes, collectedItems, itemsToProcess, dependencies);
			}
		}
		return dependencies.AsReadOnly();
	}

	private static void ProcessDependencies(ReadOnlyContext context, TypeReference type, IEnumerable<TypeReference> typeIncludes, HashSet<TypeReference> collectedItems, List<CppDeclarationsData> itemsToProcess, List<CppDeclarationsData> dependencies)
	{
		foreach (TypeReference include in typeIncludes)
		{
			if (include != type && collectedItems.Add(include))
			{
				CppDeclarationsData data = include.GetCppDeclarations(context);
				itemsToProcess.Add(data);
				dependencies.Add(data);
			}
		}
	}
}
