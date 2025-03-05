using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Visitor;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.GenericsCollection;

public class GenericContextAwareVisitor : Visitor
{
	private readonly PrimaryCollectionContext _context;

	private readonly IGenericsCollector _generics;

	private readonly GenericContext _genericContext;

	public GenericContextAwareVisitor(PrimaryCollectionContext context, IGenericsCollector generics, GenericContext genericContext)
	{
		_context = context;
		_generics = generics;
		_genericContext = genericContext;
	}

	protected override void Visit(MethodDefinition methodDefinition, Context context)
	{
		if ((methodDefinition.HasGenericParameters && _genericContext.Method == null) || ((_genericContext.Method != null) ? GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Method) : GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type)))
		{
			return;
		}
		if (!methodDefinition.HasBody && _context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetGenericSharingDataFor(_context, methodDefinition, out var sharingData))
		{
			foreach (RuntimeGenericData data in sharingData)
			{
				if (data is RuntimeGenericTypeData genericTypeData)
				{
					if (_context.Global.Services.TypeFactory.ResolverFor(_genericContext.Type, _genericContext.Method).Resolve(genericTypeData.GenericType) is GenericInstanceType genericInstanceType)
					{
						ProcessGenericType(_context, genericInstanceType, _generics);
					}
					else
					{
						Visit(genericTypeData.GenericType, context);
					}
					continue;
				}
				if (data is RuntimeGenericMethodData genericMethodData)
				{
					Visit(genericMethodData.GenericMethod, context);
					continue;
				}
				throw new NotImplementedException();
			}
		}
		base.Visit(methodDefinition, context);
	}

	protected override void Visit(PropertyDefinition propertyDefinition, Context context)
	{
		if (!GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type))
		{
			base.Visit(propertyDefinition, context);
		}
	}

	protected override void Visit(FieldDefinition fieldDefinition, Context context)
	{
		if (!GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type))
		{
			base.Visit(fieldDefinition, context);
		}
	}

	protected override void Visit(ArrayType arrayType, Context context)
	{
		ArrayType inflatedType = (ArrayType)Inflater.InflateType(_context, _genericContext, arrayType);
		ProcessArray(_context, inflatedType, _generics);
		base.Visit(inflatedType, context);
	}

	protected override void Visit(GenericInstanceType genericInstanceType, Context context)
	{
		GenericInstanceType inflatedType = Inflater.InflateType(_context, _genericContext, genericInstanceType);
		ProcessGenericType(inflatedType);
		base.Visit(inflatedType, context);
	}

	protected override void Visit(FieldReference fieldReference, Context context)
	{
		if (fieldReference.DeclaringType is GenericInstanceType && fieldReference.FieldType.ContainsGenericParameter)
		{
			VisitTypeReference(GenericParameterResolver.ResolveFieldTypeIfNeeded(_context.Global.Services.TypeFactory, fieldReference), context.ReturnType(fieldReference));
			VisitTypeReference(fieldReference.DeclaringType, context.DeclaringType(fieldReference));
		}
		else
		{
			base.Visit(fieldReference, context);
		}
	}

	protected override void Visit(MethodReference methodReference, Context context)
	{
		GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
		GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
		if (genericInstanceMethod != null || genericInstanceType != null)
		{
			VisitTypeReference(methodReference.DeclaringType, context.DeclaringType(methodReference));
			foreach (GenericParameter genericParameter in methodReference.GenericParameters)
			{
				VisitTypeReference(genericParameter, context.GenericParameter(methodReference));
			}
			VisitTypeReference(GenericParameterResolver.ResolveReturnTypeIfNeeded(_context.Global.Services.TypeFactory, methodReference), context.ReturnType(methodReference));
			foreach (ParameterDefinition parameterDefinition in methodReference.Parameters)
			{
				Visit(GenericParameterResolver.ResolveParameterTypeIfNeeded(_context.Global.Services.TypeFactory, methodReference, parameterDefinition), context.Parameter(methodReference));
			}
			if (genericInstanceMethod == null)
			{
				return;
			}
			ProcessGenericMethod(_context, Inflater.InflateMethod(_context, _genericContext, genericInstanceMethod), _generics);
			{
				foreach (TypeReference genericArgument in genericInstanceMethod.GenericArguments)
				{
					VisitTypeReference(genericArgument, context.GenericArgument(genericInstanceMethod));
				}
				return;
			}
		}
		base.Visit(methodReference, context);
	}

	protected override void Visit(Instruction instruction, Context context)
	{
		if (instruction.OpCode.Code == Code.Newarr)
		{
			TypeReference elementType = (TypeReference)instruction.Operand;
			ProcessArray(_context, _context.Global.Services.TypeFactory.CreateArrayType(Inflater.InflateType(_context, _genericContext, elementType), 1), _generics);
		}
		if (instruction.OpCode.Code == Code.Callvirt && instruction.Previous != null && instruction.Previous.OpCode.Code == Code.Constrained)
		{
			TypeReference constrainedType = (TypeReference)instruction.Previous.Operand;
			TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(_genericContext.Type, _genericContext.Method);
			TypeReference resolvedConstrainedType = typeResolver.Resolve(constrainedType);
			MethodReference methodRef = (MethodReference)instruction.Operand;
			if ((resolvedConstrainedType.IsGenericInstance || methodRef.IsGenericInstance) && resolvedConstrainedType.IsValueType)
			{
				if (resolvedConstrainedType is GenericInstanceType genericInstanceType)
				{
					ProcessGenericType(genericInstanceType);
				}
				if (methodRef.IsGenericInstance && methodRef.DeclaringType.IsInterface)
				{
					IVTableBuilderService vTable = _context.Global.Services.VTable;
					MethodReference resolvedMethodRef = typeResolver.Resolve(methodRef);
					VTableMultipleGenericInterfaceImpls multipleGenericInterfaceImpls;
					MethodReference targetMethod = vTable.GetVirtualMethodTargetMethodForConstrainedCallOnValueType(_context, resolvedConstrainedType, resolvedMethodRef, out multipleGenericInterfaceImpls);
					if (targetMethod != null)
					{
						GenericInstanceMethod inflatedTargetMethod = Inflater.InflateMethod(_context, new GenericContext(resolvedConstrainedType as GenericInstanceType, resolvedMethodRef as GenericInstanceMethod), targetMethod.Resolve());
						ProcessGenericMethod(_context, inflatedTargetMethod, _generics);
					}
				}
			}
		}
		base.Visit(instruction, context);
	}

	private void ProcessGenericType(GenericInstanceType inflatedType)
	{
		ProcessGenericType(_context, inflatedType, _generics);
	}

	internal static void ProcessGenericMethod(PrimaryCollectionContext context, GenericInstanceMethod method, IGenericsCollector generics)
	{
		if (method.DeclaringType.IsGenericInstance)
		{
			ProcessGenericType(context, (GenericInstanceType)method.DeclaringType, generics);
		}
		ProcessGenericArguments(context, method.GenericArguments, generics);
		MethodReference sharedMethod = GenericSharingAnalysis.GetSharedMethod(context, method);
		if (method.CanShare(context) && sharedMethod != method)
		{
			ProcessGenericMethod(context, (GenericInstanceMethod)sharedMethod, generics);
		}
		else
		{
			generics.AddMethod(method);
		}
	}

	internal static void ProcessGenericType(PrimaryCollectionContext context, GenericInstanceType type, IGenericsCollector generics)
	{
		generics.AddTypeDeclaration(type);
		ProcessGenericArguments(context, type.GenericArguments, generics);
		GenericInstanceType sharedType = GenericSharingAnalysis.GetSharedType(context, type);
		if (type.CanShare(context) && sharedType != type)
		{
			ProcessHardcodedDependencies(context, type, generics);
			ProcessGenericType(context, sharedType, generics);
		}
		else if (generics.AddType(type))
		{
			ProcessHardcodedDependencies(context, type, generics);
		}
	}

	private static void ProcessGenericArguments(ReadOnlyContext context, ReadOnlyCollection<TypeReference> genericArguments, IGenericsCollector generics)
	{
		for (int index = 0; index < genericArguments.Count; index++)
		{
			if (genericArguments[index] is GenericInstanceType genericInstanceType)
			{
				generics.AddTypeDeclaration(genericInstanceType);
			}
		}
	}

	private static void ProcessHardcodedDependencies(PrimaryCollectionContext context, GenericInstanceType type, IGenericsCollector generics)
	{
		ITypeProviderService typeProvider = context.Global.Services.TypeProvider;
		AddArrayIfNeeded(context, type, generics);
		if (type.GenericArguments.Count <= 0)
		{
			return;
		}
		TypeDefinition typeDef = type.Resolve();
		TypeReference genericArgument = type.GenericArguments[0];
		if (typeDef == typeProvider.GetSystemType(SystemType.EqualityComparer))
		{
			if (!genericArgument.IsNullableGenericInstance)
			{
				AddGenericComparerIfNeeded(context, genericArgument, generics, typeProvider.GetSystemType(SystemType.IEquatable_1), typeProvider.GetSystemType(SystemType.GenericEqualityComparer));
				AddEnumEqualityComparerIfNeeded(context, genericArgument, generics);
			}
			else
			{
				TypeReference nullableArgument = ((GenericInstanceType)genericArgument).GenericArguments[0];
				AddGenericComparerIfNeeded(context, nullableArgument, generics, typeProvider.GetSystemType(SystemType.IEquatable_1), typeProvider.GetSystemType(SystemType.NullableEqualityComparer));
			}
		}
		else if (typeDef == typeProvider.GetSystemType(SystemType.Comparer_1))
		{
			AddGenericComparerIfNeeded(context, genericArgument, generics, typeProvider.GetSystemType(SystemType.IComparable_1), typeProvider.GetSystemType(SystemType.GenericComparer));
		}
		else if (typeDef == typeProvider.GetSystemType(SystemType.ObjectComparer_1) && genericArgument.IsNullableGenericInstance)
		{
			TypeReference nullableArgument2 = ((GenericInstanceType)genericArgument).GenericArguments[0];
			AddGenericComparerIfNeeded(context, nullableArgument2, generics, typeProvider.GetSystemType(SystemType.IComparable_1), typeProvider.GetSystemType(SystemType.NullableComparer));
		}
	}

	private static void AddEnumEqualityComparerIfNeeded(PrimaryCollectionContext context, TypeReference keyType, IGenericsCollector generics)
	{
		if (keyType.IsEnum)
		{
			TypeReference underlyingEnumType = keyType.GetUnderlyingEnumType();
			TypeDefinition enumEqualityComparer = null;
			switch (underlyingEnumType.MetadataType)
			{
			case MetadataType.SByte:
				enumEqualityComparer = context.Global.Services.TypeProvider.GetSystemType(SystemType.SByteEnumEqualityComparer_1);
				break;
			case MetadataType.Int16:
				enumEqualityComparer = context.Global.Services.TypeProvider.GetSystemType(SystemType.ShortEnumEqualityComparer_1);
				break;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				enumEqualityComparer = context.Global.Services.TypeProvider.GetSystemType(SystemType.LongEnumEqualityComparer_1);
				break;
			default:
				enumEqualityComparer = context.Global.Services.TypeProvider.GetSystemType(SystemType.EnumEqualityComparer_1);
				break;
			}
			if (keyType.CanShare(context))
			{
				keyType = keyType.GetSharedType(context);
			}
			GenericInstanceType instanceTypeEnumEqualityComparer = enumEqualityComparer.CreateGenericInstanceType(context, keyType);
			ProcessGenericType(context, instanceTypeEnumEqualityComparer, generics);
		}
	}

	private static void AddArrayIfNeeded(PrimaryCollectionContext context, GenericInstanceType type, IGenericsCollector generics)
	{
		if (type.IsGraftedArrayInterfaceType)
		{
			ProcessArray(context, context.Global.Services.TypeFactory.CreateArrayType(type.GenericArguments[0]), generics);
		}
	}

	private static void AddGenericComparerIfNeeded(PrimaryCollectionContext context, TypeReference genericArgument, IGenericsCollector generics, TypeDefinition genericElementComparisonInterfaceDefinition, TypeDefinition genericComparerDefinition)
	{
		GenericInstanceType genericElementComparisonInterface = genericElementComparisonInterfaceDefinition.CreateGenericInstanceType(context, genericArgument);
		if (genericArgument.GetInterfaces(context).Any((TypeReference i) => i == genericElementComparisonInterface))
		{
			GenericInstanceType genericComparerInstance = genericComparerDefinition.CreateGenericInstanceType(context, genericArgument);
			ProcessGenericType(context, genericComparerInstance, generics);
		}
	}

	internal static void ProcessArray(PrimaryCollectionContext context, ArrayType inflatedType, IGenericsCollector generics)
	{
		if (!generics.AddArray(inflatedType))
		{
			return;
		}
		TypeDefinition arrayType = context.Global.Services.TypeProvider.GetSystemType(SystemType.Array);
		List<MethodDefinition> methods = new List<MethodDefinition>((arrayType != null) ? arrayType.Methods.Where((MethodDefinition m) => m.Name == "InternalArray__IEnumerable_GetEnumerator") : Enumerable.Empty<MethodDefinition>());
		foreach (MethodDefinition method in methods)
		{
			GenericInstanceMethod genericInstanceMethod = context.Global.Services.TypeFactory.CreateGenericInstanceMethod(method.DeclaringType, method, inflatedType.ElementType);
			GenericContextAwareVisitor visitor = new GenericContextAwareVisitor(context, generics, new GenericContext(null, genericInstanceMethod));
			method.Accept(visitor);
		}
		foreach (GenericInstanceMethod genericMethod in ArrayTypeInfoWriter.InflateArrayMethods(context, inflatedType))
		{
			ProcessGenericMethod(context, genericMethod, generics);
		}
		foreach (GenericInstanceType genericInstanceType in GetArrayExtraTypes(context, inflatedType))
		{
			ProcessGenericType(context, genericInstanceType, generics);
			foreach (MethodDefinition method2 in methods)
			{
				GenericInstanceMethod genericInstanceMethod2 = context.Global.Services.TypeFactory.CreateGenericInstanceMethod(method2.DeclaringType, method2, genericInstanceType.GenericArguments[0]);
				ProcessGenericMethod(context, genericInstanceMethod2, generics);
			}
		}
	}

	internal static IEnumerable<GenericInstanceType> GetArrayExtraTypes(ReadOnlyContext context, ArrayType type)
	{
		if (type.Rank != 1)
		{
			return new GenericInstanceType[0];
		}
		List<TypeReference> types = new List<TypeReference>();
		if (!type.ElementType.IsValueType)
		{
			types.AddRange(ArrayTypeInfoWriter.TypeAndAllBaseAndInterfaceTypesFor(context, type.ElementType));
			if (type.ElementType.IsArray)
			{
				types.AddRange(GetArrayExtraTypes(context, (ArrayType)type.ElementType));
			}
		}
		else
		{
			types.Add(type.ElementType);
		}
		return GetArrayExtraTypes(context, types);
	}

	private static IEnumerable<GenericInstanceType> GetArrayExtraTypes(ReadOnlyContext context, IEnumerable<TypeReference> types)
	{
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		ReadOnlyCollection<TypeDefinition> graftedArrayTypes = context.Global.Services.TypeProvider.GraftedArrayInterfaceTypes;
		foreach (TypeReference type in types)
		{
			foreach (TypeDefinition graftedArrayType in graftedArrayTypes)
			{
				yield return typeFactory.CreateGenericInstanceType(graftedArrayType, graftedArrayType.DeclaringType, type);
			}
		}
	}
}
