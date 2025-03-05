using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

internal class ReversePInvokeMethodBodyWriter : NativeToManagedInteropMethodBodyWriter, IReversePInvokeMethodBodyWriter
{
	protected ReversePInvokeMethodBodyWriter(ReadOnlyContext context, MethodReference managedMethod, MethodReference interopMethod, bool useUnicodeCharset)
		: base(context, managedMethod, interopMethod, MarshalType.PInvoke, useUnicodeCharset)
	{
	}

	internal static void WriteReversePInvokeMethodDefinitions(IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		if (InflateMonoInvokeCallbackAttributeTypes(writer.Context, method))
		{
			foreach (MethodReference interopMethod in GetReversePInvokeWrapperInteropMethods(writer.Context, method.Resolve()))
			{
				if (!GenericsUtilities.WasCollectedForMarshalling(writer.Context, interopMethod))
				{
					Create(writer.Context, method.Resolve(), interopMethod).WriteMethodDefinition(writer);
				}
			}
			return;
		}
		Create(writer.Context, method).WriteMethodDefinition(writer);
	}

	public static IReversePInvokeMethodBodyWriter Create(ReadOnlyContext context, MethodReference managedMethod)
	{
		if (context.Global.Parameters.FullGenericSharingOnly && managedMethod.IsGenericInstance)
		{
			return Create(context, managedMethod.Resolve(), managedMethod);
		}
		return Create(context, managedMethod, null);
	}

	private static IReversePInvokeMethodBodyWriter Create(ReadOnlyContext context, MethodReference managedMethod, MethodReference interopMethod)
	{
		if (managedMethod.IsUnmanagedCallersOnly)
		{
			return new UnmanagedCallersOnlyReversePInvokeMethodBodyWriter(managedMethod);
		}
		if (IsReversePInvokeWrapperNecessary(context, managedMethod))
		{
			if (interopMethod == null)
			{
				interopMethod = GetInteropMethod(context, managedMethod);
			}
			bool useUnicodeCharset = MarshalingUtils.UseUnicodeAsDefaultMarshalingForStringParameters(interopMethod);
			if (managedMethod.HasGenericParameters || managedMethod.DeclaringType.HasGenericParameters)
			{
				managedMethod = interopMethod;
			}
			return new ReversePInvokeMethodBodyWriter(context, managedMethod, interopMethod, useUnicodeCharset);
		}
		if (context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers)
		{
			return new ReversePInvokeNotImplementedMethodBodyWriter(managedMethod);
		}
		throw new InvalidOperationException("Attempting create a reverse p/invoke wrapper for method '" + managedMethod.FullName + "' when it cannot be implemented.");
	}

	public static bool IsReversePInvokeWrapperNecessary(ReadOnlyContext context, MethodReference method)
	{
		return WhyReversePInvokeWrapperCannotBeImplemented(context, method) == ReversePInvokeWrapperNotImplementedReason.None;
	}

	public static bool IsReversePInvokeWrapperNecessary(ReadOnlyContext context, in LazilyInflatedMethod method)
	{
		if (!CouldReversePInvokeWrapperBeNeeded(in method))
		{
			return false;
		}
		return IsThereAReasonWhyReversePInvokeWrapperCannotBeImplemented(context, in method) == ReversePInvokeWrapperNotImplementedReason.None;
	}

	public static bool IsReversePInvokeMethodThatMustBeGenerated(MethodReference method)
	{
		if (!method.IsGenericInstance)
		{
			return !method.DeclaringType.IsGenericInstance;
		}
		return false;
	}

	internal static bool CouldReversePInvokeWrapperBeNeeded(in LazilyInflatedMethod method)
	{
		MethodDefinition methodDefinition = method.Definition;
		if (HasMonoPInvokeCallbackAttributes(methodDefinition))
		{
			return true;
		}
		if (HasNativePInvokeCallbackAttributeWorthyOfReversePInvoke(methodDefinition))
		{
			return true;
		}
		return false;
	}

	internal static bool CouldReversePInvokeWrapperBeNeeded(TypeDefinition type)
	{
		ReadOnlyCollection<MethodDefinition> methods = type.Methods;
		if (!methods.Any(HasMonoPInvokeCallbackAttributes))
		{
			return methods.Any(HasNativePInvokeCallbackAttributeWorthyOfReversePInvoke);
		}
		return true;
	}

