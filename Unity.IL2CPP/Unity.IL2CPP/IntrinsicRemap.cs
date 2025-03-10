using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class IntrinsicRemap
{
	public readonly struct IntrinsicCall
	{
		public readonly string FunctionName;

		public readonly IReadOnlyList<string> Arguments;

		public IntrinsicCall(string functionName, IReadOnlyList<string> arguments)
		{
			FunctionName = functionName;
			Arguments = arguments;
		}

		public IntrinsicCall(string functionName, params string[] arguments)
		{
			FunctionName = functionName;
			Arguments = arguments;
		}
	}

	private class RemappedMethods
	{
		private readonly ReadOnlyDictionary<string, string> _mapping;

		private readonly HashSet<(string, string)> _typesWithIntrinsics = new HashSet<(string, string)>();

		public RemappedMethods(ReadOnlyDictionary<string, string> mapping)
		{
			_mapping = mapping;
			foreach (KeyValuePair<string, string> item in _mapping)
			{
				string key = item.Key;
				int typeEnd = key.IndexOf(':');
				int typeStart = key.LastIndexOf(' ', typeEnd, typeEnd + 1) + 1;
				string text = key.Substring(typeStart, typeEnd - typeStart);
				int namespaceSeparator = text.LastIndexOf('.');
				string typeNameSpace = text.Substring(0, namespaceSeparator);
				string typeName = text.Substring(namespaceSeparator + 1);
				_typesWithIntrinsics.Add((typeNameSpace, typeName));
			}
		}

		public bool TryGetValue(MethodReference method, out string mappedName)
		{
			if (!_typesWithIntrinsics.Contains((method.DeclaringType.Namespace, method.DeclaringType.Name)))
			{
				mappedName = null;
				return false;
			}
			return _mapping.TryGetValue(method.FullName, out mappedName);
		}
	}

	private delegate IntrinsicCall IntrinsicRemapCustomCall(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess metadataAccess);

	private const string GetTypeStringSignature = "System.Type System.Type::GetType(System.String)";

	private const string GetTypeStringSignatureBoolean = "System.Type System.Type::GetType(System.String,System.Boolean)";

	private const string GetTypeStringSignatureBooleanBoolean = "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)";

	private const string GetTypeTargetName = "il2cpp_codegen_get_type";

	private const string GetCurrentMethodSignature = "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()";

	private const string GetIUnknownForObjectSignatureObject = "System.IntPtr System.Runtime.InteropServices.Marshal::GetIUnknownForObject(System.Object)";

	private const string GetExecutingAssemblySignature = "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()";

	private const string VolatileWriteT = "System.Void System.Threading.Volatile::Write(T&,T)";

	private const string ByReferenceConstructor = "System.Void System.ByReference`1::.ctor(T&)";

	private const string ByReferenceGetValue = "T& System.ByReference`1::get_Value()";

	private const string UnsafeAddByteOffset = "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.IntPtr)";

	private const string UnsafeAddByteOffsetUint32 = "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt32)";

	private const string UnsafeAddByteOffsetUint64 = "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt64)";

	private const string UnsafeAddRefInt = "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.Int32)";

	private const string UnsafeAddRefIntPtr = "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.IntPtr)";

	private const string UnsafeAddVoidInt = "System.Void* System.Runtime.CompilerServices.Unsafe::Add(System.Void*,System.Int32)";

	private const string UnsafeAreSame = "System.Boolean System.Runtime.CompilerServices.Unsafe::AreSame(T&,T&)";

	private const string UnsafeAsPointer = "System.Void* System.Runtime.CompilerServices.Unsafe::AsPointer(T&)";

	private const string UnsafeAsRef = "T& System.Runtime.CompilerServices.Unsafe::AsRef(System.Void*)";

	private const string UnsafeAsT = "T System.Runtime.CompilerServices.Unsafe::As(System.Object)";

	private const string UnsafeAsTFromTo = "TTo& System.Runtime.CompilerServices.Unsafe::As(TFrom&)";

	private const string UnsafeByteOffset = "System.IntPtr System.Runtime.CompilerServices.Unsafe::ByteOffset(T&,T&)";

	private const string UnsafeIsAddressGreaterThan = "System.Boolean System.Runtime.CompilerServices.Unsafe::IsAddressGreaterThan(T&,T&)";

	private const string UnsafeIsAddressLessThan = "System.Boolean System.Runtime.CompilerServices.Unsafe::IsAddressLessThan(T&,T&)";

	private const string UnsafeIsNullRef = "System.Boolean System.Runtime.CompilerServices.Unsafe::IsNullRef(T&)";

	private const string UnsafeNullRef = "T& System.Runtime.CompilerServices.Unsafe::NullRef()";

	private const string UnsafeRead = "T System.Runtime.CompilerServices.Unsafe::Read(System.Void*)";

	private const string UnsafeReadUnalignedByteRef = "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Byte&)";

	private const string UnsafeReadUnalignedVoidPtr = "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Void*)";

	private const string UnsafeSizeOf = "System.Int32 System.Runtime.CompilerServices.Unsafe::SizeOf()";

	private const string UnsafeSubtractByteOffset = "T& System.Runtime.CompilerServices.Unsafe::SubtractByteOffset(T&,System.IntPtr)";

	private const string UnsafeSubtractRefInt = "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.Int32)";

	private const string UnsafeSubtractRefIntPtr = "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.IntPtr)";

	private const string UnsafeSubtractVoidInt = "System.Void* System.Runtime.CompilerServices.Unsafe::Subtract(System.Void*,System.Int32)";

	private const string UnsafeUnbox = "T& System.Runtime.CompilerServices.Unsafe::Unbox(System.Object)";

	private const string UnsafeWrite = "System.Void System.Runtime.CompilerServices.Unsafe::Write(System.Void*,T)";

	private const string UnsafeWriteUnalignedByteRef = "System.Void System.Runtime.CompilerServices.Unsafe::WriteUnaligned(System.Byte&,T)";

	private const string UnsafeWriteUnalignedVoidPtr = "System.Void System.Runtime.CompilerServices.Unsafe::WriteUnaligned(System.Void*,T)";

	public const string IsReferenceOrContainsReferences = "System.Boolean System.Runtime.CompilerServices.RuntimeHelpers::IsReferenceOrContainsReferences()";

	public const string UnsafeUtilityIsUnmanaged = "System.Boolean Unity.Collections.LowLevel.Unsafe.UnsafeUtility::IsUnmanaged()";

	private const string SpanGetItem = "T& System.Span`1::get_Item(System.Int32)";

	private const string ReadOnlySpanGetItem = "T& modreq(System.Runtime.InteropServices.InAttribute) System.ReadOnlySpan`1::get_Item(System.Int32)";

	private static readonly RemappedMethods _remappedMethods = new RemappedMethods(new Dictionary<string, string>
	{
		{ "System.Double System.Math::Asin(System.Double)", "asin" },
		{ "System.Double System.Math::Cosh(System.Double)", "cosh" },
		{ "System.Double System.Math::Abs(System.Double)", "fabs" },
		{ "System.Single System.Math::Abs(System.Single)", "fabsf" },
		{ "System.Double System.Math::Log(System.Double)", "log" },
		{ "System.Double System.Math::Tan(System.Double)", "tan" },
		{ "System.Double System.Math::Exp(System.Double)", "exp" },
		{ "System.Int64 System.Math::Abs(System.Int64)", "il2cpp_codegen_abs" },
		{ "System.Double System.Math::Ceiling(System.Double)", "ceil" },
		{ "System.Double System.Math::Atan(System.Double)", "atan" },
		{ "System.Double System.Math::Tanh(System.Double)", "tanh" },
		{ "System.Double System.Math::Sqrt(System.Double)", "sqrt" },
		{ "System.Double System.Math::Log10(System.Double)", "log10" },
		{ "System.Double System.Math::Sinh(System.Double)", "sinh" },
		{ "System.Double System.Math::Cos(System.Double)", "cos" },
		{ "System.Double System.Math::Atan2(System.Double,System.Double)", "atan2" },
		{ "System.Int32 System.Math::Abs(System.Int32)", "il2cpp_codegen_abs" },
		{ "System.Double System.Math::Sin(System.Double)", "sin" },
		{ "System.Double System.Math::Acos(System.Double)", "acos" },
		{ "System.Double System.Math::Floor(System.Double)", "floor" },
		{ "System.Double System.Math::Round(System.Double)", "bankers_round" },
		{ "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()", "il2cpp_codegen_get_method_object" },
		{ "System.Type System.Type::GetType(System.String)", "il2cpp_codegen_get_type" },
		{ "System.Type System.Type::GetType(System.String,System.Boolean)", "il2cpp_codegen_get_type" },
		{ "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)", "il2cpp_codegen_get_type" },
		{ "System.IntPtr System.Runtime.InteropServices.Marshal::GetIUnknownForObject(System.Object)", "il2cpp_codegen_com_get_iunknown_for_object" },
		{ "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()", "il2cpp_codegen_get_executing_assembly" },
		{ "System.Void System.Threading.Volatile::Write(System.Byte&,System.Byte)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Boolean&,System.Boolean)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Double&,System.Double)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Int16&,System.Int16)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Int32&,System.Int32)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Int64&,System.Int64)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.IntPtr&,System.IntPtr)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.SByte&,System.SByte)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.Single&,System.Single)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.UInt16&,System.UInt16)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.UInt32&,System.UInt32)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.UInt64&,System.UInt64)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(System.UIntPtr&,System.UIntPtr)", "VolatileWrite" },
		{ "System.Void System.Threading.Volatile::Write(T&,T)", "VolatileWrite" },
		{ "System.Byte System.Threading.Volatile::Read(System.Byte&)", "VolatileRead" },
		{ "System.Boolean System.Threading.Volatile::Read(System.Boolean&)", "VolatileRead" },
		{ "System.Double System.Threading.Volatile::Read(System.Double&)", "VolatileRead" },
		{ "System.Int16 System.Threading.Volatile::Read(System.Int16&)", "VolatileRead" },
		{ "System.Int32 System.Threading.Volatile::Read(System.Int32&)", "VolatileRead" },
		{ "System.Int64 System.Threading.Volatile::Read(System.Int64&)", "VolatileRead" },
		{ "System.IntPtr System.Threading.Volatile::Read(System.IntPtr&)", "VolatileRead" },
		{ "System.SByte System.Threading.Volatile::Read(System.SByte&)", "VolatileRead" },
		{ "System.Single System.Threading.Volatile::Read(System.Single&)", "VolatileRead" },
		{ "System.UInt16 System.Threading.Volatile::Read(System.UInt16&)", "VolatileRead" },
		{ "System.UInt32 System.Threading.Volatile::Read(System.UInt32&)", "VolatileRead" },
		{ "System.UInt64 System.Threading.Volatile::Read(System.UInt64&)", "VolatileRead" },
		{ "System.UIntPtr System.Threading.Volatile::Read(System.UIntPtr&)", "VolatileRead" },
		{ "T System.Threading.Volatile::Read(T&)", "VolatileRead" },
		{ "System.Boolean System.Platform::get_IsMacOS()", "il2cpp_codegen_platform_is_osx_or_ios" },
		{ "System.Boolean System.Platform::get_IsFreeBSD()", "il2cpp_codegen_platform_is_freebsd" },
		{ "System.Boolean System.AppDomain::IsAppXModel()", "il2cpp_codegen_platform_is_uwp" },
		{ "System.Boolean System.Net.NetworkInformation.UnixIPGlobalPropertiesFactoryPal::get_PlatformNeedsLibCWorkaround()", "il2cpp_codegen_platform_disable_libc_pinvoke" },
		{ "System.Boolean System.Runtime.CompilerServices.RuntimeHelpers::IsReferenceOrContainsReferences()", "il2cpp_codegen_is_reference_or_contains_references" },
		{ "System.Single UnityEngine.Mathf::Sin(System.Single)", "sinf" },
		{ "System.Single UnityEngine.Mathf::Cos(System.Single)", "cosf" },
		{ "System.Single UnityEngine.Mathf::Tan(System.Single)", "tanf" },
		{ "System.Single UnityEngine.Mathf::Asin(System.Single)", "asinf" },
		{ "System.Single UnityEngine.Mathf::Acos(System.Single)", "acosf" },
		{ "System.Single UnityEngine.Mathf::Atan(System.Single)", "atanf" },
		{ "System.Single UnityEngine.Mathf::Atan2(System.Single,System.Single)", "atan2f" },
		{ "System.Single UnityEngine.Mathf::Sqrt(System.Single)", "sqrtf" },
		{ "System.Single UnityEngine.Mathf::Abs(System.Single)", "fabsf" },
		{ "System.Single UnityEngine.Mathf::Pow(System.Single,System.Single)", "powf" },
		{ "System.Single UnityEngine.Mathf::Exp(System.Single)", "expf" },
		{ "System.Single UnityEngine.Mathf::Log(System.Single)", "logf" },
		{ "System.Single UnityEngine.Mathf::Log10(System.Single)", "log10f" },
		{ "System.Single UnityEngine.Mathf::Ceil(System.Single)", "ceilf" },
		{ "System.Single UnityEngine.Mathf::Floor(System.Single)", "floorf" },
		{ "System.Single UnityEngine.Mathf::Round(System.Single)", "bankers_roundf" },
		{ "System.Boolean Unity.Collections.LowLevel.Unsafe.UnsafeUtility::IsUnmanaged()", "il2cpp_codegen_is_unmanaged" },
		{ "System.Void System.Diagnostics.Debugger::Break()", "IL2CPP_DEBUG_BREAK" },
		{ "System.IntPtr System.Runtime.InteropServices.Marshal::GetComInterfaceForObject(System.Object,System.Type)", "il2cpp_codegen_get_com_interface_for_object" },
		{ "T Unity.Collections.NativeArray`1::get_Item(System.Int32)", "IL2CPP_NATIVEARRAY_GET_ITEM" },
		{ "System.Void Unity.Collections.NativeArray`1::set_Item(System.Int32,T)", "IL2CPP_NATIVEARRAY_SET_ITEM" },
		{ "System.Int32 Unity.Collections.NativeArray`1::get_Length()", "IL2CPP_NATIVEARRAY_GET_LENGTH" },
		{ "System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility::AddressOf(T&)", "il2cpp_codegen_unsafe_cast" },
		{ "System.Boolean System.Object::ReferenceEquals(System.Object,System.Object)", "il2cpp_codegen_object_reference_equals" },
		{ "T System.Array::UnsafeLoad(T[],System.Int32)", "IL2CPP_ARRAY_UNSAFE_LOAD" },
		{ "System.Void Unity.ThrowStub::ThrowNotSupportedException()", "il2cpp_codegen_raise_profile_exception" },
		{ "System.Void GBenchmarkApp.GBenchmark::RunSpecifiedBenchmarks()", "benchmark::RunSpecifiedBenchmarks" },
		{ "System.Void System.ByReference`1::.ctor(T&)", "il2cpp_codegen_by_reference_constructor" },
		{ "T& System.ByReference`1::get_Value()", "IL2CPP_BY_REFERENCE_GET_VALUE" },
		{ "System.Single System.MathF::Asin(System.Single)", "asinf" },
		{ "System.Single System.MathF::Cosh(System.Single)", "coshf" },
		{ "System.Single System.MathF::Abs(System.Single)", "fabsf" },
		{ "System.Single System.MathF::Log(System.Single)", "logf" },
		{ "System.Single System.MathF::Tan(System.Single)", "tanf" },
		{ "System.Single System.MathF::Exp(System.Single)", "expf" },
		{ "System.Single System.MathF::Ceiling(System.Single)", "ceilf" },
		{ "System.Single System.MathF::Atan(System.Single)", "atanf" },
		{ "System.Single System.MathF::Tanh(System.Single)", "tanhf" },
		{ "System.Single System.MathF::Sqrt(System.Single)", "sqrtf" },
		{ "System.Single System.MathF::Log10(System.Single)", "log10f" },
		{ "System.Single System.MathF::Sinh(System.Single)", "sinhf" },
		{ "System.Single System.MathF::Cos(System.Single)", "cosf" },
		{ "System.Single System.MathF::Atan2(System.Single,System.Single)", "atan2f" },
		{ "System.Single System.MathF::Sin(System.Single)", "sinf" },
		{ "System.Single System.MathF::Acos(System.Single)", "acosf" },
		{ "System.Single System.MathF::Floor(System.Single)", "floorf" },
		{ "System.Single System.MathF::ModF(System.Single,System.Single*)", "modff" },
		{ "System.Single System.MathF::FMod(System.Single,System.Single)", "fmodf" },
		{ "System.Single System.MathF::Round(System.Single)", "bankers_roundf" },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.IntPtr)", "il2cpp_unsafe_add_byte_offset" },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt32)", "il2cpp_unsafe_add_byte_offset" },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt64)", "il2cpp_unsafe_add_byte_offset" },
		{ "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.Int32)", "il2cpp_unsafe_add" },
		{ "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.IntPtr)", "il2cpp_unsafe_add" },
		{ "System.Void* System.Runtime.CompilerServices.Unsafe::Add(System.Void*,System.Int32)", "il2cpp_unsafe_add" },
		{ "System.Boolean System.Runtime.CompilerServices.Unsafe::AreSame(T&,T&)", "il2cpp_unsafe_are_same" },
		{ "System.Void* System.Runtime.CompilerServices.Unsafe::AsPointer(T&)", "il2cpp_unsafe_as_pointer" },
		{ "T& System.Runtime.CompilerServices.Unsafe::AsRef(System.Void*)", "il2cpp_unsafe_as_ref" },
		{ "T System.Runtime.CompilerServices.Unsafe::As(System.Object)", "il2cpp_unsafe_as" },
		{ "TTo& System.Runtime.CompilerServices.Unsafe::As(TFrom&)", "il2cpp_unsafe_as_ref" },
		{ "System.IntPtr System.Runtime.CompilerServices.Unsafe::ByteOffset(T&,T&)", "il2cpp_unsafe_byte_offset" },
		{ "System.Boolean System.Runtime.CompilerServices.Unsafe::IsAddressGreaterThan(T&,T&)", "il2cpp_unsafe_is_addr_gt" },
		{ "System.Boolean System.Runtime.CompilerServices.Unsafe::IsAddressLessThan(T&,T&)", "il2cpp_unsafe_is_addr_lt" },
		{ "System.Boolean System.Runtime.CompilerServices.Unsafe::IsNullRef(T&)", "il2cpp_unsafe_is_null_ref" },
		{ "T& System.Runtime.CompilerServices.Unsafe::NullRef()", "il2cpp_unsafe_null_ref" },
		{ "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Byte&)", "il2cpp_unsafe_read_unaligned" },
		{ "T System.Runtime.CompilerServices.Unsafe::Read(System.Void*)", "il2cpp_unsafe_read" },
		{ "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Void*)", "il2cpp_unsafe_read_unaligned" },
		{ "System.Int32 System.Runtime.CompilerServices.Unsafe::SizeOf()", "il2cpp_unsafe_sizeof" },
		{ "T& System.Runtime.CompilerServices.Unsafe::SubtractByteOffset(T&,System.IntPtr)", "il2cpp_unsafe_subtract_byte_offset" },
		{ "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.Int32)", "il2cpp_unsafe_subtract" },
		{ "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.IntPtr)", "il2cpp_unsafe_subtract" },
		{ "System.Void* System.Runtime.CompilerServices.Unsafe::Subtract(System.Void*,System.Int32)", "il2cpp_unsafe_subtract" },
		{ "T& System.Runtime.CompilerServices.Unsafe::Unbox(System.Object)", "il2cpp_unsafe_unbox" },
		{ "System.Void System.Runtime.CompilerServices.Unsafe::Write(System.Void*,T)", "il2cpp_unsafe_write" },
		{ "System.Void System.Runtime.CompilerServices.Unsafe::WriteUnaligned(System.Byte&,T)", "il2cpp_unsafe_write_unaligned" },
		{ "System.Void System.Runtime.CompilerServices.Unsafe::WriteUnaligned(System.Void*,T)", "il2cpp_unsafe_write_unaligned" },
		{ "T& System.Span`1::get_Item(System.Int32)", "il2cpp_span_get_item" },
		{ "T& modreq(System.Runtime.InteropServices.InAttribute) System.ReadOnlySpan`1::get_Item(System.Int32)", "il2cpp_span_get_item" },
		{ "System.Void System.Span`1::.ctor(System.Void*,System.Int32)", "il2cpp_array_as_span" },
		{ "System.Void System.Span`1::.ctor(T[])", "il2cpp_array_as_span" },
		{ "System.Void System.Span`1::.ctor(T[],System.Int32,System.Int32)", "il2cpp_array_as_span" },
	}.AsReadOnly());

	private static readonly ReadOnlyDictionary<string, IntrinsicRemapCustomCall> MethodNameMappingCustomCall = new Dictionary<string, IntrinsicRemapCustomCall>
	{
		{ "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()", GetCallingMethodMetadata },
		{ "System.Type System.Type::GetType(System.String)", GetTypeRemappingCustomArguments },
		{ "System.Type System.Type::GetType(System.String,System.Boolean)", GetTypeRemappingCustomArguments },
		{ "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)", GetTypeRemappingCustomArguments },
		{ "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()", GetCallingMethodMetadata },
		{ "T Unity.Collections.NativeArray`1::get_Item(System.Int32)", NativeArrayGetItemRemappedArguments },
		{ "System.Void Unity.Collections.NativeArray`1::set_Item(System.Int32,T)", NativeArraySetItemRemappedArguments },
		{ "System.Int32 Unity.Collections.NativeArray`1::get_Length()", NativeArrayGetLengthRemappedArguments },
		{ "System.Void Unity.ThrowStub::ThrowNotSupportedException()", GetCallingMethodMetadata },
		{ "System.Void System.ByReference`1::.ctor(T&)", GetByReferenceConstructorArguments },
		{ "T& System.ByReference`1::get_Value()", GetByReferenceGetValueArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.IntPtr)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt32)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::AddByteOffset(T&,System.UInt64)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.Int32)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::Add(T&,System.IntPtr)", GetUnsafeAddSubtractArguments },
		{ "System.Void* System.Runtime.CompilerServices.Unsafe::Add(System.Void*,System.Int32)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::AsRef(System.Void*)", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "T System.Runtime.CompilerServices.Unsafe::As(System.Object)", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "TTo& System.Runtime.CompilerServices.Unsafe::As(TFrom&)", GetUnsafeAsTFromTToArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::NullRef()", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "T System.Runtime.CompilerServices.Unsafe::Read(System.Void*)", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Byte&)", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "T System.Runtime.CompilerServices.Unsafe::ReadUnaligned(System.Void*)", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "System.Int32 System.Runtime.CompilerServices.Unsafe::SizeOf()", GetOneArgumentGenericMethodAsTemplatedCallArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::SubtractByteOffset(T&,System.IntPtr)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.Int32)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::Subtract(T&,System.IntPtr)", GetUnsafeAddSubtractArguments },
		{ "System.Void* System.Runtime.CompilerServices.Unsafe::Subtract(System.Void*,System.Int32)", GetUnsafeAddSubtractArguments },
		{ "T& System.Runtime.CompilerServices.Unsafe::Unbox(System.Object)", GetUnsafeUnboxArguments },
		{ "T& System.Span`1::get_Item(System.Int32)", SpanGetItemArguments },
		{ "T& modreq(System.Runtime.InteropServices.InAttribute) System.ReadOnlySpan`1::get_Item(System.Int32)", SpanGetItemArguments },
		{ "System.Void System.Threading.Volatile::Write(T&,T)", VolatileWriteTArguments }
	}.AsReadOnly();

	private static readonly ReadOnlyDictionary<string, Func<MethodReference, bool>> MethodNameImplementationCanBeRemappedMapping = new Dictionary<string, Func<MethodReference, bool>>
	{
		{ "T& System.Span`1::get_Item(System.Int32)", SpanGetItemImplementationCanBeRemapped },
		{ "T& modreq(System.Runtime.InteropServices.InAttribute) System.ReadOnlySpan`1::get_Item(System.Int32)", SpanGetItemImplementationCanBeRemapped }
	}.AsReadOnly();

	private static readonly ReadOnlyHashSet<string> IntrinsicMethodsForFullGenericSharing = new ReadOnlyHashSet<string>(new HashSet<string> { "System.Void System.ByReference`1::.ctor(T&)", "T& System.ByReference`1::get_Value()", "System.Boolean System.Runtime.CompilerServices.RuntimeHelpers::IsReferenceOrContainsReferences()", "System.Boolean Unity.Collections.LowLevel.Unsafe.UnsafeUtility::IsUnmanaged()", "T System.Runtime.CompilerServices.Unsafe::As(System.Object)", "TTo& System.Runtime.CompilerServices.Unsafe::As(TFrom&)", "System.Void* System.Runtime.CompilerServices.Unsafe::AsPointer(T&)", "T& System.Runtime.CompilerServices.Unsafe::AsRef(System.Void*)" });

	private static RemappedMethods GetMethodNameMapping(ReadOnlyContext context)
	{
		return _remappedMethods;
	}

	public static bool ShouldRemap(ReadOnlyContext context, MethodReference methodToCall, bool fullGenericSharing)
	{
		if (fullGenericSharing && !IsGenericThatShouldBeMappedForFullGenericSharing(methodToCall.Resolve()))
		{
			if (methodToCall is GenericInstanceMethod genericInstanceMethod && genericInstanceMethod.GenericArguments.Any((TypeReference a) => a.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable))
			{
				return false;
			}
			if (methodToCall.GetResolvedThisType(context) is GenericInstanceType genericInstanceType && genericInstanceType.GenericArguments.Any((TypeReference a) => a.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable))
			{
				return false;
			}
			if (methodToCall.ContainsGenericParameter)
			{
				return false;
			}
		}
		MethodReference methodDefinition = GetMethodDefinition(methodToCall);
		string mappedName;
		bool num = GetMethodNameMapping(context).TryGetValue(methodDefinition, out mappedName);
		bool implementationCanBeRemapped = ImplementationCanBeRemapped(methodDefinition);
		return num && implementationCanBeRemapped;
	}

	public static bool StillNeedsHiddenMethodInfo(ReadOnlyContext context, MethodReference methodToCall, bool forFullGenericSharing)
	{
		MethodReference methodDefinition = GetMethodDefinition(methodToCall);
		if (GetMethodNameMapping(context).TryGetValue(methodDefinition, out var target))
		{
			if (target == "il2cpp_codegen_get_type")
			{
				return true;
			}
			if (forFullGenericSharing)
			{
				if (methodDefinition.FullName == "System.Boolean System.Runtime.CompilerServices.RuntimeHelpers::IsReferenceOrContainsReferences()")
				{
					return true;
				}
				if (methodDefinition.FullName == "System.Boolean Unity.Collections.LowLevel.Unsafe.UnsafeUtility::IsUnmanaged()")
				{
					return true;
				}
			}
		}
		return false;
	}

	private static MethodReference GetMethodDefinition(MethodReference methodToCall)
	{
		return methodToCall.Resolve() ?? methodToCall;
	}

	public static IntrinsicCall GetMappedCallFor(IGeneratedMethodCodeWriter writer, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata, IReadOnlyList<string> arguments)
	{
		MethodReference methodDefinition = GetMethodDefinition(methodToCall);
		if (GetMethodNameMapping(writer.Context).TryGetValue(methodDefinition, out var name))
		{
			IntrinsicCall call = new IntrinsicCall(name, arguments);
			if (MethodNameMappingCustomCall.TryGetValue(methodDefinition.FullName, out var customizeIntrinsicCall))
			{
				return customizeIntrinsicCall(writer, call, methodToCall, callingMethod, runtimeMetadata);
			}
			return call;
		}
		throw new KeyNotFoundException($"Could not find an intrinsic method to remap for '{methodToCall}'");
	}

	private static bool ImplementationCanBeRemapped(MethodReference methodToCall)
	{
		MethodReference methodDefinition = GetMethodDefinition(methodToCall);
		if (MethodNameImplementationCanBeRemappedMapping.TryGetValue(methodDefinition.FullName, out var implementationCanBeRemapped))
		{
			return implementationCanBeRemapped(methodToCall);
		}
		return true;
	}

	private static IntrinsicCall GetCallingMethodMetadata(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, runtimeMetadata.MethodInfo(callingMethod));
	}

	private static IntrinsicCall GetByReferenceGetValueArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, GetGenericArgument(writer, methodToCall.DeclaringType, 0).CppNameForVariable, "(Il2CppByReference*)" + call.Arguments[0]);
	}

	private static IntrinsicCall GetOneArgumentGenericMethodAsTemplatedCallArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(MakeTemplate(call.FunctionName, GetGenericArgument(writer, methodToCall, 0).CppNameForVariable), call.Arguments);
	}

	private static IntrinsicCall GetUnsafeAddSubtractArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		string templateArgs = GetGenericArgument(writer, methodToCall, 0).CppNameForVariable + "," + methodToCall.Parameters[1].ParameterType.CppNameForVariable;
		return new IntrinsicCall(MakeTemplate(call.FunctionName, templateArgs), call.Arguments);
	}

	private static IntrinsicCall GetUnsafeUnboxArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(MakeTemplate(call.FunctionName, GetGenericArgument(writer, methodToCall, 0).CppNameForVariable), call.Arguments[0], runtimeMetadata.TypeInfoFor(GetGenericArgument(writer, methodToCall, 0)));
	}

	private static IntrinsicCall GetUnsafeAsTFromTToArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(MakeTemplate(call.FunctionName, GetGenericArgument(writer, methodToCall, 1).CppNameForVariable), call.Arguments);
	}

	private static IntrinsicCall GetByReferenceConstructorArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, "(Il2CppByReference*)" + call.Arguments[0], call.Arguments[1]);
	}

	private static IntrinsicCall GetTypeRemappingCustomArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		List<string> newArguments = call.Arguments.ToList();
		newArguments.Add(runtimeMetadata.MethodInfo(callingMethod));
		return new IntrinsicCall(call.FunctionName, newArguments);
	}

	private static IntrinsicCall NativeArrayGetItemRemappedArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, ((GenericInstanceType)methodToCall.DeclaringType).GenericArguments[0].CppNameForVariable, "(" + call.Arguments[0] + ")->" + methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Buffer").CppName, call.Arguments[1]);
	}

	private static IntrinsicCall NativeArraySetItemRemappedArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, ((GenericInstanceType)methodToCall.DeclaringType).GenericArguments[0].CppNameForVariable, "(" + call.Arguments[0] + ")->" + methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Buffer").CppName, call.Arguments[1], "(" + call.Arguments[2] + ")");
	}

	private static IntrinsicCall NativeArrayGetLengthRemappedArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, "(" + call.Arguments[0] + ")->" + methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Length").CppName);
	}

	private static IntrinsicCall SpanGetItemArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		return new IntrinsicCall(call.FunctionName, $"({((GenericInstanceType)methodToCall.DeclaringType).GenericArguments[0].CppNameForVariable}*)((Il2CppByReference*)&(({call.Arguments[0]})->{methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "_pointer").CppName}))->value", "(" + call.Arguments[1] + ")", "(" + call.Arguments[0] + ")->" + methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "_length").CppName);
	}

	private static IntrinsicCall VolatileWriteTArguments(IGeneratedMethodCodeWriter writer, IntrinsicCall call, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata)
	{
		TypeReference argumentType = ((GenericInstanceMethod)methodToCall).GenericArguments[0];
		return new IntrinsicCall(call.FunctionName, Emit.CastToPointer(writer.Context, argumentType, call.Arguments[0]), Emit.Cast(writer.Context, argumentType, call.Arguments[1]));
	}

	private static string MakeTemplate(string name, string templateArgs)
	{
		return name + "<" + templateArgs + ">";
	}

	private static TypeReference GetGenericArgument(IGeneratedMethodCodeWriter writer, MemberReference reference, int number)
	{
		TypeReference genericArgument = ((IGenericInstance)reference).GenericArguments[number];
		writer.AddIncludeForTypeDefinition(writer.Context, genericArgument);
		return genericArgument;
	}

	private static bool SpanGetItemImplementationCanBeRemapped(MethodReference methodToCall)
	{
		ReadOnlyCollection<FieldDefinition> fieldsToDeclaringType = methodToCall.DeclaringType.Resolve().Fields;
		if (fieldsToDeclaringType.Any((FieldDefinition f) => f.Name == "_length"))
		{
			return fieldsToDeclaringType.Any((FieldDefinition f) => f.Name == "_pointer");
		}
		return false;
	}

	private static bool IsGenericThatShouldBeMappedForFullGenericSharing(MethodDefinition method)
	{
		if (method == null)
		{
			return false;
		}
		if (method.IsGenericInstance && method.DeclaringType.IsGenericInstance)
		{
			return false;
		}
		return IntrinsicMethodsForFullGenericSharing.Contains(method.FullName);
	}
}
