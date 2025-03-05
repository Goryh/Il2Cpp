using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal static class GenericCodeFlowGraphCollector
{
	public static CodeFlowCollectionResults Collect(PrimaryCollectionContext context, IEnumerable<AssemblyDefinition> assemblies)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("GenericCodeFlowGraphCollector.Collect"))
		{
			InputData codeFlowGraphInputData;
			using (tinyProfiler.Section("GenericCodeFlowGraphCollector.GetTypesAndMethodsForAnalysis"))
			{
				codeFlowGraphInputData = CollectGenericCodeFlowGlobalInputData(context);
				MergeInputData(ref codeFlowGraphInputData, assemblies.Select((AssemblyDefinition assembly) => GetTypesAndMethodsForAnalysis(context, assembly)));
			}
			CodeFlowCollection generics = new CodeFlowCollection();
			if (codeFlowGraphInputData.DefinitionsOfInterest.Count > 0)
			{
				GenericCodeFlowGraphGenerator.Generate(context, ref codeFlowGraphInputData, assemblies).CollectGenerics(context, generics);
			}
			return generics.Complete();
		}
	}

	private static void MergeInputData(ref InputData allInputDatas, IEnumerable<InputData> inputDatas)
	{
		foreach (InputData inputData in inputDatas)
		{
			foreach (IMemberDefinition definition in inputData.DefinitionsOfInterest)
			{
				allInputDatas.DefinitionsOfInterest.Add(definition);
			}
			if (inputData.ImplicitDependencies != null)
			{
				MergeDictionaries(allInputDatas.ImplicitDependencies, inputData.ImplicitDependencies);
			}
		}
	}

	private static void MergeDictionaries<TKey, TValue>(Dictionary<TKey, List<TValue>> outDictionary, Dictionary<TKey, List<TValue>> inDictionary)
	{
		foreach (KeyValuePair<TKey, List<TValue>> pair in inDictionary)
		{
			if (outDictionary.TryGetValue(pair.Key, out var values))
			{
				values.AddRange(pair.Value);
			}
			else
			{
				outDictionary.Add(pair.Key, pair.Value);
			}
		}
	}

	private static InputData GetTypesAndMethodsForAnalysis(ReadOnlyContext context, AssemblyDefinition assembly)
	{
		HashSet<IMemberDefinition> definitionsOfInterest = new HashSet<IMemberDefinition>();
		Dictionary<IMemberDefinition, List<GenericInstanceType>> implicitDependencies = new Dictionary<IMemberDefinition, List<GenericInstanceType>>();
		HashSet<GenericInstanceType> interfacesHashSet = new HashSet<GenericInstanceType>();
		foreach (TypeDefinition type in assembly.GetAllTypes())
		{
			if (!type.HasGenericParameters)
			{
				if (type.IsWindowsRuntime)
				{
					interfacesHashSet.Clear();
					CollectAllImplementedGenericInterfaces(context, type, interfacesHashSet);
					if (interfacesHashSet.Count > 0)
					{
						implicitDependencies[type] = new List<GenericInstanceType>(interfacesHashSet);
					}
				}
			}
			else
			{
				if (type.IsInterface || type.IsAbstract)
				{
					continue;
				}
				if (type.IsDelegate && type.IsWindowsRuntime)
				{
					definitionsOfInterest.Add(type);
					continue;
				}
				TypeReference[] genericArguments = new TypeReference[type.GenericParameters.Count];
				for (int i = 0; i < type.GenericParameters.Count; i++)
				{
					genericArguments[i] = context.Global.Services.TypeProvider.SystemObject;
				}
				if (context.Global.Services.TypeFactory.CreateGenericInstanceType(type, type.DeclaringType, genericArguments).NeedsComCallableWrapper(context))
				{
					definitionsOfInterest.Add(type);
				}
			}
		}
		return new InputData(definitionsOfInterest, implicitDependencies, arraysAreOfInterest: false);
	}

	private static void CollectAllImplementedGenericInterfaces(ReadOnlyContext context, TypeReference type, HashSet<GenericInstanceType> results)
	{
		foreach (TypeReference @interface in type.GetInterfaces(context))
		{
			if (@interface is GenericInstanceType genericInterface)
			{
				results.Add(genericInterface);
				CollectAllImplementedGenericInterfaces(context, genericInterface, results);
			}
		}
	}

	private static InputData CollectGenericCodeFlowGlobalInputData(PrimaryCollectionContext context)
	{
		HashSet<IMemberDefinition> definitionsOfInterest = new HashSet<IMemberDefinition>();
		Dictionary<IMemberDefinition, List<GenericInstanceType>> implicitDependencies = new Dictionary<IMemberDefinition, List<GenericInstanceType>>();
		bool arraysAreOfInterest = false;
		ITypeProviderService typeProvider = context.Global.Services.TypeProvider;
		foreach (KeyValuePair<TypeDefinition, TypeDefinition> clrAndWindowsRuntimeInterfacePair in context.Global.Services.WindowsRuntime.GetClrToWindowsRuntimeProjectedTypes())
		{
			TypeDefinition clrType = clrAndWindowsRuntimeInterfacePair.Key;
			TypeDefinition windowsRuntimeType = clrAndWindowsRuntimeInterfacePair.Value;
			if (!clrType.HasGenericParameters || (!windowsRuntimeType.IsInterface && !windowsRuntimeType.IsDelegate))
			{
				continue;
			}
			List<GenericInstanceType> clrTypeDependencies = new List<GenericInstanceType>();
			List<GenericInstanceType> windowsRuntimeTypeDependencies = new List<GenericInstanceType>();
			if (windowsRuntimeType.Namespace == "Windows.Foundation.Collections")
			{
				if (windowsRuntimeType.Name == "IMapView`2" && typeProvider.ConstantSplittableMapType != null)
				{
					windowsRuntimeTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, windowsRuntimeType, typeProvider.ConstantSplittableMapType));
					definitionsOfInterest.Add(typeProvider.ConstantSplittableMapType);
				}
				if (windowsRuntimeType.Name == "IMap`2")
				{
					TypeDefinition readOnlyDictionary = typeProvider.GetSystemType(SystemType.ReadOnlyDictionary);
					if (readOnlyDictionary == null)
					{
						throw new InvalidProgramException("Windows.Foundation.Collections.IMap`2 was not stripped but System.Collections.ObjectModel.ReadOnlyDictionary`2 was. This indicates a bug in UnityLinker.");
					}
					windowsRuntimeTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, windowsRuntimeType, readOnlyDictionary));
					definitionsOfInterest.Add(readOnlyDictionary);
				}
				if (windowsRuntimeType.Name == "IVector`1")
				{
					TypeDefinition readOnlyCollection = typeProvider.GetSystemType(SystemType.ReadOnlyCollection);
					if (readOnlyCollection == null)
					{
						throw new InvalidProgramException("Windows.Foundation.Collections.IVector`1 was not stripped but System.Collections.ObjectModel.ReadOnlyCollection`1 was. This indicates a bug in UnityLinker.");
					}
					windowsRuntimeTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, windowsRuntimeType, readOnlyCollection));
					definitionsOfInterest.Add(readOnlyCollection);
					arraysAreOfInterest = true;
				}
				if (windowsRuntimeType.Name == "IVectorView`1")
				{
					arraysAreOfInterest = true;
				}
				if (windowsRuntimeType.Name == "IIterable`1")
				{
					TypeDefinition iiterator = typeProvider.GetSystemType(SystemType.IIterator);
					if (iiterator == null)
					{
						throw new InvalidProgramException("Windows.Foundation.Collections.IIterable`1 was not stripped but Windows.Foundation.Collections.IIterator`1 was. This indicates a bug in UnityLinker.");
					}
					windowsRuntimeTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, windowsRuntimeType, iiterator));
					arraysAreOfInterest = true;
				}
			}
			clrTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, clrType, windowsRuntimeType));
			windowsRuntimeTypeDependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, windowsRuntimeType, clrType));
			definitionsOfInterest.Add(windowsRuntimeType);
			implicitDependencies.Add(clrType, clrTypeDependencies);
			implicitDependencies.Add(windowsRuntimeType, windowsRuntimeTypeDependencies);
			if (!clrType.IsInterface)
			{
				continue;
			}
			foreach (MethodDefinition method in clrType.Methods)
			{
				definitionsOfInterest.Add(method);
			}
			foreach (MethodDefinition method2 in windowsRuntimeType.Methods)
			{
				definitionsOfInterest.Add(method2);
			}
		}
		foreach (KeyValuePair<TypeDefinition, TypeDefinition> adapterClassPair in context.Global.Services.WindowsRuntime.GetNativeToManagedInterfaceAdapterClasses())
		{
			if (!adapterClassPair.Key.HasGenericParameters)
			{
				continue;
			}
			foreach (MethodDefinition method3 in adapterClassPair.Key.Methods)
			{
				if (!implicitDependencies.TryGetValue(method3, out var dependencies))
				{
					implicitDependencies.Add(method3, dependencies = new List<GenericInstanceType>());
				}
				dependencies.Add(MakeGenericInstanceTypeWithGenericParameters(context, adapterClassPair.Key, adapterClassPair.Value));
				definitionsOfInterest.Add(adapterClassPair.Value);
			}
		}
		return new InputData(definitionsOfInterest, implicitDependencies, arraysAreOfInterest);
	}

	private static GenericInstanceType MakeGenericInstanceTypeWithGenericParameters(ReadOnlyContext context, IGenericParameterProvider genericParameterProvider, TypeDefinition type)
	{
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		TypeReference declaringType = type.DeclaringType;
		TypeReference[] genericArguments = genericParameterProvider.GenericParameters.ToArray();
		return typeFactory.CreateGenericInstanceType(type, declaringType, genericArguments);
	}
}
