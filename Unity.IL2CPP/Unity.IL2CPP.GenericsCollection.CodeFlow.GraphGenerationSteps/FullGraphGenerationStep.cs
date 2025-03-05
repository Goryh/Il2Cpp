using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class FullGraphGenerationStep
{
	public static void Run(ref GraphGenerationContext context)
	{
		foreach (AssemblyDefinition assembly in context.Assemblies)
		{
			foreach (TypeDefinition type in assembly.GetAllTypes())
			{
				GenerateCodeFlowGraph(ref context, type);
				foreach (MethodDefinition method in type.Methods)
				{
					GenerateCodeFlowGraph(ref context, method);
				}
			}
		}
	}

	private static void GenerateCodeFlowGraph(ref GraphGenerationContext context, TypeDefinition type)
	{
		int typeIndex = context.TypeDependencies.Count;
		int methodIndex = context.MethodDependencies.Count;
		int referrerIndex = context.TypeNodes.Count | int.MinValue;
		GenerateFromImplicitDependencies(ref context, type, referrerIndex);
		GenerateFromBaseType(ref context, type, referrerIndex);
		GenerateFromMethods(ref context, type, referrerIndex);
		StagingNode<TypeDefinition> typeNode = new StagingNode<TypeDefinition>(type, methodIndex, context.MethodDependencies.Count, typeIndex, context.TypeDependencies.Count);
		context.TypeNodes.Add(typeNode);
	}

	private static void GenerateFromImplicitDependencies(ref GraphGenerationContext context, IMemberDefinition definition, int referrerIndex)
	{
		if (!context.ImplicitDependencies.TryGetValue(definition, out var dependencies))
		{
			return;
		}
		foreach (GenericInstanceType dependency in dependencies)
		{
			TypeReferenceDefinitionPair pair = new TypeReferenceDefinitionPair(dependency.Resolve(), dependency, TypeDependencyKind.ImplicitDependency);
			context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(pair, referrerIndex));
		}
	}

	private static void GenerateFromBaseType(ref GraphGenerationContext context, TypeDefinition type, int referrerIndex)
	{
		if (type.BaseType is GenericInstanceType genericBaseType)
		{
			TypeReferenceDefinitionPair pair = new TypeReferenceDefinitionPair(genericBaseType.Resolve(), genericBaseType, TypeDependencyKind.BaseTypeOrInterface);
			context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(pair, referrerIndex));
		}
	}

	private static void GenerateFromMethods(ref GraphGenerationContext context, TypeDefinition type, int referrerIndex)
	{
		foreach (MethodDefinition method in type.Methods)
		{
			if (!method.HasGenericParameters && method.HasThis)
			{
				AddDependency(ref context, method, referrerIndex);
			}
		}
	}

	private static void GenerateCodeFlowGraph(ref GraphGenerationContext context, MethodDefinition method)
	{
		int typeIndex = context.TypeDependencies.Count;
		int methodIndex = context.MethodDependencies.Count;
		int referrerIndex = context.MethodNodes.Count;
		GenerateFromImplicitDependencies(ref context, method, referrerIndex);
		GenerateFromMethodSignature(ref context, method, referrerIndex);
		GenerateFromMethodBody(ref context, method, referrerIndex);
		StagingNode<MethodDefinition> methodNode = new StagingNode<MethodDefinition>(method, methodIndex, context.MethodDependencies.Count, typeIndex, context.TypeDependencies.Count);
		context.MethodNodes.Add(methodNode);
	}

	private static void GenerateFromMethodSignature(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
	{
		if (method.ReturnType is GenericInstanceType genericReturnType)
		{
			AddDependency(ref context, genericReturnType, referrerIndex, TypeDependencyKind.MethodParameterOrReturnType);
		}
		foreach (ParameterDefinition parameter in method.Parameters)
		{
			if (parameter.ParameterType is GenericInstanceType genericParameter)
			{
				AddDependency(ref context, genericParameter, referrerIndex, TypeDependencyKind.MethodParameterOrReturnType);
			}
		}
	}

	private static void GenerateFromMethodBody(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
	{
		if (!method.HasBody)
		{
			return;
		}
		foreach (Instruction instruction in method.Body.Instructions)
		{
			switch (instruction.OpCode.Code)
			{
			case Code.Call:
			case Code.Callvirt:
			case Code.Ldftn:
			case Code.Ldvirtftn:
			{
				MethodReference callee = (MethodReference)instruction.Operand;
				if (callee.DeclaringType.IsGenericInstance || callee.IsGenericInstance)
				{
					AddDependency(ref context, callee, referrerIndex);
				}
				break;
			}
			case Code.Box:
				if (instruction.Operand is GenericInstanceType type2)
				{
					AddDependency(ref context, type2, referrerIndex, TypeDependencyKind.InstantiatedGenericInstance);
				}
				break;
			case Code.Newobj:
				if (((MethodReference)instruction.Operand).DeclaringType is GenericInstanceType type)
				{
					TypeDefinition typeDefinition = type.Resolve();
					if (!typeDefinition.IsValueType)
					{
						AddDependency(ref context, typeDefinition, type, referrerIndex, TypeDependencyKind.InstantiatedGenericInstance);
					}
				}
				break;
			case Code.Newarr:
				if (context.ArraysAreOfInterest)
				{
					TypeReference elementType = (TypeReference)instruction.Operand;
					ArrayType arrayType = context.TypeFactory.CreateArrayType(elementType);
					AddDependency(ref context, null, arrayType, referrerIndex, TypeDependencyKind.InstantiatedArray);
				}
				break;
			}
		}
	}

	private static void AddDependency(ref GraphGenerationContext context, TypeReference typeReference, int referrerIndex, TypeDependencyKind kind)
	{
		AddDependency(ref context, typeReference.Resolve(), typeReference, referrerIndex, kind);
	}

	private static void AddDependency(ref GraphGenerationContext context, TypeDefinition typeDefinition, TypeReference typeReference, int referrerIndex, TypeDependencyKind kind)
	{
		TypeReferenceDefinitionPair pair = new TypeReferenceDefinitionPair(typeDefinition, typeReference, kind);
		context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(pair, referrerIndex));
	}

	private static void AddDependency(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
	{
		context.MethodDependencies.Add(new StagingDependency<MethodReferenceDefinitionPair>(new MethodReferenceDefinitionPair(method, method), referrerIndex));
	}

	private static void AddDependency(ref GraphGenerationContext context, MethodReference method, int referrerIndex)
	{
		context.MethodDependencies.Add(new StagingDependency<MethodReferenceDefinitionPair>(new MethodReferenceDefinitionPair(method.Resolve(), method), referrerIndex));
	}
}
