using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.RuntimeStorage;

namespace Unity.IL2CPP.DataModel.BuildLogic.Populaters;

internal static class DefinitionPopulater
{
	public static void PopulateAssembly(TypeContext context, CecilSourcedAssemblyData assemblyData)
	{
		AssemblyDefinition assemblyDef = assemblyData.Assembly.Ours;
		if (assemblyData.Assembly.Source.EntryPoint == null)
		{
			assemblyDef.InitializeEntryPoint(null);
		}
		else
		{
			assemblyDef.InitializeEntryPoint((MethodDefinition)assemblyData.ResolveReference(assemblyData.Assembly.Source.EntryPoint));
		}
		PopulateCustomAttrProvider(assemblyData, assemblyDef);
		PopulateCustomAttrProvider(assemblyData, assemblyDef.MainModule);
		PopulateModuleDefinition(context, assemblyData, assemblyDef.MainModule, assemblyData.Assembly.Source.MainModule);
		assemblyDef.InitializeReferences(CollectAssemblyDependencies(context, assemblyDef, assemblyData.Assembly.Source).ToArray().AsReadOnly());
	}

	private static void PopulateModuleDefinition(TypeContext context, CecilSourcedAssemblyData assemblyData, ModuleDefinition moduleDefinition, Mono.Cecil.ModuleDefinition source)
	{
		Mono.Cecil.MethodDefinition moduleInitializer = source.GetType("<Module>")?.Methods.SingleOrDefault((Mono.Cecil.MethodDefinition m) => m.Name == ".cctor");
		if (moduleInitializer != null)
		{
			moduleDefinition.InitializeModuleInitializer(context.GetDef(moduleInitializer));
		}
		else
		{
			moduleDefinition.InitializeModuleInitializer(null);
		}
		moduleDefinition.InitializeExportedTypes((from e in source.ExportedTypes
			select e.Resolve() into e
			where e != null
			select e).Select(assemblyData.ResolveReference).ToArray().AsReadOnly());
		moduleDefinition.InitializeResources(source.HasResources ? source.Resources.Select((Mono.Cecil.Resource r) => r.ResourceType switch
		{
			Mono.Cecil.ResourceType.Embedded => new EmbeddedResource((Mono.Cecil.EmbeddedResource)r), 
			Mono.Cecil.ResourceType.Linked => new LinkedResource((Mono.Cecil.LinkedResource)r), 
			Mono.Cecil.ResourceType.AssemblyLinked => new AssemblyLinkedResource((Mono.Cecil.AssemblyLinkedResource)r), 
			_ => throw new NotSupportedException($"Unsupported resource type {r.ResourceType}"), 
		}).ToArray().AsReadOnly() : ReadOnlyCollectionCache<Resource>.Empty);
		moduleDefinition.InitializeAssemblyReferences(source.AssemblyReferences.Select((Mono.Cecil.AssemblyNameReference asm) => new AssemblyNameReference(asm)).ToArray().AsReadOnly());
	}

