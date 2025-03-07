using System;
using System.Collections.Generic;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.Awesome;

public class GenericParameterResolver
{
	public static TypeReference ResolveReturnTypeIfNeeded(ITypeFactory typeFactory, MethodReference methodReference)
	{
		if (methodReference.DeclaringType.IsArray && methodReference.Name == "Get")
		{
			return methodReference.ReturnType;
		}
		GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
		GenericInstanceType declaringGenericInstanceType = methodReference.DeclaringType as GenericInstanceType;
		if (genericInstanceMethod == null && declaringGenericInstanceType == null)
		{
			return methodReference.ReturnType;
		}
		return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, methodReference.ReturnType);
	}

	public static TypeReference ResolveThisTypeIfNeeded(ITypeFactory typeFactory, MethodReference methodReference)
	{
		TypeReference thisType = methodReference.DeclaringType;
		if (methodReference.DeclaringType.IsArray && (methodReference.Name == "Get" || methodReference.Name == "Set"))
		{
			return thisType;
		}
		GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
		GenericInstanceType declaringGenericInstanceType = methodReference.DeclaringType as GenericInstanceType;
		if ((genericInstanceMethod == null && declaringGenericInstanceType == null) || declaringGenericInstanceType == thisType)
		{
			return thisType;
		}
		return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, thisType);
	}

	public static TypeReference ResolveFieldTypeIfNeeded(ITypeFactory typeFactory, FieldReference fieldReference)
	{
		return ResolveIfNeeded(typeFactory, null, fieldReference.DeclaringType as GenericInstanceType, fieldReference.FieldType);
	}

	public static TypeReference ResolveParameterTypeIfNeeded(ITypeFactory typeFactory, MethodReference method, ParameterDefinition parameter)
	{
		GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
		GenericInstanceType declaringGenericInstanceType = method.DeclaringType as GenericInstanceType;
		if (genericInstanceMethod == null && declaringGenericInstanceType == null)
		{
			return parameter.ParameterType;
		}
		return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, parameter.ParameterType);
	}

	public static TypeReference ResolveVariableTypeIfNeeded(ITypeFactory typeFactory, MethodReference method, VariableDefinition variable)
	{
		GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
		GenericInstanceType declaringGenericInstanceType = method.DeclaringType as GenericInstanceType;
		if (genericInstanceMethod == null && declaringGenericInstanceType == null)
		{
			return variable.VariableType;
		}
		return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, variable.VariableType);
	}

	private static TypeReference ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance declaringGenericInstanceType, TypeReference parameterType)
	{
		if (genericInstanceMethod == null && declaringGenericInstanceType == null)
		{
			return parameterType;
		}
		if (parameterType is ByReferenceType byRefType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, byRefType);
		}
		if (parameterType is ArrayType arrayType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, arrayType);
		}
		if (parameterType is GenericInstanceType genericInstanceType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, genericInstanceType);
		}
		if (parameterType is GenericParameter genericParameter)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, genericParameter);
		}
		if (parameterType is PointerType pointerType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, pointerType);
		}
		if (parameterType is RequiredModifierType { ContainsGenericParameter: not false } requiredModifierType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, requiredModifierType);
		}
		if (parameterType is OptionalModifierType { ContainsGenericParameter: not false } optionalModifierType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, optionalModifierType.ElementType);
		}
		if (parameterType is PinnedType pinnedType)
		{
			return ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, pinnedType.ElementType);
		}
		if (parameterType is FunctionPointerType functionPointerType)
		{
			if (!parameterType.ContainsGenericParameter)
			{
				return parameterType;
			}
			TypeReference returnType = ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, functionPointerType.ReturnType);
			List<ParameterDefinition> parameters = new List<ParameterDefinition>(functionPointerType.Parameters.Count);
			foreach (ParameterDefinition parameter in functionPointerType.Parameters)
			{
				ParameterDefinition newParameter = new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.Index, parameter.CustomAttributes, parameter.MarshalInfo, parameter.HasConstant, parameter.Constant, MetadataToken.ParamZero);
				newParameter.InitializeParameterType(ResolveIfNeeded(typeFactory, genericInstanceMethod, declaringGenericInstanceType, parameter.ParameterType));
				parameters.Add(newParameter);
			}
			return typeFactory.CreateFunctionPointerType(returnType, parameters.AsReadOnly(), functionPointerType.CallingConvention, functionPointerType.HasThis, functionPointerType.ExplicitThis);
		}
		if (parameterType.ContainsGenericParameter)
		{
			throw new Exception("Unexpected generic parameter.");
		}
		return parameterType;
	}

	private static TypeReference ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, GenericParameter genericParameterElement)
	{
		if (genericParameterElement.MetadataType != MetadataType.MVar)
		{
			return genericInstanceType.GenericArguments[genericParameterElement.Position];
		}
		if (genericInstanceMethod == null)
		{
			return genericParameterElement;
		}
		return genericInstanceMethod.GenericArguments[genericParameterElement.Position];
	}

	private static ArrayType ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, ArrayType arrayType)
	{
		return typeFactory.CreateArrayType(ResolveIfNeeded(typeFactory, genericInstanceMethod, genericInstanceType, arrayType.ElementType), arrayType.Rank);
	}

	private static ByReferenceType ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, ByReferenceType byReferenceType)
	{
		return typeFactory.CreateByReferenceType(ResolveIfNeeded(typeFactory, genericInstanceMethod, genericInstanceType, byReferenceType.ElementType));
	}

	private static PointerType ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, PointerType pointerType)
	{
		return typeFactory.CreatePointerType(ResolveIfNeeded(typeFactory, genericInstanceMethod, genericInstanceType, pointerType.ElementType));
	}

	private static RequiredModifierType ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, RequiredModifierType requiredModifierType)
	{
		return typeFactory.CreateRequiredModifierType(requiredModifierType.ModifierType, ResolveIfNeeded(typeFactory, genericInstanceMethod, genericInstanceType, requiredModifierType.ElementType));
	}

	private static GenericInstanceType ResolveIfNeeded(ITypeFactory typeFactory, IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, GenericInstanceType genericInstanceType1)
	{
		if (!genericInstanceType1.ContainsGenericParameter)
		{
			return genericInstanceType1;
		}
		List<TypeReference> genericArguments = new List<TypeReference>(genericInstanceType1.GenericArguments.Count);
		foreach (TypeReference genericArgument in genericInstanceType1.GenericArguments)
		{
			if (!genericArgument.IsGenericParameter)
			{
				genericArguments.Add(ResolveIfNeeded(typeFactory, genericInstanceMethod, genericInstanceType, genericArgument));
				continue;
			}
			GenericParameter genParam = (GenericParameter)genericArgument;
			switch (genParam.Type)
			{
			case GenericParameterType.Type:
				if (genericInstanceType == null)
				{
					throw new NotSupportedException();
				}
				genericArguments.Add(genericInstanceType.GenericArguments[genParam.Position]);
				break;
			case GenericParameterType.Method:
				if (genericInstanceMethod == null)
				{
					genericArguments.Add(genParam);
				}
				else
				{
					genericArguments.Add(genericInstanceMethod.GenericArguments[genParam.Position]);
				}
				break;
			}
		}
		return typeFactory.CreateGenericInstanceType(genericInstanceType1.ElementType.Resolve(), genericInstanceType1.DeclaringType, genericArguments.ToArray());
	}
}
