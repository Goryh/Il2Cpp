using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public sealed class DefaultRuntimeMetadataAccess : IRuntimeMetadataAccess
{
	private readonly SourceWritingContext _context;

	private readonly MethodMetadataUsage _methodMetadataUsage;

	private readonly MethodUsage _methodUsage;

	private readonly TypeResolver _typeResolver;

	private int _initMetadataInline;

	private bool InitRuntimeDataInline => _initMetadataInline > 0;

	public DefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference methodReference, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
	{
		_context = context;
		_methodMetadataUsage = methodMetadataUsage;
		_methodUsage = methodUsage;
		_typeResolver = _context.Global.Services.TypeFactory.ResolverFor(methodReference?.DeclaringType, methodReference);
	}

	public string StaticData(TypeReference type)
	{
		if (TryGetWellKnownMetadataType(type, out var staticData))
		{
			return staticData;
		}
		TypeReference resolvedType = _typeResolver.Resolve(type);
		IIl2CppRuntimeType runtimeType = _context.Global.Collectors.Types.Add(resolvedType);
		_methodMetadataUsage.AddTypeInfo(runtimeType, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeTypeInfo, runtimeType, "RuntimeClass");
	}

	public string TypeInfoFor(TypeReference type)
	{
		return UnresolvedTypeInfoFor(_typeResolver.Resolve(type));
	}

	public string TypeInfoFor(TypeReference type, IRuntimeMetadataAccess.TypeInfoForReason reason)
	{
		return UnresolvedTypeInfoFor(_typeResolver.Resolve(type));
	}

	public string UnresolvedTypeInfoFor(TypeReference type)
	{
		if (TryGetWellKnownMetadataType(type, out var typeInfo))
		{
			return typeInfo;
		}
		IIl2CppRuntimeType runtimeType = _context.Global.Collectors.Types.Add(type);
		_methodMetadataUsage.AddTypeInfo(runtimeType, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeTypeInfo, runtimeType, "RuntimeClass");
	}

	public string Il2CppTypeFor(TypeReference type)
	{
		if (TryGetWellKnownMetadataType(type, out var typeInfo))
		{
			return "&" + typeInfo + "->byval_arg";
		}
		TypeReference resolvedType = _typeResolver.Resolve(type, resolveGenericParameters: false);
		IIl2CppRuntimeType runtimeType = _context.Global.Collectors.Types.Add(resolvedType);
		_methodMetadataUsage.AddIl2CppType(runtimeType, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeIl2CppType, runtimeType, "RuntimeType");
	}

	public string ArrayInfo(ArrayType arrayType)
	{
		return UnresolvedTypeInfoFor(_typeResolver.Resolve(arrayType));
	}

	public string MethodInfo(MethodReference method)
	{
		return UnresolvedMethodInfo(_typeResolver.Resolve(method));
	}

	public string UnresolvedMethodInfo(MethodReference method)
	{
		if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
		{
			_context.Global.Collectors.GenericMethods.Add(_context, method);
		}
		_methodMetadataUsage.AddInflatedMethod(method, InitRuntimeDataInline);
		return FormatRuntimeIdentifier((ReadOnlyContext naming, MethodReference method1) => _context.Global.Services.Naming.ForRuntimeMethodInfo(_context, method1), method, "RuntimeMethod");
	}

	public string HiddenMethodInfo(MethodReference method)
	{
		MethodReference methodReference = _typeResolver.Resolve(method);
		if ((method.IsGenericInstance || method.DeclaringType.IsGenericInstance) && !method.DeclaringType.IsDelegate)
		{
			_context.Global.Collectors.GenericMethods.Add(_context, methodReference);
			_methodMetadataUsage.AddInflatedMethod(methodReference, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((ReadOnlyContext naming, MethodReference method1) => _context.Global.Services.Naming.ForRuntimeMethodInfo(_context, method1), methodReference, "RuntimeMethod");
		}
		return "NULL";
	}

	public string Newobj(MethodReference ctor)
	{
		TypeReference resolvedDeclaringType = _typeResolver.Resolve(ctor.DeclaringType);
		IIl2CppRuntimeType runtimeType = _context.Global.Collectors.Types.Add(resolvedDeclaringType);
		_methodMetadataUsage.AddTypeInfo(runtimeType, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeTypeInfo, runtimeType, "RuntimeClass");
	}

	public string Method(MethodReference method)
	{
		if (MethodVerifier.IsNonGenericMethodThatDoesntExist(method))
		{
			method = method.Resolve();
		}
		MethodReference resolvedMethod = _typeResolver.Resolve(method);
		_methodUsage.AddMethod(resolvedMethod);
		if (method.ShouldInline(_context.Global.Parameters))
		{
			return resolvedMethod.CppName + "_inline";
		}
		return resolvedMethod.CppName;
	}

	public string FieldInfo(FieldReference field, TypeReference declaringType)
	{
		TypeReference resolvedDeclaringType = _typeResolver.Resolve(declaringType);
		Il2CppRuntimeFieldReference fieldUsage = new Il2CppRuntimeFieldReference(field, _context.Global.Collectors.Types.Add(resolvedDeclaringType));
		_methodMetadataUsage.AddFieldInfo(fieldUsage, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeFieldInfo, fieldUsage, "RuntimeField");
	}

	public string FieldRvaData(FieldReference field, TypeReference declaringType)
	{
		TypeReference resolvedDeclaringType = _typeResolver.Resolve(declaringType);
		Il2CppRuntimeFieldReference fieldUsage = new Il2CppRuntimeFieldReference(field, _context.Global.Collectors.Types.Add(resolvedDeclaringType));
		_methodMetadataUsage.AddFieldRvaInfo(fieldUsage);
		return _context.Global.Services.Naming.ForRuntimeFieldRvaStructStorage(_context, fieldUsage);
	}

	public string StringLiteral(string literal)
	{
		return StringLiteral(literal, _context.Global.Services.TypeProvider.Corlib);
	}

	public string StringLiteral(string literal, AssemblyDefinition assemblyDefinition)
	{
		if (literal == null)
		{
			return "NULL";
		}
		_methodMetadataUsage.AddStringLiteral(literal, assemblyDefinition, InitRuntimeDataInline);
		return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeUniqueStringLiteralIdentifier, literal, "String_t");
	}

	private string FormatRuntimeIdentifier<T>(Func<ReadOnlyContext, T, string> formatter, T value, string typeName)
	{
		string varName = formatter(_context, value);
		if (InitRuntimeDataInline)
		{
			return $"(({typeName}*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&{varName}))";
		}
		return varName;
	}

	public bool NeedsBoxingForValueTypeThis(MethodReference method)
	{
		return false;
	}

	public void AddInitializerStatement(string statement)
	{
		_methodMetadataUsage.AddInitializationStatement(statement);
	}

	public bool MustDoVirtualCallFor(ResolvedTypeInfo type, MethodReference methodToCall)
	{
		return false;
	}

	public void StartInitMetadataInline()
	{
		_initMetadataInline++;
	}

	public void EndInitMetadataInline()
	{
		_initMetadataInline--;
	}

	public IMethodMetadataAccess MethodMetadataFor(MethodReference unresolvedMethodToCall)
	{
		return new MethodMetadataAccess(this, unresolvedMethodToCall);
	}

	public IMethodMetadataAccess ConstrainedMethodMetadataFor(ResolvedTypeInfo constrainedType, ResolvedMethodInfo methodToCall)
	{
		throw new NotSupportedException("Constrained metadata is only supported in shared generic code");
	}

	private bool TryGetWellKnownMetadataType(TypeReference typeReference, out string metadata)
	{
		metadata = null;
		switch (typeReference.MetadataType)
		{
		case MetadataType.Void:
			metadata = "il2cpp_defaults.void_class";
			break;
		case MetadataType.Boolean:
			metadata = "il2cpp_defaults.boolean_class";
			break;
		case MetadataType.Char:
			metadata = "il2cpp_defaults.char_class";
			break;
		case MetadataType.SByte:
			metadata = "il2cpp_defaults.sbyte_class";
			break;
		case MetadataType.Byte:
			metadata = "il2cpp_defaults.byte_class";
			break;
		case MetadataType.Int16:
			metadata = "il2cpp_defaults.int16_class";
			break;
		case MetadataType.UInt16:
			metadata = "il2cpp_defaults.uint16_class";
			break;
		case MetadataType.Int32:
			metadata = "il2cpp_defaults.int32_class";
			break;
		case MetadataType.UInt32:
			metadata = "il2cpp_defaults.uint32_class";
			break;
		case MetadataType.Int64:
			metadata = "il2cpp_defaults.int64_class";
			break;
		case MetadataType.UInt64:
			metadata = "il2cpp_defaults.uint64_class";
			break;
		case MetadataType.Single:
			metadata = "il2cpp_defaults.single_class";
			break;
		case MetadataType.Double:
			metadata = "il2cpp_defaults.double_class";
			break;
		case MetadataType.String:
			metadata = "il2cpp_defaults.string_class";
			break;
		case MetadataType.IntPtr:
			metadata = "il2cpp_defaults.int_class";
			break;
		case MetadataType.UIntPtr:
			metadata = "il2cpp_defaults.uint_class";
			break;
		case MetadataType.Object:
			metadata = "il2cpp_defaults.object_class";
			break;
		case MetadataType.Class:
			if (typeReference.IsSystemType)
			{
				metadata = "il2cpp_defaults.systemtype_class";
			}
			else if (typeReference.IsSystemArray)
			{
				metadata = "il2cpp_defaults.array_class";
			}
			else if (typeReference.IsSystemEnum)
			{
				metadata = "il2cpp_defaults.enum_class";
			}
			else if (typeReference.IsSystemValueType)
			{
				metadata = "il2cpp_defaults.value_type_class";
			}
			break;
		}
		return metadata != null;
	}
}
