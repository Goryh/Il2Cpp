using System;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public sealed class SharedRuntimeMetadataAccess : IRuntimeMetadataAccess
{
	private enum SharedGenericCallType
	{
		Normal,
		Indirect,
		Invoker,
		InvokerIfSharedGeneric
	}

	private class SharedMethodMetadataAccess : IMethodMetadataAccess
	{
		private readonly MethodReference _unresolvedMethodToCall;

		private readonly SharedRuntimeMetadataAccess _runtimeMetadataAccess;

		private readonly string _hiddenMethodInfo;

		private readonly bool _hasCustomHiddenMethodInfo;

		private readonly bool _forAdjustorThunk;

		public IRuntimeMetadataAccess RuntimeMetadataAccess => _runtimeMetadataAccess;

		public bool IsConstrainedCall => false;

		public SharedMethodMetadataAccess(SharedRuntimeMetadataAccess runtimeMetadataAccess, MethodReference unresolvedMethodToCall, string hiddenMethodInfo, bool hasCustomHiddenMethodInfo, bool forAdjustorThunk)
		{
			_unresolvedMethodToCall = unresolvedMethodToCall;
			_runtimeMetadataAccess = runtimeMetadataAccess;
			_hiddenMethodInfo = hiddenMethodInfo;
			_hasCustomHiddenMethodInfo = hasCustomHiddenMethodInfo;
			_forAdjustorThunk = forAdjustorThunk;
		}

		public string Method(MethodReference resolvedMethodToCall)
		{
			if (_runtimeMetadataAccess.GetSharedGenericCallType(resolvedMethodToCall, MethodCallType.Normal) == SharedGenericCallType.Normal)
			{
				return _runtimeMetadataAccess.Method(resolvedMethodToCall);
			}
			return _runtimeMetadataAccess.Method(_unresolvedMethodToCall);
		}

		public string MethodInfo()
		{
			return _runtimeMetadataAccess.MethodInfo(_unresolvedMethodToCall);
		}

		public string HiddenMethodInfo()
		{
			if (!_hasCustomHiddenMethodInfo)
			{
				return _runtimeMetadataAccess.HiddenMethodInfo(_unresolvedMethodToCall);
			}
			return _hiddenMethodInfo;
		}

		public string TypeInfoForDeclaringType()
		{
			return _runtimeMetadataAccess.TypeInfoFor(_unresolvedMethodToCall.DeclaringType);
		}

		public bool DoCallViaInvoker(MethodReference resolvedMethodToCall, MethodCallType callType)
		{
			if (!_forAdjustorThunk)
			{
				return _runtimeMetadataAccess.DoCallViaInvoker(_unresolvedMethodToCall, resolvedMethodToCall, callType);
			}
			return false;
		}

		public IMethodMetadataAccess OverrideHiddenMethodInfo(string newHiddenMethodInfo)
		{
			return new SharedMethodMetadataAccess(_runtimeMetadataAccess, _unresolvedMethodToCall, newHiddenMethodInfo, hasCustomHiddenMethodInfo: true, _forAdjustorThunk);
		}

		public IMethodMetadataAccess ForAdjustorThunk()
		{
			return new SharedMethodMetadataAccess(_runtimeMetadataAccess, _unresolvedMethodToCall, _hiddenMethodInfo, _hasCustomHiddenMethodInfo, forAdjustorThunk: true);
		}
	}

	private class ConstrainedMethodMetadataAccess : IMethodMetadataAccess
	{
		private readonly ResolvedTypeInfo _constrainedType;

		private readonly MethodReference _constrainedMethod;

		private readonly SharedRuntimeMetadataAccess _runtimeMetadataAccess;

		public IRuntimeMetadataAccess RuntimeMetadataAccess => _runtimeMetadataAccess;

		public bool IsConstrainedCall => true;

		public ConstrainedMethodMetadataAccess(SharedRuntimeMetadataAccess runtimeMetadataAccess, ResolvedTypeInfo constrainedType, ResolvedMethodInfo constrainedMethod)
		{
			_constrainedType = constrainedType;
			_constrainedMethod = constrainedMethod.UnresovledMethodReference;
			_runtimeMetadataAccess = runtimeMetadataAccess;
		}

		public string Method(MethodReference resolvedMethodToCall)
		{
			return _runtimeMetadataAccess.Method(resolvedMethodToCall);
		}

		public string MethodInfo()
		{
			return _runtimeMetadataAccess.ConstrainedHiddenMethodInfo(_constrainedType.UnresolvedType, _constrainedMethod);
		}

		public string HiddenMethodInfo()
		{
			return _runtimeMetadataAccess.ConstrainedHiddenMethodInfo(_constrainedType.UnresolvedType, _constrainedMethod);
		}

		public bool DoCallViaInvoker(MethodReference resolvedMethodToCall, MethodCallType callType)
		{
			if (!_constrainedType.GetRuntimeStorage(_runtimeMetadataAccess._context).IsByValue())
			{
				return _runtimeMetadataAccess.DoCallViaInvoker(_constrainedType.UnresolvedType, _constrainedMethod, resolvedMethodToCall, callType);
			}
			return false;
		}

		public string TypeInfoForDeclaringType()
		{
			return _runtimeMetadataAccess.TypeInfoFor(_constrainedMethod.DeclaringType);
		}

		public IMethodMetadataAccess OverrideHiddenMethodInfo(string newHiddenMethodInfo)
		{
			throw new NotSupportedException();
		}

		public IMethodMetadataAccess ForAdjustorThunk()
		{
			throw new NotSupportedException();
		}
	}

	private readonly SourceWritingContext _context;

	private readonly MethodReference _enclosingMethod;

	private readonly TypeResolver _typeResolver;

	private readonly DefaultRuntimeMetadataAccess _default;

	private readonly WritingMethodFor _writingMethodFor;

	private readonly GenericSharingAnalysisResults _genericSharingAnalysis;

	private GenericContextUsage _methodRgctxUsage;

	public GenericContextUsage GetMethodRgctxDataUsage()
	{
		return _methodRgctxUsage;
	}

	public SharedRuntimeMetadataAccess(SourceWritingContext context, MethodReference enclosingMethod, DefaultRuntimeMetadataAccess defaultRuntimeMetadataAccess, WritingMethodFor writingMethodFor)
	{
		_context = context;
		_enclosingMethod = enclosingMethod;
		_typeResolver = _context.Global.Services.TypeFactory.ResolverFor(enclosingMethod.DeclaringType, enclosingMethod);
		_default = defaultRuntimeMetadataAccess;
		_writingMethodFor = writingMethodFor;
		_genericSharingAnalysis = context.Global.Results.PrimaryCollection.GenericSharingAnalysis;
	}

	public string StaticData(TypeReference type)
	{
		return RetrieveType(type, _enclosingMethod, () => _default.StaticData(type), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Static, _genericSharingAnalysis);
	}

	public string TypeInfoFor(TypeReference type)
	{
		return TypeInfoFor(type, IRuntimeMetadataAccess.TypeInfoForReason.Any);
	}

	public string TypeInfoFor(TypeReference type, IRuntimeMetadataAccess.TypeInfoForReason reason)
	{
		string rgctxAccessMethod = "il2cpp_rgctx_data";
		if ((uint)(reason - 1) <= 4u)
		{
			rgctxAccessMethod = "il2cpp_rgctx_data_no_init";
		}
		return RetrieveType(type, _enclosingMethod, () => _default.TypeInfoFor(type), (int index) => Emit.Call(_context, rgctxAccessMethod, GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, rgctxAccessMethod, "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
	}

	public string UnresolvedTypeInfoFor(TypeReference type)
	{
		return RetrieveType(type, _enclosingMethod, () => _default.UnresolvedTypeInfoFor(type), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
	}

	public string Il2CppTypeFor(TypeReference type)
	{
		return RetrieveType(type, _enclosingMethod, () => _default.Il2CppTypeFor(type), (int index) => Emit.Call(_context, "il2cpp_rgctx_type", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_type", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Type, _genericSharingAnalysis);
	}

	public string ArrayInfo(ArrayType arrayType)
	{
		return RetrieveType(arrayType.ElementType, _enclosingMethod, () => _default.ArrayInfo(arrayType), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Array, _genericSharingAnalysis);
	}

	public string Newobj(MethodReference ctor)
	{
		return RetrieveType(ctor.DeclaringType, _enclosingMethod, () => _default.Newobj(ctor), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_data", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
	}

	private string GetTypeRgctxDataExpression()
	{
		string declaringTypeExpression = "method->klass";
		if (!_enclosingMethod.HasThis || _enclosingMethod.DeclaringType.IsValueType)
		{
			declaringTypeExpression = Emit.InitializedTypeInfo(declaringTypeExpression);
		}
		return declaringTypeExpression + "->rgctx_data";
	}

	public string Method(MethodReference method)
	{
		MethodReference methodReference = _typeResolver.Resolve(method);
		return RetrieveMethod<string>(method, _enclosingMethod, () => _default.Method(method), (int index) => "(" + Emit.Cast(MethodSignatureWriter.GetMethodPointer(_context, methodReference), Emit.Call(_context, "il2cpp_codegen_get_direct_method_pointer", Emit.Call(_context, "il2cpp_rgctx_method", GetTypeRgctxDataExpression(), index.ToString()))) + ")", (int index) => "(" + Emit.Cast(MethodSignatureWriter.GetMethodPointer(_context, methodReference), Emit.Call(_context, "il2cpp_codegen_get_direct_method_pointer", Emit.Call(_context, "il2cpp_rgctx_method", "method->rgctx_data", index.ToString()))) + ")", RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
	}

	private bool DoCallViaInvoker(MethodReference unresolvedMethod, MethodReference resolvedMethodToCall, MethodCallType callType)
	{
		return GetDoCalViaInvoker(GetSharedGenericCallType(resolvedMethodToCall, callType), () => RetrieveMethod(unresolvedMethod, _enclosingMethod, () => false, (int _) => true, (int _) => true, RuntimeGenericContextInfo.Method, _genericSharingAnalysis));
	}

	private bool DoCallViaInvoker(TypeReference constrainedType, MethodReference constrainedMethod, MethodReference resolvedMethodToCall, MethodCallType callType)
	{
		return GetDoCalViaInvoker(GetSharedGenericCallType(resolvedMethodToCall, callType), () => RetrieveConstrainedMethod(constrainedType, constrainedMethod, _enclosingMethod, () => false, (int _) => true, (int _) => true, _genericSharingAnalysis));
	}

	private static bool GetDoCalViaInvoker(SharedGenericCallType sharedGenericCallType, Func<bool> isSharedGeneric)
	{
		switch (sharedGenericCallType)
		{
		case SharedGenericCallType.Normal:
		case SharedGenericCallType.Indirect:
			return false;
		case SharedGenericCallType.Invoker:
			return true;
		case SharedGenericCallType.InvokerIfSharedGeneric:
			return isSharedGeneric();
		default:
			throw new InvalidOperationException($"Unsupported {"SharedGenericCallType"} {sharedGenericCallType}");
		}
	}

	private SharedGenericCallType GetSharedGenericCallType(MethodReference resolvedMethodToCall, MethodCallType callType)
	{
		if (!resolvedMethodToCall.CanShare(_context))
		{
			return SharedGenericCallType.Normal;
		}
		if (!MethodSignatureWriter.NeedsMethodMetadataCollected(_context, resolvedMethodToCall, _enclosingMethod.ContainsFullySharedGenericTypes))
		{
			return SharedGenericCallType.Normal;
		}
		if (resolvedMethodToCall.ContainsFullySharedGenericTypes)
		{
			if (callType == MethodCallType.Virtual && resolvedMethodToCall.HasFullGenericSharingSignature(_context))
			{
				return SharedGenericCallType.Invoker;
			}
			if (_context.Global.Parameters.FullGenericSharingOnly)
			{
				return SharedGenericCallType.Normal;
			}
			if (resolvedMethodToCall.HasFullGenericSharingSignature(_context))
			{
				return SharedGenericCallType.Invoker;
			}
			return SharedGenericCallType.Indirect;
		}
		if (_context.Global.Parameters.DisableFullGenericSharing)
		{
			if (resolvedMethodToCall.IsStatic || resolvedMethodToCall.DeclaringType.IsValueType)
			{
				return SharedGenericCallType.Normal;
			}
			return SharedGenericCallType.Indirect;
		}
		if (_context.Global.Parameters.SharedGenericCallsViaInvokers)
		{
			return SharedGenericCallType.InvokerIfSharedGeneric;
		}
		return SharedGenericCallType.Normal;
	}

	public bool NeedsBoxingForValueTypeThis(MethodReference method)
	{
		return RetrieveMethod(method, _enclosingMethod, () => false, (int index) => true, (int index) => true, RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
	}

	public void AddInitializerStatement(string statement)
	{
		_default.AddInitializerStatement(statement);
	}

	public bool MustDoVirtualCallFor(ResolvedTypeInfo type, MethodReference methodToCall)
	{
		return false;
	}

	public void StartInitMetadataInline()
	{
		_default.StartInitMetadataInline();
	}

	public void EndInitMetadataInline()
	{
		_default.EndInitMetadataInline();
	}

	public IMethodMetadataAccess MethodMetadataFor(MethodReference unresolvedMethodToCall)
	{
		return new SharedMethodMetadataAccess(this, unresolvedMethodToCall, null, hasCustomHiddenMethodInfo: false, forAdjustorThunk: false);
	}

	public IMethodMetadataAccess ConstrainedMethodMetadataFor(ResolvedTypeInfo constrainedType, ResolvedMethodInfo methodToCall)
	{
		return new ConstrainedMethodMetadataAccess(this, constrainedType, methodToCall);
	}

	public string FieldInfo(FieldReference field, TypeReference declaringType)
	{
		if (GetRGCTXAccess(declaringType, _enclosingMethod) == RuntimeGenericAccess.None)
		{
			return _default.FieldInfo(field);
		}
		string typeInfo = TypeInfoFor(declaringType, IRuntimeMetadataAccess.TypeInfoForReason.Field);
		return $"il2cpp_rgctx_field({typeInfo},{field.FieldIndex})";
	}

	public string FieldRvaData(FieldReference field, TypeReference declaringType)
	{
		return _default.FieldRvaData(field, declaringType);
	}

	public string MethodInfo(MethodReference method)
	{
		if (_writingMethodFor == WritingMethodFor.MethodBody && (method == _enclosingMethod || method == _enclosingMethod.Resolve()))
		{
			return "method";
		}
		return RetrieveMethod(method, _enclosingMethod, () => _default.MethodInfo(method), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
	}

	public string UnresolvedMethodInfo(MethodReference method)
	{
		if (_writingMethodFor == WritingMethodFor.MethodBody && (method == _enclosingMethod || method == _enclosingMethod.Resolve()))
		{
			return "method";
		}
		return RetrieveMethod(method, _enclosingMethod, () => _default.UnresolvedMethodInfo(method), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
	}

	public string HiddenMethodInfo(MethodReference method)
	{
		if (method.IsGenericHiddenMethodNeverUsed)
		{
			return "NULL";
		}
		if (_writingMethodFor == WritingMethodFor.MethodBody && (method == _enclosingMethod || method == _enclosingMethod.Resolve()))
		{
			return "method";
		}
		return RetrieveMethod(method, _enclosingMethod, () => _default.HiddenMethodInfo(method), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", "method->rgctx_data", index.ToString()), RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
	}

	public string ConstrainedHiddenMethodInfo(TypeReference constrainedType, MethodReference method)
	{
		if (method.IsGenericHiddenMethodNeverUsed)
		{
			return "NULL";
		}
		if (_writingMethodFor == WritingMethodFor.MethodBody && (method == _enclosingMethod || method == _enclosingMethod.Resolve()))
		{
			return "method";
		}
		return RetrieveConstrainedMethod(constrainedType, method, _enclosingMethod, () => _default.HiddenMethodInfo(method), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", GetTypeRgctxDataExpression(), index.ToString()), (int index) => Emit.Call(_context, "il2cpp_rgctx_method", "method->rgctx_data", index.ToString()), _genericSharingAnalysis);
	}

	public string StringLiteral(string literal)
	{
		return StringLiteral(literal, _context.Global.Services.TypeProvider.Corlib);
	}

	public string StringLiteral(string literal, AssemblyDefinition assemblyDefinition)
	{
		return _default.StringLiteral(literal, assemblyDefinition);
	}

	private T RetrieveMethod<T>(MethodReference method, MethodReference enclosingMethod, Func<T> defaultFunc, Func<int, T> retrieveTypeSharedAccess, Func<int, T> retrieveMethodSharedAccess, RuntimeGenericContextInfo info, GenericSharingAnalysisResults genericSharingAnalysisService)
	{
		switch (GetRGCTXAccess(method, enclosingMethod))
		{
		case RuntimeGenericAccess.None:
			return defaultFunc();
		case RuntimeGenericAccess.Method:
		{
			GenericSharingData rgctx2 = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.Resolve());
			int index2 = RetrieveMethodIndex(method, info, rgctx2);
			if (index2 == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
			}
			return retrieveMethodSharedAccess(index2);
		}
		case RuntimeGenericAccess.This:
		case RuntimeGenericAccess.Type:
		{
			GenericSharingData rgctx = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.DeclaringType.Resolve());
			int index = RetrieveMethodIndex(method, info, rgctx);
			if (index == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
			}
			return retrieveTypeSharedAccess(index);
		}
		default:
			throw new ArgumentOutOfRangeException("method");
		}
	}

	private T RetrieveConstrainedMethod<T>(TypeReference constrainedType, MethodReference method, MethodReference enclosingMethod, Func<T> defaultFunc, Func<int, T> retrieveTypeSharedAccess, Func<int, T> retrieveMethodSharedAccess, GenericSharingAnalysisResults genericSharingAnalysisService)
	{
		RuntimeGenericAccess typeAccess = GetRGCTXAccess(constrainedType, enclosingMethod);
		RuntimeGenericAccess methodAccess = GetRGCTXAccess(method, enclosingMethod);
		RuntimeGenericAccess access = RuntimeGenericAccess.None;
		if (typeAccess == RuntimeGenericAccess.Method || methodAccess == RuntimeGenericAccess.Method)
		{
			access = RuntimeGenericAccess.Method;
		}
		else if (typeAccess != 0)
		{
			access = typeAccess;
		}
		else if (methodAccess != 0)
		{
			access = methodAccess;
		}
		switch (access)
		{
		case RuntimeGenericAccess.None:
			return defaultFunc();
		case RuntimeGenericAccess.Method:
		{
			GenericSharingData rgctx2 = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.Resolve());
			int index2 = RetrieveConstrainedMethodIndex(constrainedType, method, rgctx2);
			if (index2 == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
			}
			return retrieveMethodSharedAccess(index2);
		}
		case RuntimeGenericAccess.This:
		case RuntimeGenericAccess.Type:
		{
			GenericSharingData rgctx = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.DeclaringType.Resolve());
			int index = RetrieveConstrainedMethodIndex(constrainedType, method, rgctx);
			if (index == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
			}
			return retrieveTypeSharedAccess(index);
		}
		default:
			throw new ArgumentOutOfRangeException("method");
		}
	}

	private string RetrieveType(TypeReference type, MethodReference enclosingMethod, Func<string> defaultFunc, Func<int, string> retrieveTypeSharedAccess, Func<int, string> retrieveMethodSharedAccess, RuntimeGenericContextInfo info, GenericSharingAnalysisResults genericSharingAnalysisService)
	{
		switch (GetRGCTXAccess(type, enclosingMethod))
		{
		case RuntimeGenericAccess.None:
			return defaultFunc();
		case RuntimeGenericAccess.Method:
		{
			GenericSharingData rgctx2 = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.Resolve());
			int index2 = RetrieveTypeIndex(type, info, rgctx2);
			if (index2 == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(type.FullName));
			}
			return retrieveMethodSharedAccess(index2);
		}
		case RuntimeGenericAccess.This:
		case RuntimeGenericAccess.Type:
		{
			GenericSharingData rgctx = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.DeclaringType.Resolve());
			int index = RetrieveTypeIndex(type, info, rgctx);
			if (index == -1)
			{
				throw new InvalidOperationException(FormatGenericContextErrorMessage(type.FullName));
			}
			return retrieveTypeSharedAccess(index);
		}
		default:
			throw new ArgumentOutOfRangeException("type");
		}
	}

	public RuntimeGenericAccess GetRGCTXAccess(TypeReference type, MethodReference enclosingMethod)
	{
		GenericContextUsage usage = GenericSharingVisitor.GenericUsageFor(type);
		_methodRgctxUsage |= usage;
		switch (usage)
		{
		case GenericContextUsage.None:
			return RuntimeGenericAccess.None;
		case GenericContextUsage.Type:
			if (GenericSharingAnalysis.NeedsTypeContextAsArgument(enclosingMethod))
			{
				return RuntimeGenericAccess.Type;
			}
			return RuntimeGenericAccess.This;
		case GenericContextUsage.Method:
		case GenericContextUsage.Both:
			return RuntimeGenericAccess.Method;
		default:
			throw new ArgumentOutOfRangeException("type");
		}
	}

	private RuntimeGenericAccess GetRGCTXAccess(MethodReference method, MethodReference enclosingMethod)
	{
		GenericContextUsage usage = GenericSharingVisitor.GenericUsageFor(method);
		_methodRgctxUsage |= usage;
		switch (usage)
		{
		case GenericContextUsage.None:
			return RuntimeGenericAccess.None;
		case GenericContextUsage.Type:
			if (GenericSharingAnalysis.NeedsTypeContextAsArgument(enclosingMethod))
			{
				return RuntimeGenericAccess.Type;
			}
			return RuntimeGenericAccess.This;
		case GenericContextUsage.Method:
		case GenericContextUsage.Both:
			return RuntimeGenericAccess.Method;
		default:
			throw new ArgumentOutOfRangeException("method");
		}
	}

	public static int RetrieveTypeIndex(TypeReference type, RuntimeGenericContextInfo info, GenericSharingData rgctx)
	{
		int index = -1;
		for (int i = 0; i < rgctx.RuntimeGenericDatas.Count; i++)
		{
			RuntimeGenericData data = rgctx.RuntimeGenericDatas[i];
			if (data.InfoType == info)
			{
				RuntimeGenericTypeData typeData = (RuntimeGenericTypeData)data;
				if (typeData.GenericType != null && typeData.GenericType == type)
				{
					index = i;
					break;
				}
			}
		}
		return index;
	}

	public static int RetrieveMethodIndex(MethodReference method, RuntimeGenericContextInfo info, GenericSharingData rgctx)
	{
		int index = -1;
		for (int i = 0; i < rgctx.RuntimeGenericDatas.Count; i++)
		{
			RuntimeGenericData data = rgctx.RuntimeGenericDatas[i];
			if (data.InfoType == info)
			{
				RuntimeGenericMethodData methodData = (RuntimeGenericMethodData)data;
				if (methodData.GenericMethod != null && methodData.GenericMethod == method)
				{
					index = i;
					break;
				}
			}
		}
		return index;
	}

	public static int RetrieveConstrainedMethodIndex(TypeReference constrainedType, MethodReference method, GenericSharingData rgctx)
	{
		int index = -1;
		for (int i = 0; i < rgctx.RuntimeGenericDatas.Count; i++)
		{
			RuntimeGenericData data = rgctx.RuntimeGenericDatas[i];
			if (data.InfoType == RuntimeGenericContextInfo.Constrained)
			{
				RuntimeGenericConstrainedCallData constrainedCallData = (RuntimeGenericConstrainedCallData)data;
				if (constrainedCallData.ConstrainedType == constrainedType && constrainedCallData.ConstrainedMethod == method)
				{
					index = i;
					break;
				}
			}
		}
		return index;
	}

	private static string FormatGenericContextErrorMessage(string name)
	{
		return "Unable to retrieve the runtime generic context for '" + name + "'.";
	}
}
