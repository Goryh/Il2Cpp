using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.CppDeclarations;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public static class Extensions
{
	private class MemberOrdingComparerBy<T, K> : IComparer<T> where K : MemberReference
	{
		private readonly Func<T, K> _selector;

		public MemberOrdingComparerBy(Func<T, K> selector)
		{
			_selector = selector;
		}

		public int Compare(T x, T y)
		{
			return string.Compare(_selector(x).FullName, _selector(y).FullName, StringComparison.Ordinal);
		}
	}

	private class MetadataTokenOrderingComparerBy<T> : IComparer<T>
	{
		private readonly Func<T, MetadataToken> _selector;

		public MetadataTokenOrderingComparerBy(Func<T, MetadataToken> selector)
		{
			_selector = selector;
		}

		public int Compare(T x, T y)
		{
			return _selector(x).ToUInt32().CompareTo(_selector(y).ToUInt32());
		}
	}

	private class OrderingComparerBy<T, K> : IComparer<T> where K : IComparable
	{
		private readonly Func<T, K> _selector;

		public OrderingComparerBy(Func<T, K> selector)
		{
			_selector = selector;
		}

		public int Compare(T x, T y)
		{
			return _selector(x).CompareTo(_selector(y));
		}
	}

	private class DictionaryValueOrderingComparer<TKey> : IComparer<KeyValuePair<TKey, int>>, IComparer<KeyValuePair<TKey, uint>>
	{
		public int Compare(KeyValuePair<TKey, int> x, KeyValuePair<TKey, int> y)
		{
			return x.Value.CompareTo(y.Value);
		}

		public int Compare(KeyValuePair<TKey, uint> x, KeyValuePair<TKey, uint> y)
		{
			return x.Value.CompareTo(y.Value);
		}
	}

	private class DictionaryValueOrderingComparer<TKey, TMetadataIndex> : IComparer<KeyValuePair<TKey, TMetadataIndex>> where TMetadataIndex : MetadataIndex
	{
		public int Compare(KeyValuePair<TKey, TMetadataIndex> x, KeyValuePair<TKey, TMetadataIndex> y)
		{
			return x.Value.Index.CompareTo(y.Value.Index);
		}
	}

	private class DictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<string, TValue>>
	{
		public int Compare(KeyValuePair<string, TValue> x, KeyValuePair<string, TValue> y)
		{
			return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
		}
	}

	private class MemberReferenceDictionaryKeyOrderingComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : MemberReference
	{
		public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
		{
			return string.Compare(x.Key.FullName, y.Key.FullName, StringComparison.Ordinal);
		}
	}

	private class Il2CppTypeDataDictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<Il2CppTypeData, TValue>>
	{
		public int Compare(KeyValuePair<Il2CppTypeData, TValue> x, KeyValuePair<Il2CppTypeData, TValue> y)
		{
			int namesCompare = string.Compare(x.Key.Type.FullName, y.Key.Type.FullName, StringComparison.Ordinal);
			if (namesCompare == 0)
			{
				return x.Key.Attrs.CompareTo(y.Key.Attrs);
			}
			return namesCompare;
		}
	}

	private class GenericMethodReferenceDictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<Il2CppMethodSpec, TValue>>
	{
		public int Compare(KeyValuePair<Il2CppMethodSpec, TValue> x, KeyValuePair<Il2CppMethodSpec, TValue> y)
		{
			return x.Key.GenericMethod.Compare(y.Key.GenericMethod);
		}
	}

	private class ToStringDictionaryKeyOrderingComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : class
	{
		public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
		{
			return string.Compare(x.Key.ToString(), y.Key.ToString(), StringComparison.Ordinal);
		}
	}

	private class OrderingComparer : IComparer<string>, IComparer<NPath>, IComparer<Il2CppMethodSpec>, IComparer<Il2CppRuntimeFieldReference>
	{
		public int Compare(string x, string y)
		{
			return string.Compare(x, y, StringComparison.Ordinal);
		}

		public int Compare(Il2CppRuntimeFieldReference x, Il2CppRuntimeFieldReference y)
		{
			return x.Field.Compare(y.Field);
		}

		public int Compare(Il2CppMethodSpec x, Il2CppMethodSpec y)
		{
			return x.GenericMethod.Compare(y.GenericMethod);
		}

		public int Compare(NPath x, NPath y)
		{
			return string.Compare(x.ToString(SlashMode.Forward), y.ToString(SlashMode.Forward), StringComparison.Ordinal);
		}
	}

	private class UnresolvedOrderingComparer : IComparer<ResolvedTypeInfo>
	{
		public static readonly UnresolvedOrderingComparer Default = new UnresolvedOrderingComparer();

		private UnresolvedOrderingComparer()
		{
		}

		public int Compare(ResolvedTypeInfo x, ResolvedTypeInfo y)
		{
			return x.UnresolvedType.Compare(y.UnresolvedType);
		}
	}

	public static bool HasFinalizer(this TypeDefinition type)
	{
		if (type.IsInterface)
		{
			return false;
		}
		if (type.MetadataType == MetadataType.Object)
		{
			return false;
		}
		if (type.BaseType == null)
		{
			return false;
		}
		if (!type.BaseType.Resolve().HasFinalizer())
		{
			return type.Methods.SingleOrDefault((MethodDefinition m) => m.IsFinalizerMethod) != null;
		}
		return true;
	}

	public static bool ShouldNotInline(this MethodReference method)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition == null)
		{
			return false;
		}
		return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.NoInlining);
	}

	public static bool ShouldAgressiveInline(this MethodReference method)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition == null)
		{
			return false;
		}
		return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.AggressiveInlining);
	}

	private static bool IsCheapGetterSetter(MethodDefinition method)
	{
		ReadOnlyCollection<Instruction> instructions = method.Body.Instructions;
		if (instructions.Count > 4)
		{
			return false;
		}
		Instruction fieldInstruction = null;
		if (instructions.Count == 2 && instructions[0].OpCode == OpCodes.Ldsfld && instructions[1].OpCode == OpCodes.Ret)
		{
			fieldInstruction = instructions[0];
		}
		else if (instructions.Count == 3 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Stsfld && instructions[2].OpCode == OpCodes.Ret)
		{
			fieldInstruction = instructions[1];
		}
		else if (instructions.Count == 3 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Ldfld && instructions[2].OpCode == OpCodes.Ret)
		{
			fieldInstruction = instructions[1];
		}
		else if (instructions.Count == 4 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Ldarg_1 && instructions[2].OpCode == OpCodes.Stfld && instructions[3].OpCode == OpCodes.Ret)
		{
			fieldInstruction = instructions[2];
		}
		if (fieldInstruction == null)
		{
			return false;
		}
		return true;
	}

	private static bool IsSmallWithoutCalls(MethodDefinition method)
	{
		if (method.Body.Instructions.Count <= 20)
		{
			return !method.Body.Instructions.Any((Instruction ins) => ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Calli || ins.OpCode.Code == Code.Callvirt);
		}
		return false;
	}

	private static bool IsCheapToInline(this MethodReference method)
	{
		MethodDefinition def = method.Resolve();
		if (def == null)
		{
			return false;
		}
		if (!def.HasBody)
		{
			if (def.DeclaringType.IsDelegate && def.Name == "Invoke")
			{
				return true;
			}
			return false;
		}
		return IsCheapGetterSetter(def);
	}

	public static bool ShouldInline(this MethodReference method, AssemblyConversionParameters parameters)
	{
		if (parameters.EnableInlining && !method.ShouldNotInline())
		{
			if (!method.ShouldAgressiveInline())
			{
				return method.IsCheapToInline();
			}
			return true;
		}
		return false;
	}

	public static bool ShouldNotOptimize(this MethodReference method)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition == null)
		{
			return false;
		}
		return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.NoOptimization);
	}

	public static IEnumerable<MethodReference> GetVirtualMethods(this TypeReference typeReference, ReadOnlyContext context)
	{
		ReadOnlyCollection<MethodReference> methods = typeReference.GetMethods(context.Global.Services.TypeFactory);
		for (int i = 0; i < methods.Count; i++)
		{
			MethodReference method = methods[i];
			if (method.IsVirtual && !method.IsStatic && !method.IsStripped)
			{
				yield return method;
			}
		}
	}

	public static IEnumerable<TypeDefinition> GetTypeHierarchy(this TypeDefinition type)
	{
		while (type != null)
		{
			yield return type;
			type = ((type.BaseType != null) ? type.BaseType.Resolve() : null);
		}
	}

	public static IEnumerable<TypeReference> GetTypeHierarchyWithInflatedGenericTypes(this GenericInstanceType type, ReadOnlyContext context)
	{
		for (TypeReference currentType = type; currentType != null; currentType = currentType.GetBaseType(context))
		{
			yield return currentType;
		}
	}

	public static FieldDefinition GetRuntimeRequiredField(this TypeDefinition type, string fieldName, ResolvedTypeInfo fieldType)
	{
		return type.GetRuntimeRequiredField(fieldName, fieldType.ResolvedType);
	}

	public static FieldDefinition GetRuntimeRequiredField(this TypeDefinition type, string fieldName, TypeReference fieldType)
	{
		return type.Fields.SingleOrDefault((FieldDefinition f) => f.Name == fieldName && f.FieldType == fieldType) ?? throw new InvalidOperationException($"Failed to retrieve runtime required field '{fieldType} {fieldName}' from {type.FullName}.");
	}

	public static bool IsSuitableForStaticFieldInTinyProfile(this TypeReference typeReference, ReadOnlyContext context)
	{
		typeReference = typeReference.WithoutModifiers();
		switch (typeReference.MetadataType)
		{
		case MetadataType.Boolean:
		case MetadataType.Char:
		case MetadataType.SByte:
		case MetadataType.Byte:
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
		case MetadataType.Int64:
		case MetadataType.UInt64:
		case MetadataType.Single:
		case MetadataType.Double:
		case MetadataType.Pointer:
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return true;
		default:
			return typeReference.IsSystemType;
		case MetadataType.ValueType:
		case MetadataType.GenericInstance:
		{
			if (!typeReference.IsValueType)
			{
				return false;
			}
			TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(typeReference);
			foreach (FieldDefinition field in typeReference.Resolve().Fields)
			{
				if (!field.IsStatic && !typeResolver.Resolve(field.FieldType).IsSuitableForStaticFieldInTinyProfile(context))
				{
					return false;
				}
			}
			return true;
		}
		}
	}

	private static bool IsArrayOrGenericParameter(TypeReference typeReference)
	{
		while (typeReference is TypeSpecification typeSpecification)
		{
			if (typeSpecification.IsArray)
			{
				return true;
			}
			if (typeSpecification.IsGenericParameter)
			{
				return true;
			}
			typeReference = typeSpecification.ElementType;
		}
		return false;
	}

	public static string GetWindowsRuntimePrimitiveName(this TypeReference type)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
			return "Boolean";
		case MetadataType.Char:
			return "Char16";
		case MetadataType.Byte:
			return "UInt8";
		case MetadataType.Int16:
			return "Int16";
		case MetadataType.UInt16:
			return "UInt16";
		case MetadataType.Int32:
			return "Int32";
		case MetadataType.UInt32:
			return "UInt32";
		case MetadataType.Int64:
			return "Int64";
		case MetadataType.UInt64:
			return "UInt64";
		case MetadataType.Single:
			return "Single";
		case MetadataType.Double:
			return "Double";
		case MetadataType.String:
			return "String";
		case MetadataType.Object:
			return "Object";
		case MetadataType.ValueType:
			if (type.Name == "Guid" && type.Namespace == "System")
			{
				return "Guid";
			}
			break;
		}
		return null;
	}

	public static string GetWindowsRuntimeTypeName(this TypeReference type, ReadOnlyContext context)
	{
		string primitiveName = type.GetWindowsRuntimePrimitiveName();
		if (primitiveName != null)
		{
			return primitiveName;
		}
		TypeReference windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
		if (windowsRuntimeType is GenericInstanceType genericInstanceType)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(genericInstanceType.Namespace);
			builder.Append('.');
			builder.Append(genericInstanceType.Name);
			builder.Append('<');
			bool hasSeparator = false;
			foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
			{
				if (hasSeparator)
				{
					builder.Append(',');
				}
				hasSeparator = true;
				builder.Append(genericArgument.GetWindowsRuntimeTypeName(context));
			}
			builder.Append('>');
			return builder.ToString();
		}
		return windowsRuntimeType.FullName;
	}

	private static bool AreGenericArgumentsValidForWindowsRuntimeType(ReadOnlyContext context, GenericInstanceType genericInstance)
	{
		foreach (TypeReference genericArgument in genericInstance.GenericArguments)
		{
			if (!genericArgument.IsValidForWindowsRuntimeType(context))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsValidForWindowsRuntimeType(this TypeReference type, ReadOnlyContext context)
	{
		if (type.IsWindowsRuntimePrimitiveType())
		{
			return true;
		}
		if (type.IsAttribute)
		{
			return false;
		}
		if (type.IsGenericInstance)
		{
			GenericInstanceType genericInstanceType = (GenericInstanceType)context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
			if (!IsComOrWindowsRuntimeType(context, genericInstanceType, (TypeDefinition typeDef) => typeDef.IsExposedToWindowsRuntime() && (typeDef.IsInterface || typeDef.IsDelegate)))
			{
				return false;
			}
			return AreGenericArgumentsValidForWindowsRuntimeType(context, genericInstanceType);
		}
		if (type.IsGenericParameter || type is TypeSpecification)
		{
			return false;
		}
		return context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type.Resolve()).IsExposedToWindowsRuntime();
	}

	private static bool IsComOrWindowsRuntimeType(ReadOnlyContext context, TypeReference type, Func<TypeDefinition, bool> predicate)
	{
		if (IsArrayOrGenericParameter(type))
		{
			return false;
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition == null)
		{
			return false;
		}
		if (!predicate(typeDefinition))
		{
			return false;
		}
		if (type is GenericInstanceType genericInstance)
		{
			return AreGenericArgumentsValidForWindowsRuntimeType(context, genericInstance);
		}
		return true;
	}

	public static bool IsWindowsRuntimeDelegate(this TypeReference type, ReadOnlyContext context)
	{
		return IsComOrWindowsRuntimeType(context, type, (TypeDefinition typeDef) => typeDef.IsDelegate && typeDef.IsExposedToWindowsRuntime());
	}

	public static bool IsComOrWindowsRuntimeInterface(this TypeReference type, ReadOnlyContext context)
	{
		return type.IsComOrWindowsRuntimeInterface(context.Global.Services.TypeFactory);
	}

	public static RuntimeStorageKind GetRuntimeStorage(this TypeReference typeReference, ReadOnlyContext context)
	{
		return typeReference.GetRuntimeStorage(context.Global.Services.TypeFactory);
	}

	public static RuntimeFieldLayoutKind GetRuntimeFieldLayout(this TypeReference typeReference, ReadOnlyContext context)
	{
		return typeReference.GetRuntimeFieldLayout(context.Global.Services.TypeFactory);
	}

	public static RuntimeFieldLayoutKind GetRuntimeStaticFieldLayout(this TypeReference typeReference, ReadOnlyContext context)
	{
		return typeReference.GetStaticRuntimeFieldLayout(context.Global.Services.TypeFactory);
	}

	public static RuntimeFieldLayoutKind GetThreadStaticRuntimeFieldLayout(this TypeReference typeReference, ReadOnlyContext context)
	{
		return typeReference.GetThreadStaticRuntimeFieldLayout(context.Global.Services.TypeFactory);
	}

	public static bool IsReturnedByRef(this TypeReference typeReference, ReadOnlyContext context)
	{
		return typeReference.GetRuntimeStorage(context).IsVariableSized();
	}

	public static bool ReturnValueIsByRef(this MethodReference methodReference, ReadOnlyContext context)
	{
		return GenericParameterResolver.ResolveReturnTypeIfNeeded(context.Global.Services.TypeFactory, methodReference).GetRuntimeStorage(context).IsVariableSized();
	}

	public static bool IsSpecialSystemBaseType(this TypeReference typeReference)
	{
		if (typeReference.Namespace == "System")
		{
			if (!(typeReference.Name == "Object") && !(typeReference.Name == "ValueType"))
			{
				return typeReference.Name == "Enum";
			}
			return true;
		}
		return false;
	}

	public static bool ContainsGenericArgumentsProjectableToClr(this GenericInstanceType type, ReadOnlyContext context)
	{
		foreach (TypeReference genericArgument in type.GenericArguments)
		{
			if (context.Global.Services.WindowsRuntime.ProjectToCLR(genericArgument) != genericArgument)
			{
				return true;
			}
			if (genericArgument is GenericInstanceType genericInstanceArgument && genericInstanceArgument.ContainsGenericArgumentsProjectableToClr(context))
			{
				return false;
			}
		}
		return false;
	}

	public static bool NeedsWindowsRuntimeFactory(this TypeDefinition type)
	{
		if (type.Module.MetadataKind != MetadataKind.ManagedWindowsMetadata)
		{
			return false;
		}
		if (!type.IsPublic)
		{
			return false;
		}
		if (type.HasGenericParameters)
		{
			return false;
		}
		if (type.IsInterface || type.IsValueType || type.IsAttribute || type.IsDelegate)
		{
			return false;
		}
		foreach (CustomAttribute customAttribute in type.CustomAttributes)
		{
			TypeReference attributeType = customAttribute.AttributeType;
			if (!(attributeType.Namespace != "Windows.Foundation.Metadata") && (attributeType.Name == "StaticAttribute" || attributeType.Name == "ActivatableAttribute"))
			{
				return true;
			}
		}
		foreach (MethodDefinition method in type.Methods)
		{
			if (method.IsConstructor && method.IsPublic && method.Parameters.Count == 0)
			{
				return true;
			}
		}
		return false;
	}

	public static bool NeedsComCallableWrapper(this TypeReference type, ReadOnlyContext context)
	{
		if (type.IsArray)
		{
			return type.GetInterfacesImplementedByComCallableWrapper(context).Any();
		}
		TypeDefinition typeDef = type.Resolve();
		if (typeDef.CanBoxToWindowsRuntime(context))
		{
			return true;
		}
		if (typeDef.IsInterface || typeDef.IsComOrWindowsRuntimeType() || typeDef.IsAbstract || typeDef.IsImport)
		{
			return false;
		}
		if (type is GenericInstanceType genericInstanceType && genericInstanceType.ContainsGenericArgumentsProjectableToClr(context))
		{
			return false;
		}
		if (!typeDef.IsValueType && context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDef) != typeDef && !typeDef.IsAttribute && !typeDef.IsDelegate)
		{
			return true;
		}
		if (type.GetInterfacesImplementedByComCallableWrapper(context).Any())
		{
			return true;
		}
		while (typeDef.BaseType != null)
		{
			typeDef = typeDef.BaseType.Resolve();
			if (typeDef.IsComOrWindowsRuntimeType())
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<TypeReference> ImplementedComOrWindowsRuntimeInterfaces(this TypeReference type, ReadOnlyContext context)
	{
		List<TypeReference> results = new List<TypeReference>();
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(type);
		foreach (InterfaceImplementation iface in type.Resolve().Interfaces)
		{
			TypeReference interfaceType = typeResolver.Resolve(iface.InterfaceType);
			if (interfaceType.IsComOrWindowsRuntimeInterface(context))
			{
				results.Add(interfaceType);
			}
		}
		return results;
	}

	public static IEnumerable<TypeReference> GetInterfacesImplementedByComCallableWrapper(this TypeReference type, ReadOnlyContext context)
	{
		if (type.IsNullableGenericInstance)
		{
			return Enumerable.Empty<TypeReference>();
		}
		HashSet<TypeReference> results = new HashSet<TypeReference>();
		foreach (TypeReference assignableType in GetAllValidComOrWindowsRuntimeTypesAssignableFrom(context, type, 0))
		{
			TypeReference windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, assignableType);
			if (windowsRuntimeType.IsComOrWindowsRuntimeInterface(context))
			{
				results.Add(windowsRuntimeType);
			}
		}
		return results;
	}

	private static void CollectComOrWindowsRuntimeTypesCovariantlyAssignableFrom(ReadOnlyContext context, GenericInstanceType type, HashSet<TypeReference> collectedTypes, int genericDepth)
	{
		TypeDefinition typeDefinition = type.Resolve();
		if (!typeDefinition.IsExposedToWindowsRuntime() && context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDefinition) == typeDefinition)
		{
			return;
		}
		if (genericDepth > 1)
		{
			collectedTypes.Add(type);
			return;
		}
		TypeReference[][] genericArgumentTypes = new TypeReference[type.GenericArguments.Count][];
		for (int i = 0; i < genericArgumentTypes.Length; i++)
		{
			TypeReference genericArgument = type.GenericArguments[i];
			GenericParameter genericParameter = typeDefinition.GenericParameters[i];
			GenericParameterAttributes genericParameterVariance = genericParameter.Attributes & GenericParameterAttributes.VarianceMask;
			switch (genericParameterVariance)
			{
			case GenericParameterAttributes.NonVariant:
				genericArgumentTypes[i] = ((!genericArgument.IsValidForWindowsRuntimeType(context)) ? new TypeReference[0] : new TypeReference[1] { genericArgument });
				break;
			case GenericParameterAttributes.Covariant:
				genericArgumentTypes[i] = (from t in GetAllValidComOrWindowsRuntimeTypesAssignableFrom(context, genericArgument, genericDepth + 1)
					where t.IsValidForWindowsRuntimeType(context)
					select t).ToArray();
				break;
			case GenericParameterAttributes.Contravariant:
				throw new NotSupportedException($"'{type.FullName}' type contains unsupported contravariant generic parameter '{genericParameter.Name}'.");
			default:
				throw new Exception($"'{genericParameter.Name}' generic parameter in '{type.FullName}' type contains invalid variance value '{genericParameterVariance}'.");
			}
			if (genericArgumentTypes[i].Length == 0)
			{
				return;
			}
		}
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		foreach (TypeReference[] combination in GetTypeCombinations(genericArgumentTypes))
		{
			GenericInstanceType result = typeFactory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, combination);
			collectedTypes.Add(result);
		}
	}

	private static IEnumerable<TypeReference> GetAllValidComOrWindowsRuntimeTypesAssignableFrom(ReadOnlyContext context, TypeReference type, int genericDepth)
	{
		HashSet<TypeReference> result = new HashSet<TypeReference>();
		CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, type, result, genericDepth);
		return result;
	}

	private static void CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(ReadOnlyContext context, TypeReference type, HashSet<TypeReference> collectedTypes, int genericDepth)
	{
		if (!collectedTypes.Add(type) || type.IsSystemObject)
		{
			return;
		}
		if (type.IsArray)
		{
			ArrayType arrayType = (ArrayType)type;
			if (arrayType.IsVector)
			{
				foreach (TypeDefinition interfaceType in context.Global.Services.TypeProvider.GraftedArrayInterfaceTypes)
				{
					GenericInstanceType interfaceInstance = context.Global.Services.TypeFactory.CreateGenericInstanceType(interfaceType, interfaceType.DeclaringType, arrayType.ElementType);
					CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, interfaceInstance, collectedTypes, genericDepth);
				}
			}
			TypeDefinition systemArray = context.Global.Services.TypeProvider.SystemArray;
			if (systemArray != null)
			{
				CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, systemArray, collectedTypes, genericDepth);
			}
			return;
		}
		if (!type.IsValueType)
		{
			if (type.IsGenericInstance)
			{
				CollectComOrWindowsRuntimeTypesCovariantlyAssignableFrom(context, (GenericInstanceType)type, collectedTypes, genericDepth);
			}
			TypeReference baseType = type.GetBaseType(context);
			if (baseType != null)
			{
				CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, baseType, collectedTypes, baseType.IsGenericInstance ? (genericDepth + 1) : genericDepth);
			}
		}
		foreach (TypeReference interfaceType2 in type.GetInterfaces(context))
		{
			CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, interfaceType2, collectedTypes, genericDepth);
		}
	}

	private static IEnumerable<TypeReference[]> GetTypeCombinations(TypeReference[][] types, int level = 0)
	{
		TypeReference[] levelTypes = types[level];
		if (level + 1 == types.Length)
		{
			TypeReference[] array = levelTypes;
			foreach (TypeReference type in array)
			{
				TypeReference[] result = new TypeReference[types.Length];
				result[types.Length - 1] = type;
				yield return result;
			}
			yield break;
		}
		IEnumerable<TypeReference[]> combinations = GetTypeCombinations(types, level + 1);
		foreach (TypeReference[] item in combinations)
		{
			TypeReference[] array2 = levelTypes;
			foreach (TypeReference type2 in array2)
			{
				TypeReference[] result2 = (TypeReference[])item.Clone();
				result2[level] = type2;
				yield return result2;
			}
		}
	}

	public static bool IsComOrWindowsRuntimeMethod(this MethodDefinition method, ReadOnlyContext context)
	{
		TypeDefinition declaringType = method.DeclaringType;
		if (declaringType.IsWindowsRuntime)
		{
			return true;
		}
		if (declaringType.Is(Il2CppCustomType.Il2CppComObject) || declaringType.Is(Il2CppCustomType.Il2CppComDelegate))
		{
			return true;
		}
		if (!declaringType.IsImport)
		{
			return false;
		}
		if (!method.IsInternalCall && !method.IsFinalizerMethod)
		{
			return declaringType.IsInterface;
		}
		return true;
	}

	private static IEnumerable<TypeReference> GetTypesFromSpecificAttribute(this TypeDefinition type, string attributeName, Func<CustomAttribute, TypeReference> customAttributeSelector)
	{
		return type.CustomAttributes.Where((CustomAttribute ca) => ca.AttributeType.FullName == attributeName).Select(customAttributeSelector);
	}

	public static IEnumerable<TypeReference> GetStaticFactoryTypes(this TypeReference type)
	{
		TypeDefinition typeDef = type.Resolve();
		if (!typeDef.IsWindowsRuntime || typeDef.IsValueType)
		{
			return Enumerable.Empty<TypeReference>();
		}
		return typeDef.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.StaticAttribute", (CustomAttribute attribute) => (TypeReference)attribute.ConstructorArguments[0].Value);
	}

	public static IEnumerable<TypeReference> GetActivationFactoryTypes(this TypeReference type, ReadOnlyContext context)
	{
		TypeDefinition typeDef = type.Resolve();
		if (!typeDef.IsWindowsRuntime || typeDef.IsValueType)
		{
			return Enumerable.Empty<TypeReference>();
		}
		return typeDef.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.ActivatableAttribute", delegate(CustomAttribute attribute)
		{
			CustomAttributeArgument customAttributeArgument = attribute.ConstructorArguments[0];
			return customAttributeArgument.Type.IsSystemType ? ((TypeReference)customAttributeArgument.Value) : context.Global.Services.TypeProvider.IActivationFactoryTypeReference;
		});
	}

	public static IEnumerable<TypeReference> GetComposableFactoryTypes(this TypeReference type)
	{
		TypeDefinition typeDef = type.Resolve();
		if (!typeDef.IsWindowsRuntime || typeDef.IsValueType)
		{
			return Enumerable.Empty<TypeReference>();
		}
		return typeDef.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.ComposableAttribute", (CustomAttribute attribute) => (TypeReference)attribute.ConstructorArguments[0].Value);
	}

	public static IEnumerable<TypeReference> GetAllFactoryTypes(this TypeReference type, ReadOnlyContext context)
	{
		TypeDefinition typeDef = type.Resolve();
		if (!typeDef.IsWindowsRuntime || typeDef.IsValueType)
		{
			return Enumerable.Empty<TypeReference>();
		}
		return typeDef.GetActivationFactoryTypes(context).Concat(typeDef.GetComposableFactoryTypes()).Concat(typeDef.GetStaticFactoryTypes())
			.Distinct();
	}

	public static TypeReference ExtractDefaultInterface(this TypeDefinition type)
	{
		if (!type.IsExposedToWindowsRuntime())
		{
			throw new ArgumentException("Extracting default interface is only valid for Windows Runtime types. " + type.FullName + " is not a Windows Runtime type.");
		}
		foreach (InterfaceImplementation interfaceImplementation in type.Interfaces)
		{
			foreach (CustomAttribute customAttribute in interfaceImplementation.CustomAttributes)
			{
				if (customAttribute.AttributeType.FullName == "Windows.Foundation.Metadata.DefaultAttribute")
				{
					return interfaceImplementation.InterfaceType;
				}
			}
		}
		throw new InvalidProgramException($"Windows Runtime class {type} has no default interface!");
	}

	public static bool CanBoxToWindowsRuntime(this TypeReference type, ReadOnlyContext context)
	{
		if (context.Global.Services.TypeProvider.IReferenceType == null)
		{
			return false;
		}
		if (type.MetadataType == MetadataType.Object)
		{
			return false;
		}
		if (type.IsWindowsRuntimePrimitiveType())
		{
			return true;
		}
		if (type is ArrayType arrayType)
		{
			if (context.Global.Services.TypeProvider.IReferenceArrayType == null)
			{
				return false;
			}
			if (!arrayType.IsVector)
			{
				return false;
			}
			if (arrayType.ElementType.IsArray)
			{
				return false;
			}
			if (!arrayType.ElementType.CanBoxToWindowsRuntime(context))
			{
				return arrayType.ElementType.MetadataType == MetadataType.Object;
			}
			return true;
		}
		TypeReference projectedToWindowsRuntime = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
		if (!projectedToWindowsRuntime.IsValueType)
		{
			return false;
		}
		if (projectedToWindowsRuntime == type)
		{
			return type.Resolve().IsExposedToWindowsRuntime();
		}
		return true;
	}

	public static bool StoresNonFieldsInStaticFields(this TypeReference type)
	{
		return type.HasActivationFactories;
	}

	public static MethodReference GetFactoryMethodForConstructor(this MethodReference constructor, IEnumerable<TypeReference> activationFactoryTypes, bool isComposing)
	{
		int extraParameterCount = (isComposing ? 2 : 0);
		foreach (TypeReference activationFactoryType in activationFactoryTypes)
		{
			foreach (MethodDefinition method in activationFactoryType.Resolve().Methods)
			{
				if (method.Parameters.Count - extraParameterCount != constructor.Parameters.Count)
				{
					continue;
				}
				bool matches = true;
				for (int i = 0; i < constructor.Parameters.Count; i++)
				{
					if (method.Parameters[i].ParameterType != constructor.Parameters[i].ParameterType)
					{
						matches = false;
						break;
					}
				}
				if (matches)
				{
					return method;
				}
			}
		}
		return null;
	}

	public static MethodReference GetOverriddenInterfaceMethod(this MethodReference overridingMethod, ReadOnlyContext context, IEnumerable<TypeReference> candidateInterfaces)
	{
		MethodDefinition methodDef = overridingMethod.Resolve();
		if (methodDef.Overrides.Count > 0)
		{
			if (methodDef.Overrides.Count != 1)
			{
				throw new InvalidOperationException("Cannot choose overridden method for '" + overridingMethod.FullName + "'");
			}
			return context.Global.Services.TypeFactory.ResolverFor(overridingMethod.DeclaringType, overridingMethod).Resolve(methodDef.Overrides[0]);
		}
		foreach (TypeReference candidateInterface in candidateInterfaces)
		{
			foreach (LazilyInflatedMethod interfaceMethod in candidateInterface.IterateLazilyInflatedMethods(context))
			{
				if (!(overridingMethod.Name != interfaceMethod.Name) && VirtualMethodResolution.MethodSignaturesMatchIgnoreStaticness(interfaceMethod.InflatedMethod, overridingMethod, context.Global.Services.TypeFactory))
				{
					return interfaceMethod.InflatedMethod;
				}
			}
		}
		return null;
	}

	public static bool DerivesFromObject(this TypeReference typeReference, ReadOnlyContext context)
	{
		TypeReference baseType = typeReference.GetBaseType(context);
		if (baseType == null)
		{
			return false;
		}
		return baseType.MetadataType == MetadataType.Object;
	}

	public static bool DerivesFrom(this TypeReference type, ReadOnlyContext context, TypeReference potentialBaseType, bool checkInterfaces = true)
	{
		while (type != null)
		{
			if (type == potentialBaseType)
			{
				return true;
			}
			if (checkInterfaces)
			{
				foreach (TypeReference @interface in type.GetInterfaces(context))
				{
					if (@interface == potentialBaseType)
					{
						return true;
					}
				}
			}
			type = type.GetBaseType(context);
		}
		return false;
	}

	public static bool HasIID(this TypeReference type, ReadOnlyContext context)
	{
		if (type.IsComOrWindowsRuntimeInterface(context))
		{
			return !type.HasGenericParameters;
		}
		return false;
	}

	public static bool HasCLSID(this TypeReference type)
	{
		if (type is TypeSpecification || type is GenericParameter)
		{
			return false;
		}
		return type.Resolve().HasCLSID();
	}

	public static bool HasCLSID(this TypeDefinition type)
	{
		if (!type.IsInterface && !type.HasGenericParameters)
		{
			return type.CustomAttributes.Any((CustomAttribute a) => a.AttributeType.FullName == "System.Runtime.InteropServices.GuidAttribute");
		}
		return false;
	}

	public static Guid GetGuid(this TypeReference type, ReadOnlyContext context)
	{
		return context.Global.Services.GuidProvider.GuidFor(context, type);
	}

	public static string ToInitializer(this Guid guid)
	{
		byte[] bytes = guid.ToByteArray();
		uint a = BitConverter.ToUInt32(bytes, 0);
		ushort b = BitConverter.ToUInt16(bytes, 4);
		ushort c = BitConverter.ToUInt16(bytes, 6);
		return "{" + $" 0x{a:x}, 0x{b:x}, 0x{c:x}, 0x{bytes[8]:x}, 0x{bytes[9]:x}, 0x{bytes[10]:x}, 0x{bytes[11]:x}, 0x{bytes[12]:x}, 0x{bytes[13]:x}, 0x{bytes[14]:x}, 0x{bytes[15]:x} " + "}";
	}

	public static IEnumerable<CustomAttribute> GetConstructibleCustomAttributes(this ICustomAttributeProvider customAttributeProvider)
	{
		return customAttributeProvider.CustomAttributes.Where(delegate(CustomAttribute ca)
		{
			TypeDefinition typeDefinition = ca.AttributeType.Resolve();
			return typeDefinition != null && !typeDefinition.IsWindowsRuntime;
		});
	}

	public static bool IsPrimitiveType(this MetadataType type)
	{
		if (type - 2 <= MetadataType.UInt64)
		{
			return true;
		}
		return false;
	}

	public static bool IsPrimitiveCppType(this string typeName)
	{
		switch (typeName)
		{
		case "bool":
		case "char":
		case "int64_t":
		case "wchar_t":
		case "uint8_t":
		case "int16_t":
		case "int32_t":
		case "double":
		case "int8_t":
		case "size_t":
		case "uint16_t":
		case "uint32_t":
		case "uint64_t":
		case "float":
			return true;
		default:
			return false;
		}
	}

	public static bool IsCallInstruction(this Instruction instruction)
	{
		Code code = instruction.OpCode.Code;
		if ((uint)(code - 39) <= 1u || code == Code.Callvirt || code == Code.Newobj)
		{
			return true;
		}
		return false;
	}

	public static ReadOnlyCollection<NPath> ToSortedCollection(this IEnumerable<NPath> set)
	{
		return set.ToSortedCollection(new OrderingComparer());
	}

	public static ReadOnlyCollection<string> ToSortedCollection(this IEnumerable<string> set)
	{
		return set.ToSortedCollection(new OrderingComparer());
	}

	public static ReadOnlyCollection<FieldReference> ToSortedCollection(this IEnumerable<FieldReference> set)
	{
		return set.ToSortedCollection(new FieldOrderingComparer());
	}

	public static ReadOnlyCollection<ArrayType> ToSortedCollection(this IEnumerable<ArrayType> set)
	{
		return set.ToSortedCollection(new TypeOrderingComparer());
	}

	public static ReadOnlyCollection<Il2CppMethodSpec> ToSortedCollection(this IEnumerable<Il2CppMethodSpec> set)
	{
		return set.ToSortedCollection(new OrderingComparer());
	}

	public static ReadOnlyCollection<Il2CppRuntimeFieldReference> ToSortedCollection(this IEnumerable<Il2CppRuntimeFieldReference> set)
	{
		return set.ToSortedCollection(new OrderingComparer());
	}

	public static ReadOnlyCollection<StringMetadataToken> ToSortedCollection(this IEnumerable<StringMetadataToken> set)
	{
		return set.ToSortedCollection(StringMetadataTokenComparer.Default);
	}

	public static ReadOnlyCollection<IIl2CppRuntimeType> ToSortedCollection(this IEnumerable<IIl2CppRuntimeType> set)
	{
		return set.ToSortedCollection(new Il2CppRuntimeTypeComparer());
	}

	public static ReadOnlyCollection<ResolvedTypeInfo> ToSortedCollectionByUnresolvedType(this IEnumerable<ResolvedTypeInfo> set)
	{
		return set.ToSortedCollection(UnresolvedOrderingComparer.Default);
	}

	public static ReadOnlyCollection<IIl2CppRuntimeType[]> ToSortedCollection(this IEnumerable<IIl2CppRuntimeType[]> set)
	{
		return set.ToSortedCollection(new Il2CppRuntimeTypeArrayComparer());
	}

	public static ReadOnlyCollection<CppDeclarationsData> ToSortedCollection(this IEnumerable<CppDeclarationsData> set, ReadOnlyContext context)
	{
		return set.ToSortedCollection(new CppDeclarationsComparer(context));
	}

	public static ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, int> selector)
	{
		return set.ToSortedCollection(new OrderingComparerBy<T, int>(selector));
	}

	public static ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, string> selector)
	{
		return set.ToSortedCollection(new OrderingComparerBy<T, string>(selector));
	}

	public static ReadOnlyCollection<T> ToSortedCollectionBy<T, K>(this IEnumerable<T> set, Func<T, K> selector) where K : MemberReference
	{
		return set.ToSortedCollection(new MemberOrdingComparerBy<T, K>(selector));
	}

	public static ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, MetadataToken> selector)
	{
		return set.ToSortedCollection(new MetadataTokenOrderingComparerBy<T>(selector));
	}

	public static ReadOnlyCollection<T> ToSortedCollection<T>(this IEnumerable<T> set, IComparer<T> comparer)
	{
		List<T> list = new List<T>(set);
		list.Sort(comparer);
		return list.AsReadOnly();
	}

	public static ReadOnlyCollection<KeyValuePair<TKey, int>> ItemsSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, int>> dict)
	{
		return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey>());
	}

	public static ReadOnlyCollection<KeyValuePair<TKey, uint>> ItemsSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, uint>> dict)
	{
		return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey>());
	}

	public static ReadOnlyCollection<KeyValuePair<TKey, TMetadataIndex>> ItemsSortedByValue<TKey, TMetadataIndex>(this IEnumerable<KeyValuePair<TKey, TMetadataIndex>> dict) where TMetadataIndex : MetadataIndex
	{
		return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey, TMetadataIndex>());
	}

	public static ReadOnlyCollection<KeyValuePair<string, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<string, TValue>> dict)
	{
		return dict.ToSortedCollection(new DictionaryKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<TKey, TValue>> ItemsSortedByKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict) where TKey : MemberReference
	{
		return dict.ToSortedCollection(new MemberReferenceDictionaryKeyOrderingComparer<TKey, TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<IIl2CppRuntimeType, TValue>> dict)
	{
		return dict.ToSortedCollection(new Il2CppRuntimeTypeKeyComparer<IIl2CppRuntimeType, TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<Il2CppTypeData, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<Il2CppTypeData, TValue>> dict)
	{
		return dict.ToSortedCollection(new Il2CppTypeDataDictionaryKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<Il2CppMethodSpec, TValue>> dict)
	{
		return dict.ToSortedCollection(new GenericMethodReferenceDictionaryKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<TKey, TValue>> ItemsSortedByKeyToString<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict) where TKey : class
	{
		return dict.ToSortedCollection(new ToStringDictionaryKeyOrderingComparer<TKey, TValue>());
	}

	public static ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, int>> dict)
	{
		return dict.KeysSortedByValue((TKey key) => key);
	}

	public static ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, uint>> dict)
	{
		return dict.KeysSortedByValue((TKey key) => key);
	}

	public static ReadOnlyCollection<TSelectedKeyValue> KeysSortedByValue<TKey, TSelectedKeyValue>(this IEnumerable<KeyValuePair<TKey, uint>> dict, Func<TKey, TSelectedKeyValue> selector)
	{
		return (from kvp in dict.ItemsSortedByValue()
			select selector(kvp.Key)).ToList().AsReadOnly();
	}

	public static ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IDictionary<TKey, int> dict)
	{
		return dict.KeysSortedByValue((TKey key) => key);
	}

	public static ReadOnlyCollection<TSelectedKeyValue> KeysSortedByValue<TKey, TSelectedKeyValue>(this IEnumerable<KeyValuePair<TKey, int>> dict, Func<TKey, TSelectedKeyValue> selector)
	{
		return (from kvp in dict.ItemsSortedByValue()
			select selector(kvp.Key)).ToList().AsReadOnly();
	}

	public static MethodReference ModuleInitializerMethod(this AssemblyDefinition assembly)
	{
		return assembly.MainModule.ModuleInitializer;
	}

	public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
	{
		key = tuple.Key;
		value = tuple.Value;
	}

	public static string GetModuleFileName(this ModuleDefinition module)
	{
		return Path.GetFileName(module.FileName ?? module.Name);
	}
}