	internal static ReversePInvokeWrapperNotImplementedReason IsThereAReasonWhyReversePInvokeWrapperCannotBeImplemented(ReadOnlyContext context, in LazilyInflatedMethod lazilyInflatedMethod)
	{
		if (lazilyInflatedMethod.HasThis)
		{
			return ReversePInvokeWrapperNotImplementedReason.IsInstanceMethod;
		}
		if (!context.Global.Parameters.FullGenericSharingOnly && (lazilyInflatedMethod.DeclaringType.HasGenericParameters || lazilyInflatedMethod.InflatedMethod.HasGenericParameters))
		{
			return ReversePInvokeWrapperNotImplementedReason.HasGenericParameters;
		}
		MethodReference method = lazilyInflatedMethod.InflatedMethod;
		MethodReference interopMethod = method;
		if (InflateMonoInvokeCallbackAttributeTypes(context, method))
		{
			interopMethod = GetInteropMethod(context, method);
		}
		if (method.HasGenericParameters && interopMethod.HasGenericParameters)
		{
			return ReversePInvokeWrapperNotImplementedReason.HasGenericParameters;
		}
		if (method.DeclaringType.HasGenericParameters && interopMethod.DeclaringType.HasGenericParameters)
		{
			return ReversePInvokeWrapperNotImplementedReason.HasGenericParameters;
		}
		if (IntrinsicRemap.ShouldRemap(context, method, fullGenericSharing: false))
		{
			return ReversePInvokeWrapperNotImplementedReason.IsIntrinsicRemap;
		}
		if (method.ContainsFullySharedGenericTypes)
		{
			return ReversePInvokeWrapperNotImplementedReason.IsSharedGenericMethod;
		}
		return ReversePInvokeWrapperNotImplementedReason.None;
	}

	internal static ReversePInvokeWrapperNotImplementedReason WhyReversePInvokeWrapperCannotBeImplemented(ReadOnlyContext context, MethodReference method)
	{
		LazilyInflatedMethod fakeLazilyInflatedMethod = method.ToFakeLazilyInflatedMethod();
		if (IsThereAReasonWhyReversePInvokeWrapperCannotBeImplemented(context, in fakeLazilyInflatedMethod) == ReversePInvokeWrapperNotImplementedReason.None && CouldReversePInvokeWrapperBeNeeded(in fakeLazilyInflatedMethod))
		{
			return ReversePInvokeWrapperNotImplementedReason.None;
		}
		return ReversePInvokeWrapperNotImplementedReason.MissingPInvokeCallbackAttribute;
	}

	private static IEnumerable<MethodReference> GetReversePInvokeWrapperInteropMethods(ReadOnlyContext context, MethodDefinition methodDefinition)
	{
		List<MethodReference> reversePInvokeWrappers = new List<MethodReference>();
		foreach (TypeReference delegateType in GetDelegateTypesForMonoPInvokeCallbackAttribute(methodDefinition))
		{
			reversePInvokeWrappers.Add(GetInteropMethod(context, methodDefinition, delegateType));
		}
		if (!reversePInvokeWrappers.Any())
		{
			reversePInvokeWrappers.Add(GetInteropMethod(context, methodDefinition));
		}
		return reversePInvokeWrappers;
	}

	private static bool ParameterIsGenericInstance(ParameterDefinition p)
	{
		TypeReference typeReference = p.ParameterType;
		do
		{
			if (typeReference.IsGenericInstance)
			{
				return true;
			}
			typeReference = (typeReference as TypeSpecification)?.ElementType;
		}
		while (typeReference != null);
		return false;
	}

	public void WriteMethodDeclaration(IGeneratedCodeWriter writer)
	{
		MarshaledParameter[] parameters = Parameters;
		foreach (MarshaledParameter parameter in parameters)
		{
			MarshalInfoWriterFor(writer.Context, parameter).WriteIncludesForFieldDeclaration(writer);
		}
		MarshalInfoWriterFor(writer.Context, GetMethodReturnType()).WriteIncludesForFieldDeclaration(writer);
		writer.AddMethodForwardDeclaration(GetMethodSignature(writer.Context));
	}