	public static IEnumerable<AssemblyDefinition> CollectAssemblyDependencies(TypeContext context, AssemblyDefinition assembly, Mono.Cecil.AssemblyDefinition source)
	{
		HashSet<AssemblyDefinition> result = new HashSet<AssemblyDefinition>();
		bool hasWindowsRuntimeReferences = false;
		foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences)
		{
			if (!reference.IsWindowsRuntime)
			{
				if (context.TryGetAssembly(reference, out var resolved))
				{
					result.Add(resolved);
				}
			}
			else
			{
				hasWindowsRuntimeReferences = true;
			}
		}
		if (hasWindowsRuntimeReferences)
		{
			foreach (AssemblyDefinition reference2 in ResolveWindowsRuntimeReferences(context, source))
			{
				result.Add(reference2);
			}
		}
		return result.ToArray();
	}

	private static IEnumerable<AssemblyDefinition> ResolveWindowsRuntimeReferences(TypeContext context, Mono.Cecil.AssemblyDefinition source)
	{
		HashSet<AssemblyDefinition> resolvedAssemblies = new HashSet<AssemblyDefinition>();
		foreach (Mono.Cecil.TypeReference typeReference in source.MainModule.GetTypeReferences())
		{
			if (typeReference.Module == source.MainModule && typeReference.Scope is Mono.Cecil.AssemblyNameReference)
			{
				Mono.Cecil.TypeDefinition typeDef = typeReference.Resolve();
				if (typeDef != null && context.TryGetAssembly(typeDef.Module.Assembly.Name.Name, out var resolvedAssembly))
				{
					resolvedAssemblies.Add(resolvedAssembly);
				}
			}
		}
		return resolvedAssemblies;
	}

	public static void PopulateTypeDef(TypeContext context, UnderConstructionMember<TypeDefinition, Mono.Cecil.TypeDefinition> type)
	{
		TypeDefinition typeDef = type.Ours;
		Mono.Cecil.TypeDefinition source = type.Source;
		CecilSourcedAssemblyData cecilData = type.CecilData;
		typeDef.InitializeBaseType(cecilData.ResolveReference(source.BaseType));
		PopulateFieldDefs(cecilData, typeDef);
		PopulatePropertyDefs(cecilData, typeDef);
		PopulateEventDefs(cecilData, typeDef);
		PopulateMethodDefs(cecilData, typeDef);
		PopulateInterfaceImpls(cecilData, typeDef);
		PopulateCustomAttrProvider(cecilData, typeDef);
		PopulateGenericParameters(cecilData, typeDef);
		RuntimeStorageKind runtimeStorage = TypeRuntimeStorage.GetTypeDefinitionRuntimeStorageKind(typeDef);
		typeDef.InitializeTypeDefProperties(typeDef.Methods.SingleOrDefault((MethodDefinition m) => m.IsStaticConstructor), IsGraftedArrayInterfaceType(typeDef), IsByRefLike(context, typeDef, runtimeStorage), runtimeStorage);
		ReferencePopulater.PopulateTypeRefProperties(typeDef);
	}

	internal static bool IsGraftedArrayInterfaceType(TypeDefinition type)
	{
		if (!type.IsInterface || type.Assembly != type.Context.SystemAssembly)
		{
			return false;
		}
		foreach (TypeDefinition graftedArrayInterfaceType in type.Context.GraftedArrayInterfaceTypes)
		{
			if (graftedArrayInterfaceType == type)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsByRefLike(TypeContext typeContext, TypeDefinition type, RuntimeStorageKind runtimeStorage)
	{
		if (runtimeStorage != RuntimeStorageKind.ValueType)
		{
			return false;
		}
		return type.CustomAttributes.Any((CustomAttribute a) => a.AttributeType == typeContext.GetSystemType(SystemType.IsByRefLikeAttribute));
	}

	private static void PopulateFieldDefs(CecilSourcedAssemblyData assemblyData, TypeDefinition typeDef)
	{
		foreach (FieldDefinition fieldDef in typeDef.Fields)
		{
			fieldDef.InitializeFieldType(assemblyData.ResolveReference(fieldDef.Definition.FieldType));
			PopulateCustomAttrProvider(assemblyData, fieldDef);
			PopulateMarshalInfoProvider(assemblyData, fieldDef, fieldDef.Definition);
		}
		typeDef.InitializeFieldDuplication();
	}

	private static void PopulatePropertyDefs(CecilSourcedAssemblyData assemblyData, TypeDefinition typeDef)
	{
		foreach (PropertyDefinition propertyDef in typeDef.Properties)
		{
			propertyDef.InitializePropertyType(assemblyData.ResolveReference(propertyDef.Definition.PropertyType));
			PopulateCustomAttrProvider(assemblyData, propertyDef);
			foreach (ParameterDefinition parameter in propertyDef.Parameters)
			{
				parameter.InitializeParameterType(assemblyData.ResolveReference(parameter.Definition.ParameterType));
				PopulateCustomAttrProvider(assemblyData, parameter);
			}
		}
	}

	private static void PopulateEventDefs(CecilSourcedAssemblyData assemblyData, TypeDefinition typeDef)
	{
		foreach (EventDefinition eventDef in typeDef.Events)
		{
			eventDef.InitializeEventType(assemblyData.ResolveReference(eventDef.Definition.EventType));
			PopulateCustomAttrProvider(assemblyData, eventDef);
		}
	}

	private static void PopulateInterfaceImpls(CecilSourcedAssemblyData assemblyData, TypeDefinition typeDef)
	{
		foreach (InterfaceImplementation interfaceImpl in typeDef.Interfaces)
		{
			interfaceImpl.InitializeInterfaceType(assemblyData.ResolveReference(interfaceImpl.Definition.InterfaceType));
			PopulateCustomAttrProvider(assemblyData, interfaceImpl);
		}
	}

	private static void PopulateMethodDefs(CecilSourcedAssemblyData assemblyData, TypeDefinition typeDef)
	{
		foreach (MethodDefinition methodDef in typeDef.Methods)
		{
			if (methodDef.Definition == null)
			{
				continue;
			}
			TypeReference returnType = assemblyData.ResolveReference(GenericParameterResolver.ResolveReturnTypeIfNeeded(methodDef.Definition));
			methodDef.InitializeReturnType(returnType);
			methodDef.MethodReturnType.InitializeReturnType(returnType);
			PopulateMethodDefinitionProperties(methodDef);
			foreach (ParameterDefinition parameter in methodDef.Parameters)
			{
				parameter.InitializeParameterType(assemblyData.ResolveReference(GenericParameterResolver.ResolveParameterTypeIfNeeded(methodDef.Definition, parameter.Definition)));
				PopulateCustomAttrProvider(assemblyData, parameter);
				PopulateMarshalInfoProvider(assemblyData, parameter, parameter.Definition);
			}
			PopulateCustomAttrProvider(assemblyData, methodDef);
			PopulateCustomAttrProvider(assemblyData, methodDef.MethodReturnType);
			PopulateMarshalInfoProvider(assemblyData, methodDef.MethodReturnType, methodDef.Definition.MethodReturnType);
			PopulateGenericParameters(assemblyData, methodDef);
			MethodBodyPopulator.PopulateMethodBody(assemblyData, methodDef, methodDef.Definition);
			PopulateMethodDebugInformation(methodDef, methodDef.Definition);
			if (methodDef.Definition.HasOverrides)
			{
				methodDef.InitializeOverrides(methodDef.Definition.Overrides.Select((Mono.Cecil.MethodReference m) => assemblyData.ResolveReference(ResolveMethodReference(m))).ToArray().AsReadOnly());
			}
			else
			{
				methodDef.InitializeOverrides(ReadOnlyCollectionCache<MethodReference>.Empty);
			}
		}
	}

	public static void PopulateMethodDefinitionProperties(MethodDefinition methodDef)
	{
		methodDef.InitializeMethodDefProperties(methodDef.Name.StartsWith("$__Stripped"), methodDef.IsRuntimeSpecialName && methodDef.IsSpecialName && (methodDef.Name == ".cctor" || methodDef.Name == ".ctor"));
	}

	private static Mono.Cecil.MethodReference ResolveMethodReference(Mono.Cecil.MethodReference methodReference)
	{
		if (methodReference.IsGenericInstance)
		{
			return methodReference;
		}
		if (methodReference.DeclaringType.IsGenericInstance)
		{
			return methodReference;
		}
		return methodReference.Resolve();
	}

	private static void PopulateCustomAttrProvider(CecilSourcedAssemblyData assemblyData, ICustomAttributeProvider customAttrProvider)
	{
		foreach (CustomAttribute customAttr in customAttrProvider.CustomAttributes)
		{
			customAttr.InitializeConstructor((MethodDefinition)assemblyData.ResolveReference(customAttr.Definition.Constructor));
			foreach (CustomAttributeArgument ctorArg in customAttr.ConstructorArguments)
			{
				PopulateCustomAttrArgument(assemblyData, ctorArg);
			}
			foreach (CustomAttributeNamedArgument fieldArgument in customAttr.Fields)
			{
				PopulateCustomAttrArgument(assemblyData, fieldArgument.Argument);
			}
			foreach (CustomAttributeNamedArgument propertyArgument in customAttr.Properties)
			{
				PopulateCustomAttrArgument(assemblyData, propertyArgument.Argument);
			}
		}
	}

	private static void PopulateCustomAttrArgument(CecilSourcedAssemblyData assemblyData, CustomAttributeArgument argument)
	{
		argument.InitializeType(assemblyData.ResolveReference(argument.Definition.Type));
		object value = argument.Value;
		if (!(value is Mono.Cecil.TypeReference t))
		{
			if (!(value is Mono.Cecil.CustomAttributeArgument innerArgument))
			{
				if (value is Mono.Cecil.CustomAttributeArgument[] innerArguments)
				{
					CustomAttributeArgument[] newArgs = new CustomAttributeArgument[innerArguments.Length];
					for (int i = 0; i < innerArguments.Length; i++)
					{
						newArgs[i] = TranslateCustomAttrValue(assemblyData, innerArguments[i]);
					}
					argument.TranslateValue(newArgs);
				}
			}
			else
			{
				argument.TranslateValue(TranslateCustomAttrValue(assemblyData, innerArgument));
			}
		}
		else
		{
			argument.TranslateValue(assemblyData.ResolveReference(t));
		}
	}

	private static CustomAttributeArgument TranslateCustomAttrValue(CecilSourcedAssemblyData assemblyData, Mono.Cecil.CustomAttributeArgument argument)
	{
		CustomAttributeArgument newArg = new CustomAttributeArgument(argument, argument.Value);
		PopulateCustomAttrArgument(assemblyData, newArg);
		return newArg;
	}

	private static void PopulateMarshalInfoProvider(CecilSourcedAssemblyData assemblyData, IMarshalInfoProvider ours, Mono.Cecil.IMarshalInfoProvider cecil)
	{
		if (ours.MarshalInfo is CustomMarshalInfo customMarshalInfo)
		{
			customMarshalInfo.InitializeManagedType(assemblyData.ResolveReference(((Mono.Cecil.CustomMarshalInfo)cecil.MarshalInfo).ManagedType));
		}
	}

	private static void PopulateGenericParameters(CecilSourcedAssemblyData assemblyData, IGenericParameterProvider genericParamProvider)
	{
		for (int i = 0; i < genericParamProvider.GenericParameters.Count; i++)
		{
			GenericParameter genericParam = genericParamProvider.GenericParameters[i];
			PopulateCustomAttrProvider(assemblyData, genericParam);
			ReferencePopulater.PopulateTypeRefProperties(genericParam);
			foreach (GenericParameterConstraint genericConstraint in genericParam.Constraints)
			{
				genericConstraint.InitializeConstraintType(assemblyData.ResolveReference(genericConstraint.Definition.ConstraintType));
				PopulateCustomAttrProvider(assemblyData, genericConstraint);
			}
		}
	}

	private static void PopulateMethodDebugInformation(MethodDefinition method, Mono.Cecil.MethodDefinition definition)
	{
		if (definition.DebugInformation.HasSequencePoints && definition.HasBody)
		{
			Collection<Mono.Cecil.Cil.SequencePoint> sourceSequencePoints = definition.DebugInformation.SequencePoints;
			SequencePoint[] sequencePoints = new SequencePoint[sourceSequencePoints.Count];
			Dictionary<int, SequencePoint> offsetMapping = new Dictionary<int, SequencePoint>(sourceSequencePoints.Count);
			for (int i = 0; i < sourceSequencePoints.Count; i++)
			{
				Mono.Cecil.Cil.SequencePoint sourceSequencePoint = sourceSequencePoints[i];
				SequencePoint ourSequencePoint = (sequencePoints[i] = new SequencePoint(sourceSequencePoint, new Document(sourceSequencePoint.Document.Url, sourceSequencePoint.Document.Hash)));
				if (!offsetMapping.ContainsKey(sourceSequencePoints[i].Offset))
				{
					offsetMapping.Add(sourceSequencePoints[i].Offset, ourSequencePoint);
				}
			}
			foreach (Instruction t in method.Body.Instructions)
			{
				if (offsetMapping.TryGetValue(t.Offset, out var ourSequencePoint2))
				{
					t.InitializeSequencePoint(ourSequencePoint2);
				}
			}
			method.DebugInformation.InitializeDebugInformation(sequencePoints.AsReadOnly());
		}
		else
		{
			method.DebugInformation.InitializeDebugInformation(Array.Empty<SequencePoint>().AsReadOnly());
		}
	}
}
