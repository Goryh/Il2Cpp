using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class PrimitiveMarshalInfoWriter : DefaultMarshalInfoWriter
{
	private readonly int _nativeSizeWithoutPointers;

	private readonly string _nativeSize;

	private readonly string _customMarshaledTypeName;

	private string MarshaledTypeName => _customMarshaledTypeName ?? _typeRef.CppNameForVariable;

	public override int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		return _nativeSizeWithoutPointers;
	}

	public override string GetNativeSize(ReadOnlyContext context)
	{
		return _nativeSize;
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(MarshaledTypeName)
		};
	}

	public PrimitiveMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalInfo marshalInfo, MarshalType marshalType, bool useUnicodeCharSet = false)
		: base(type)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
			if (marshalType != MarshalType.WindowsRuntime)
			{
				_nativeSizeWithoutPointers = 4;
				_nativeSize = "4";
				_customMarshaledTypeName = "int32_t";
			}
			else
			{
				_nativeSizeWithoutPointers = 1;
				_nativeSize = "1";
				_customMarshaledTypeName = "bool";
			}
			break;
		case MetadataType.Char:
			if (marshalType == MarshalType.WindowsRuntime || useUnicodeCharSet)
			{
				_nativeSizeWithoutPointers = 2;
				_nativeSize = "2";
				_customMarshaledTypeName = "Il2CppChar";
			}
			else
			{
				_nativeSizeWithoutPointers = 1;
				_nativeSize = "1";
				_customMarshaledTypeName = "uint8_t";
			}
			break;
		case MetadataType.Void:
			_nativeSizeWithoutPointers = 1;
			_nativeSize = "1";
			break;
		case MetadataType.SByte:
		case MetadataType.Byte:
			_nativeSizeWithoutPointers = 1;
			break;
		case MetadataType.Int16:
		case MetadataType.UInt16:
			_nativeSizeWithoutPointers = 2;
			break;
		case MetadataType.Int32:
		case MetadataType.UInt32:
		case MetadataType.Single:
			_nativeSizeWithoutPointers = 4;
			break;
		case MetadataType.Int64:
		case MetadataType.UInt64:
		case MetadataType.Double:
			_nativeSizeWithoutPointers = 8;
			break;
		case MetadataType.IntPtr:
			_customMarshaledTypeName = "intptr_t";
			_nativeSizeWithoutPointers = 0;
			break;
		case MetadataType.UIntPtr:
			_customMarshaledTypeName = "uintptr_t";
			_nativeSizeWithoutPointers = 0;
			break;
		case MetadataType.Pointer:
			_nativeSizeWithoutPointers = 0;
			break;
		}
		if (marshalInfo != null)
		{
			switch (marshalInfo.NativeType)
			{
			case NativeType.Boolean:
			case NativeType.I4:
				_nativeSize = "4";
				_nativeSizeWithoutPointers = 4;
				_customMarshaledTypeName = "int32_t";
				break;
			case NativeType.I1:
				_nativeSize = "1";
				_nativeSizeWithoutPointers = 1;
				_customMarshaledTypeName = "int8_t";
				break;
			case NativeType.I2:
				_nativeSize = "2";
				_nativeSizeWithoutPointers = 2;
				_customMarshaledTypeName = "int16_t";
				break;
			case NativeType.I8:
				_nativeSize = "8";
				_nativeSizeWithoutPointers = 8;
				_customMarshaledTypeName = "int64_t";
				break;
			case NativeType.U1:
				_nativeSize = "1";
				_nativeSizeWithoutPointers = 1;
				_customMarshaledTypeName = "uint8_t";
				break;
			case NativeType.VariantBool:
				_nativeSize = "2";
				_nativeSizeWithoutPointers = 2;
				_customMarshaledTypeName = "IL2CPP_VARIANT_BOOL";
				break;
			case NativeType.U2:
				_nativeSize = "2";
				_nativeSizeWithoutPointers = 2;
				_customMarshaledTypeName = "uint16_t";
				break;
			case NativeType.U4:
				_nativeSize = "4";
				_nativeSizeWithoutPointers = 4;
				_customMarshaledTypeName = "uint32_t";
				break;
			case NativeType.U8:
				_nativeSize = "8";
				_nativeSizeWithoutPointers = 8;
				_customMarshaledTypeName = "uint64_t";
				break;
			case NativeType.R4:
				_nativeSize = "4";
				_nativeSizeWithoutPointers = 4;
				_customMarshaledTypeName = "float";
				break;
			case NativeType.R8:
				_nativeSize = "8";
				_nativeSizeWithoutPointers = 8;
				_customMarshaledTypeName = "double";
				break;
			case NativeType.Int:
				_nativeSize = "sizeof(void*)";
				_nativeSizeWithoutPointers = 0;
				_customMarshaledTypeName = "intptr_t";
				break;
			case NativeType.UInt:
				_nativeSize = "sizeof(void*)";
				_nativeSizeWithoutPointers = 0;
				_customMarshaledTypeName = "uintptr_t";
				break;
			}
		}
		if (_nativeSize == null)
		{
			_nativeSize = base.GetNativeSize(context);
		}
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		if (_typeRef.IsPointer && _typeRef.GetElementType().MetadataType == MetadataType.ValueType && !_typeRef.GetElementType().IsEnum)
		{
			TypeReference forwardType = GeneratedCodeWriterExtensions.GetForwardDeclarationType(_typeRef);
			writer.AddForwardDeclaration("struct " + forwardType.CppNameForVariable);
		}
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{destinationVariable} = {WriteMarshalVariableToNative(writer, sourceVariable, managedVariableName, metadataAccess)};");
	}

	public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (_typeRef.MetadataType == MetadataType.Boolean && MarshaledTypeName == "IL2CPP_VARIANT_BOOL")
		{
			return MarshalVariantBoolToNative(sourceVariable.Load(writer.Context));
		}
		if (_typeRef.CppNameForVariable != MarshaledTypeName)
		{
			return $"static_cast<{MarshaledTypeName}>({sourceVariable.Load(writer.Context)})";
		}
		return sourceVariable.Load(writer.Context);
	}

	public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		if (_typeRef.MetadataType == MetadataType.Boolean && MarshaledTypeName == "IL2CPP_VARIANT_BOOL")
		{
			return MarshalVariantBoolFromNative(variableName);
		}
		string managedTypeName = _typeRef.CppNameForVariable;
		if (managedTypeName != MarshaledTypeName)
		{
			return $"static_cast<{managedTypeName}>({variableName})";
		}
		return variableName;
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess));
	}

	private static string MarshalVariantBoolToNative(string variableName)
	{
		return "((" + variableName + ") ? IL2CPP_VARIANT_TRUE : IL2CPP_VARIANT_FALSE)";
	}

	private static string MarshalVariantBoolFromNative(string variableName)
	{
		return "((" + variableName + ") != IL2CPP_VARIANT_FALSE)";
	}

	public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
	{
		if (_typeRef.IsPointer)
		{
			base.WriteNativeVariableDeclarationOfType(writer, variableName);
			return;
		}
		string initializer = "0";
		switch (MarshaledTypeName)
		{
		case "float":
			initializer = "0.0f";
			break;
		case "double":
			initializer = "0.0";
			break;
		case "IL2CPP_VARIANT_BOOL":
			initializer = "IL2CPP_VARIANT_FALSE";
			break;
		}
		writer.WriteLine($"{MarshaledTypeName} {variableName} = {initializer};");
	}
}
