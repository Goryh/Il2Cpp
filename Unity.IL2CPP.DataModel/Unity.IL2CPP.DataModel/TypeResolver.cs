using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class TypeResolver : ITypeResolver
{
	private readonly IGenericInstance _typeDefinitionContext;

	private readonly IGenericInstance _methodDefinitionContext;

	private readonly TypeContext _typeContext;

	private readonly ITypeFactory _typeFactory;

	private TypeResolver(TypeContext typeContext, ITypeFactory typeFactory)
	{
		_typeContext = typeContext;
		_typeFactory = typeFactory;
	}

	internal TypeResolver(GenericInstanceType typeDefinitionContext, IGenericInstance methodDefinitionContext, TypeContext typeContext, ITypeFactory typeFactory)
		: this(typeContext, typeFactory)
	{
		if (typeContext == null)
		{
			throw new ArgumentNullException("typeContext");
		}
		if (typeFactory == null)
		{
			throw new ArgumentNullException("typeFactory");
		}
		_typeDefinitionContext = typeDefinitionContext;
		_methodDefinitionContext = methodDefinitionContext;
	}

	public TypeResolver Nested(MethodReference method)
	{
		return new TypeResolver((GenericInstanceType)_typeDefinitionContext, method as GenericInstanceMethod, _typeContext, _typeFactory);
	}

	public MethodReference Resolve(MethodReference method)
	{
		return Resolve(method, _methodDefinitionContext != null);
	}

	public MethodReference Resolve(MethodReference method, bool resolveGenericParameters)
	{
		MethodReference methodReference = method;
		if (IsDummy())
		{
			return methodReference;
		}
		TypeReference declaringType = Resolve(method.DeclaringType);
		if (method is GenericInstanceMethod { GenericArguments: var genericArgs } genericInstanceMethod)
		{
			TypeReference[] resolved = new TypeReference[genericArgs.Count];
			for (int i = 0; i < genericArgs.Count; i++)
			{
				resolved[i] = Resolve(genericArgs[i]);
			}
			methodReference = _typeFactory.CreateGenericInstanceMethod(declaringType, genericInstanceMethod.MethodDef, resolved);
		}
		else
		{
			if (resolveGenericParameters && method.HasGenericParameters)
			{
				ReadOnlyCollection<GenericParameter> genericParams = method.GenericParameters;
				TypeReference[] resolved2 = new TypeReference[genericParams.Count];
				for (int j = 0; j < genericParams.Count; j++)
				{
					resolved2[j] = Resolve(genericParams[j]);
				}
				return _typeFactory.CreateGenericInstanceMethod(declaringType, method.Resolve(), resolved2);
			}
			if (declaringType is GenericInstanceType genericInstanceType)
			{
				methodReference = _typeFactory.CreateMethodReferenceOnGenericInstance(genericInstanceType, method.Resolve());
			}
			else if (method is SystemImplementedArrayMethod systemImplementedArrayMethod)
			{
				methodReference = _typeFactory.CreateSystemImplementedArrayMethod((ArrayType)declaringType, systemImplementedArrayMethod);
			}
		}
		return methodReference;
	}

	public FieldReference Resolve(FieldReference field)
	{
		TypeReference declaringType = Resolve(field.DeclaringType);
		if (declaringType == field.DeclaringType)
		{
			return field;
		}
		if (declaringType is GenericInstanceType declaringGenericInstance)
		{
			return _typeFactory.CreateFieldReference(declaringGenericInstance, field);
		}
		throw new InvalidOperationException("Attempted to Resolved a field reference " + field.Name + " on an unsupported type " + declaringType.FullName);
	}

	public TypeReference ResolveReturnType(IMethodSignature method)
	{
		return Resolve(method.ReturnType);
	}

	public TypeReference ResolveReturnType(MethodReference method)
	{
		return Resolve(method.GetResolvedReturnType(_typeFactory));
	}

	public TypeReference ResolveParameterType(MethodReference method, ParameterDefinition parameter)
	{
		return Resolve(method.GetResolvedParameters(_typeFactory)[parameter.Index].ParameterType);
	}

	public TypeReference ResolveVariableType(MethodReference method, VariableDefinition variable)
	{
		return Resolve(variable.VariableType);
	}

	public TypeReference ResolveFieldType(FieldReference field)
	{
		return Resolve(field.ResolvedFieldType(_typeFactory));
	}

	public TypeReference Resolve(TypeReference typeReference)
	{
		return Resolve(typeReference, resolveGenericParameters: true);
	}

	public TypeReference Resolve(TypeReference typeReference, bool resolveGenericParameters)
	{
		if (IsDummy())
		{
			return typeReference;
		}
		if (_typeDefinitionContext != null && _typeDefinitionContext.GenericArguments.Contains(typeReference))
		{
			return typeReference;
		}
		if (_methodDefinitionContext != null && _methodDefinitionContext.GenericArguments.Contains(typeReference))
		{
			return typeReference;
		}
		if (typeReference is GenericParameter genericParameter)
		{
			if (_typeDefinitionContext != null && _typeDefinitionContext.GenericArguments.Contains(genericParameter))
			{
				return genericParameter;
			}
			if (_methodDefinitionContext != null && _methodDefinitionContext.GenericArguments.Contains(genericParameter))
			{
				return genericParameter;
			}
			return ResolveGenericParameter(genericParameter);
		}
		if (typeReference is ArrayType arrayType)
		{
			return _typeFactory.CreateArrayType(Resolve(arrayType.ElementType), arrayType.Rank, arrayType.IsVector);
		}
		if (typeReference is PointerType pointerType)
		{
			return _typeFactory.CreatePointerType(Resolve(pointerType.ElementType));
		}
		if (typeReference is ByReferenceType byReferenceType)
		{
			return _typeFactory.CreateByReferenceType(Resolve(byReferenceType.ElementType));
		}
		if (typeReference is PinnedType pinnedType)
		{
			return _typeFactory.CreatePinnedType(Resolve(pinnedType.ElementType));
		}
		if (typeReference is GenericInstanceType { GenericArguments: var genericArgs } genericInstanceType)
		{
			TypeReference[] resolved = new TypeReference[genericArgs.Count];
			for (int i = 0; i < genericArgs.Count; i++)
			{
				resolved[i] = Resolve(genericArgs[i]);
			}
			return _typeFactory.CreateGenericInstanceType(genericInstanceType.TypeDef, genericInstanceType.DeclaringType, resolved);
		}
		if (typeReference is RequiredModifierType requiredModType)
		{
			return _typeFactory.CreateRequiredModifierType(requiredModType.ModifierType, Resolve(requiredModType.ElementType));
		}
		if (typeReference is OptionalModifierType optionalModType)
		{
			return _typeFactory.CreateOptionalModifierType(optionalModType.ModifierType, Resolve(optionalModType.ElementType));
		}
		if (typeReference is FunctionPointerType)
		{
			return _typeContext.GetIl2CppCustomType(Il2CppCustomType.Il2CppFNPtrFakeClass);
		}
		if (resolveGenericParameters && typeReference is TypeDefinition typeDefinition && typeReference.HasGenericParameters)
		{
			ReadOnlyCollection<GenericParameter> genericParams = typeDefinition.GenericParameters;
			TypeReference[] resolved2 = new TypeReference[genericParams.Count];
			for (int j = 0; j < genericParams.Count; j++)
			{
				resolved2[j] = Resolve(genericParams[j]);
			}
			return _typeFactory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, resolved2);
		}
		if (typeReference is TypeSpecification)
		{
			throw new NotSupportedException($"The type {typeReference} cannot be resolved correctly.");
		}
		return typeReference;
	}

	private TypeReference ResolveGenericParameter(GenericParameter genericParameter)
	{
		if (genericParameter.Owner == null)
		{
			return HandleOwnerlessInvalidILCode(genericParameter);
		}
		if (!(genericParameter.Owner is MemberReference))
		{
			throw new NotSupportedException();
		}
		if (genericParameter.Type != 0)
		{
			if (_methodDefinitionContext == null)
			{
				return genericParameter;
			}
			return _methodDefinitionContext.GenericArguments[genericParameter.Position];
		}
		return _typeDefinitionContext.GenericArguments[genericParameter.Position];
	}

	private TypeReference HandleOwnerlessInvalidILCode(GenericParameter genericParameter)
	{
		if (genericParameter.Type == GenericParameterType.Method && _typeDefinitionContext != null && genericParameter.Position < _typeDefinitionContext.GenericArguments.Count)
		{
			return _typeDefinitionContext.GenericArguments[genericParameter.Position];
		}
		return _typeContext.GetSystemType(SystemType.Object);
	}

	private bool IsDummy()
	{
		if (_typeDefinitionContext == null)
		{
			return _methodDefinitionContext == null;
		}
		return false;
	}
}
