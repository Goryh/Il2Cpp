using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedTypeFactory
{
	private readonly TypeResolver _typeResolver;

	private readonly ITypeFactory _typeFactory;

	public ITypeFactory TypeFactory => _typeFactory;

	public static ResolvedTypeFactory Create(ReadOnlyContext context, TypeResolver typeResolver)
	{
		return new ResolvedTypeFactory(typeResolver, context.Global.Services.TypeFactory);
	}

	private ResolvedTypeFactory(TypeResolver typeResolver, ITypeFactory typeFactory)
	{
		_typeResolver = typeResolver;
		_typeFactory = typeFactory;
	}

	public ResolvedTypeInfo Create(TypeReference unresolvedType)
	{
		return Create(unresolvedType, _typeResolver.Resolve(unresolvedType));
	}

	public ResolvedFieldInfo Create(FieldReference fieldReference)
	{
		return new ResolvedFieldInfo(fieldReference, Create(GenericParameterResolver.ResolveFieldTypeIfNeeded(_typeFactory, fieldReference), _typeResolver.ResolveFieldType(fieldReference)), Create(fieldReference.DeclaringType));
	}

	public ResolvedParameter Create(ParameterDefinition parameterReference, MethodReference unresolvedMethod, MethodReference resolvedMethod)
	{
		TypeReference resolvedType = _typeResolver.ResolveParameterType(resolvedMethod, parameterReference);
		ResolvedTypeInfo typeInfo = Create(GenericParameterResolver.ResolveParameterTypeIfNeeded(_typeFactory, unresolvedMethod, parameterReference), resolvedType);
		return new ResolvedParameter(parameterReference.Index, parameterReference.Name, parameterReference.CppName, typeInfo);
	}

	public ResolvedParameter Create(ParameterDefinition parameterReference)
	{
		return new ResolvedParameter(parameterReference.Index, parameterReference.Name, parameterReference.CppName, Create(parameterReference.ParameterType));
	}

	public ResolvedVariable Create(VariableDefinition variableReference, MethodReference methodReference)
	{
		TypeReference resolvedType = _typeResolver.ResolveVariableType(methodReference, variableReference);
		return new ResolvedVariable(variableReference, Create(variableReference.VariableType, resolvedType));
	}

	public ResolvedTypeInfo ResolveReturnType(MethodReference methodReference)
	{
		TypeReference resolvedType = _typeResolver.ResolveReturnType(methodReference);
		return Create(GenericParameterResolver.ResolveReturnTypeIfNeeded(_typeFactory, methodReference), resolvedType);
	}

	public ResolvedTypeInfo ResolveThisType(MethodDefinition methodDefinition)
	{
		if (methodDefinition.DeclaringType.HasGenericParameters)
		{
			ITypeFactory typeFactory = _typeFactory;
			TypeDefinition typeDefinition = methodDefinition.DeclaringType.Resolve();
			TypeReference declaringType = methodDefinition.DeclaringType.DeclaringType;
			TypeReference[] genericArguments = methodDefinition.DeclaringType.GenericParameters.ToArray();
			GenericInstanceType unresolvedType = typeFactory.CreateGenericInstanceType(typeDefinition, declaringType, genericArguments);
			return Create(unresolvedType);
		}
		return Create(GenericParameterResolver.ResolveThisTypeIfNeeded(_typeFactory, methodDefinition));
	}

	public ResolvedCallSiteInfo Create(CallSite unresolvedCallSite)
	{
		ResolvedTypeInfo returnType = Create(unresolvedCallSite.ReturnType);
		ReadOnlyCollection<ResolvedParameter> parameters = (unresolvedCallSite.HasParameters ? unresolvedCallSite.Parameters.Select(Create).ToArray().AsReadOnly() : Array.Empty<ResolvedParameter>().AsReadOnly());
		return new ResolvedCallSiteInfo(unresolvedCallSite, returnType, parameters);
	}

	public ResolvedMethodInfo Create(MethodReference unresovledMethodReference)
	{
		MethodReference resolvedMethodReference = _typeResolver.Resolve(unresovledMethodReference);
		ResolvedTypeInfo declaringType = Create(unresovledMethodReference.DeclaringType, resolvedMethodReference.DeclaringType);
		ResolvedTypeInfo returnType = Create(GenericParameterResolver.ResolveReturnTypeIfNeeded(_typeFactory, unresovledMethodReference), _typeResolver.ResolveReturnType(resolvedMethodReference));
		ReadOnlyCollection<ResolvedParameter> parameters = (unresovledMethodReference.HasParameters ? unresovledMethodReference.Parameters.Select((ParameterDefinition p) => Create(p, unresovledMethodReference, resolvedMethodReference)).ToArray().AsReadOnly() : Array.Empty<ResolvedParameter>().AsReadOnly());
		return new ResolvedMethodInfo(unresovledMethodReference, resolvedMethodReference, declaringType, returnType, parameters);
	}

	public ResolvedMethodInfo CreateForConstrainedMethod(ResolvedMethodInfo origMethodToCall, ResolvedTypeInfo constrainedType, MethodReference constrainedTargetMethod)
	{
		if (origMethodToCall.IsGenericInstance)
		{
			constrainedTargetMethod = _typeResolver.Nested((GenericInstanceMethod)origMethodToCall.ResolvedMethodReference).Resolve(constrainedTargetMethod, resolveGenericParameters: true);
		}
		ResolvedTypeInfo declaringType = Create(constrainedType.UnresolvedType, constrainedTargetMethod.DeclaringType);
		ResolvedTypeInfo returnType = Create(origMethodToCall.ReturnType.UnresolvedType, GenericParameterResolver.ResolveReturnTypeIfNeeded(_typeFactory, constrainedTargetMethod));
		ReadOnlyCollection<ResolvedParameter> parameters = (origMethodToCall.UnresovledMethodReference.HasParameters ? origMethodToCall.UnresovledMethodReference.Parameters.Select((ParameterDefinition p) => Create(p, origMethodToCall.UnresovledMethodReference, constrainedTargetMethod)).ToArray().AsReadOnly() : Array.Empty<ResolvedParameter>().AsReadOnly());
		return new ResolvedMethodInfo(origMethodToCall.UnresovledMethodReference, constrainedTargetMethod, declaringType, returnType, parameters);
	}

	public ResolvedTypeInfo Create(TypeReference unresolvedType, TypeReference resolvedType)
	{
		return new ResolvedTypeInfo(unresolvedType.WithoutModifiers(), resolvedType.WithoutModifiers());
	}
}