	public void WriteMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteMethodWithMetadataInitialization(GetMethodSignature(writer.Context), WriteMethodBody, writer.Context.Global.Services.Naming.ForReversePInvokeWrapperMethod(writer.Context, _managedMethod), _managedMethod);
		writer.Context.Global.Collectors.ReversePInvokeWrappers.AddReversePInvokeWrapper(_managedMethod);
	}

	private string GetMethodSignature(ReadOnlyContext context)
	{
		string methodName = context.Global.Services.Naming.ForReversePInvokeWrapperMethod(context, _managedMethod);
		string methodReturnType = MarshaledReturnType.DecoratedName;
		string callingConvention = GetCallingConvention(context, _managedMethod);
		string parameters = MarshaledParameterTypes.Select((MarshaledType parameterType) => parameterType.DecoratedName + " " + parameterType.VariableName).AggregateWithComma(context);
		return $"extern \"C\" {methodReturnType} {callingConvention} {methodName}({parameters})";
	}

	private static IEnumerable<TypeReference> GetDelegateTypesForMonoPInvokeCallbackAttribute(MethodDefinition methodDef)
	{
		List<TypeReference> delegateTypes = new List<TypeReference>();
		foreach (CustomAttribute pinvokeCallbackAttribute in GetMonoPInvokeCallbackAttributes(methodDef))
		{
			if (pinvokeCallbackAttribute != null && pinvokeCallbackAttribute.HasConstructorArguments && (from argument in pinvokeCallbackAttribute.ConstructorArguments
				where argument.Type.IsSystemType
				select argument.Value).FirstOrDefault() is TypeReference delegateType)
			{
				delegateTypes.Add(delegateType);
			}
		}
		return delegateTypes.Distinct();
	}

	private static MethodReference GetInteropMethod(ReadOnlyContext context, MethodReference method)
	{
		IEnumerable<TypeReference> delegateTypes = GetDelegateTypesForMonoPInvokeCallbackAttribute(method.Resolve());
		if (delegateTypes.Any())
		{
			return GetInteropMethod(context, method, delegateTypes.First());
		}
		return method;
	}

	private static bool InflateMonoInvokeCallbackAttributeTypes(ReadOnlyContext context, MethodReference method)
	{
		if (context.Global.Parameters.FullGenericSharingOnly)
		{
			if (method.IsDefinition)
			{
				if (!method.HasGenericParameters)
				{
					return method.DeclaringType.HasGenericParameters;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private static MethodReference GetInteropMethod(ReadOnlyContext context, MethodReference method, TypeReference delegateType)
	{
		TypeDefinition delegateTypeDef = delegateType.Resolve();
		if (delegateTypeDef == null || !delegateTypeDef.IsDelegate)
		{
			return method;
		}
		MethodDefinition invokeMethod = delegateTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Invoke");
		if (invokeMethod == null)
		{
			return method;
		}
		if (delegateTypeDef.GenericParameters.Count != method.GenericParameters.Count + method.DeclaringType.GenericParameters.Count)
		{
			return method;
		}
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
		MethodReference resolvedInvokeMethod = context.Global.Services.TypeFactory.ResolverFor(typeResolver.Resolve(delegateTypeDef)).Resolve(invokeMethod);
		bool methodWasInflated = false;
		if (delegateType is GenericInstanceType)
		{
			TypeReference[] genericArguments = ((GenericInstanceType)delegateType).GenericArguments.ToArray();
			if (genericArguments.Length != ExpectedNumberOfGenericArguments(method))
			{
				return method;
			}
			IDataModelService typeFactory = context.Global.Services.TypeFactory;
			int methodGenericArgumentSkip = 0;
			GenericInstanceType genericInstanceType = null;
			if (method.DeclaringType.HasGenericParameters)
			{
				int genericParameterCount = method.DeclaringType.GenericParameters.Count;
				genericInstanceType = typeFactory.CreateGenericInstanceType((TypeDefinition)method.DeclaringType, method.DeclaringType.DeclaringType, genericArguments.Take(genericParameterCount).ToArray());
				method = typeFactory.ResolverFor(genericInstanceType).Resolve(method);
				methodWasInflated = true;
				methodGenericArgumentSkip = genericParameterCount;
			}
			if (method.HasGenericParameters || method.ContainsGenericParameter)
			{
				MethodDefinition methodDefinition = method.Resolve();
				GenericInstanceMethod genericInstanceMethod = typeFactory.CreateGenericInstanceMethod(genericInstanceType ?? method.DeclaringType, methodDefinition, genericArguments.Skip(methodGenericArgumentSkip).ToArray());
				method = Inflater.InflateMethod(context, new GenericContext(genericInstanceType, genericInstanceMethod), methodDefinition);
				methodWasInflated = true;
			}
		}
		if (methodWasInflated || !VirtualMethodResolution.MethodSignaturesMatchIgnoreStaticness(resolvedInvokeMethod, method, context.Global.Services.TypeFactory))
		{
			return method;
		}
		return invokeMethod;
	}

	private static int ExpectedNumberOfGenericArguments(MethodReference method)
	{
		int expectedNumberOfGenericArguments = 0;
		if (method.DeclaringType.HasGenericParameters)
		{
			expectedNumberOfGenericArguments += method.DeclaringType.GenericParameters.Count;
		}
		if (method.HasGenericParameters)
		{
			expectedNumberOfGenericArguments += method.GenericParameters.Count;
		}
		return expectedNumberOfGenericArguments;
	}

	private static bool HasMonoPInvokeCallbackAttributes(MethodDefinition methodDef)
	{
		return GetMonoPInvokeCallbackAttributes(methodDef).Any();
	}

	private static IEnumerable<CustomAttribute> GetMonoPInvokeCallbackAttributes(MethodDefinition methodDef)
	{
		return methodDef.CustomAttributes.Where((CustomAttribute attribute) => attribute.AttributeType.FullName.Contains("MonoPInvokeCallback"));
	}

	private static bool HasNativePInvokeCallbackAttributeWorthyOfReversePInvoke(MethodDefinition methodDef)
	{
		if (GetNativePInvokeCallbackAttribute(methodDef) != null)
		{
			return (methodDef.Attributes & MethodAttributes.PInvokeImpl) != 0;
		}
		return false;
	}

	private static CustomAttribute GetNativePInvokeCallbackAttribute(MethodDefinition methodDef)
	{
		return methodDef.CustomAttributes.FirstOrDefault((CustomAttribute attribute) => attribute.AttributeType.FullName.Contains("NativePInvokeCallback"));
	}

	protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReturnType methodReturnType = GetMethodReturnType();
		if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			string returnType = _typeResolver.Resolve(methodReturnType.ReturnType).CppNameForVariable;
			writer.WriteLine($"{returnType} {writer.Context.Global.Services.Naming.ForInteropReturnValue()};");
			WriteMethodCallStatement(metadataAccess, "NULL", localVariableNames, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
		}
		else
		{
			WriteMethodCallStatement(metadataAccess, "NULL", localVariableNames, writer);
		}
	}

	internal static string GetCallingConvention(ReadOnlyContext context, MethodReference managedMethod)
	{
		MethodDefinition managedMethodDefinition = managedMethod.Resolve();
		TypeReference[] delegateTypes = GetDelegateTypesForMonoPInvokeCallbackAttribute(managedMethodDefinition).ToArray();
		TypeReference typeReference;
		switch (delegateTypes.Length)
		{
		case 0:
			return "DEFAULT_CALL";
		default:
			typeReference = FindMatchingDelegateType(context, managedMethod, delegateTypes, managedMethodDefinition);
			break;
		case 1:
			typeReference = delegateTypes.First();
			break;
		}
		TypeReference delegateTypeToUse = typeReference;
		if (delegateTypeToUse == null)
		{
			return "DEFAULT_CALL";
		}
		TypeDefinition delegateTypedef = delegateTypeToUse.Resolve();
		if (delegateTypedef == null)
		{
			return "DEFAULT_CALL";
		}
		return InteropMethodBodyWriter.GetDelegateCallingConvention(delegateTypedef);
	}

	private static TypeReference FindMatchingDelegateType(ReadOnlyContext context, MethodReference managedMethod, IEnumerable<TypeReference> delegateTypes, MethodDefinition managedMethodDefinition)
	{
		foreach (TypeReference delegateType in delegateTypes)
		{
			if (GetInteropMethod(context, managedMethodDefinition, delegateType) == managedMethod)
			{
				return delegateType;
			}
		}
		return null;
	}

	protected override void WriteReturnStatementEpilogue(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName)
	{
		if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
		{
			writer.WriteLine($"return {unmarshaledReturnValueVariableName};");
		}
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (GetNativePInvokeCallbackAttribute(_managedMethod.Resolve()) == null)
		{
			base.WriteMethodPrologue(writer, metadataAccess);
		}
	}
}
