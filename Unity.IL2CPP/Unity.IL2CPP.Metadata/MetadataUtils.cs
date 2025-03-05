using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata;

public class MetadataUtils
{
	public const byte ArrayTypeWithSameElements = 0;

	public const byte ArrayTypeWithDifferentElements = 1;

	public static string RegistrationTableName(ReadOnlyContext context)
	{
		return context.Global.Services.ContextScope.ForMetadataGlobalVar("g_MetadataRegistration");
	}

	internal static string TypeRepositoryTypeFor(SourceWritingContext context, IIl2CppRuntimeType type)
	{
		return Emit.AddressOf(context.Global.Services.Naming.ForIl2CppType(context, type));
	}

	public static TypeReference GetUnderlyingType(TypeReference type)
	{
		if (type.IsEnum)
		{
			return type.GetUnderlyingEnumType();
		}
		return type;
	}

	private static byte[] GetBytesForConstantValue(object constantValueToSerialize, TypeReference declaredParameterOrFieldType, string name)
	{
		switch (declaredParameterOrFieldType.WithoutModifiers().MetadataType)
		{
		case MetadataType.Boolean:
			return new byte[1] { ((bool)constantValueToSerialize) ? ((byte)1) : ((byte)0) };
		case MetadataType.Char:
			return BitConverter.GetBytes((ushort)(char)constantValueToSerialize);
		case MetadataType.SByte:
			return new byte[1] { (byte)(sbyte)constantValueToSerialize };
		case MetadataType.Byte:
			return new byte[1] { (byte)constantValueToSerialize };
		case MetadataType.Int16:
			return BitConverter.GetBytes((short)constantValueToSerialize);
		case MetadataType.UInt16:
			return BitConverter.GetBytes((ushort)constantValueToSerialize);
		case MetadataType.Int32:
			return GetCompressedInt32((int)constantValueToSerialize);
		case MetadataType.UInt32:
			return GetCompressedUInt32((uint)constantValueToSerialize);
		case MetadataType.Int64:
			return BitConverter.GetBytes((long)constantValueToSerialize);
		case MetadataType.UInt64:
			return BitConverter.GetBytes((ulong)constantValueToSerialize);
		case MetadataType.IntPtr:
			return BitConverter.GetBytes(Convert.ToInt64(constantValueToSerialize));
		case MetadataType.UIntPtr:
			return BitConverter.GetBytes(Convert.ToUInt64(constantValueToSerialize));
		case MetadataType.Single:
			return BitConverter.GetBytes((float)constantValueToSerialize);
		case MetadataType.Double:
			return BitConverter.GetBytes((double)constantValueToSerialize);
		case MetadataType.Array:
		{
			if (constantValueToSerialize == null)
			{
				return GetCompressedInt32(-1);
			}
			ArrayType arrayType = (ArrayType)declaredParameterOrFieldType;
			if (!arrayType.IsVector)
			{
				throw new InvalidOperationException("Default value for " + name + " must be null.");
			}
			if (!arrayType.ElementType.IsPrimitive && !arrayType.ElementType.IsString)
			{
				throw new InvalidOperationException($"Cannot serialize arrays of {arrayType}");
			}
			Array arr = (Array)constantValueToSerialize;
			Il2CppTypeEnum elementType = Il2CppTypeSupport.ValueFor(arrayType.ElementType, useIl2CppExtensions: true);
			List<byte> arrayData = new List<byte>(6 + arr.Length * IntPtr.Size);
			arrayData.AddRange(GetCompressedInt32(arr.Length));
			arrayData.Add((byte)elementType);
			arrayData.Add(0);
			for (int i = 0; i < arr.Length; i++)
			{
				arrayData.AddRange(GetBytesForConstantValue(arr.GetValue(i), arrayType.ElementType, i.ToString()));
			}
			return arrayData.ToArray();
		}
		case MetadataType.Object:
			if (constantValueToSerialize != null)
			{
				throw new InvalidOperationException("Default value for " + name + " must be null.");
			}
			return GetCompressedInt32(-1);
		case MetadataType.ByReference:
			return GetBytesForConstantValue(constantValueToSerialize, declaredParameterOrFieldType.GetElementType(), name);
		case MetadataType.String:
		{
			if (constantValueToSerialize == null)
			{
				return GetCompressedInt32(-1);
			}
			string stringValue = (string)constantValueToSerialize;
			int byteCount = Encoding.UTF8.GetByteCount(stringValue);
			byte[] byteCountData = GetCompressedInt32(byteCount);
			byte[] lengthPrefixedData = new byte[byteCountData.Length + byteCount];
			Array.Copy(byteCountData, lengthPrefixedData, byteCountData.Length);
			Array.Copy(Encoding.UTF8.GetBytes(stringValue), 0, lengthPrefixedData, byteCountData.Length, byteCount);
			return lengthPrefixedData;
		}
		case MetadataType.ValueType:
		case MetadataType.GenericInstance:
			if (declaredParameterOrFieldType.IsEnum)
			{
				return ConstantDataFor(constantValueToSerialize, declaredParameterOrFieldType.GetUnderlyingEnumType(), name);
			}
			break;
		}
		throw new ArgumentOutOfRangeException("declaredParameterOrFieldType", $"Cannot create a constant value for types of {declaredParameterOrFieldType} for {name}");
	}

	internal static byte[] ConstantDataFor(object value, TypeReference declaredParameterOrFieldType, string name)
	{
		if (declaredParameterOrFieldType is GenericInstanceType genericInstanceType && declaredParameterOrFieldType.IsNullableGenericInstance)
		{
			return ConstantDataFor(value, genericInstanceType.GenericArguments[0], name);
		}
		object constantValueToSerialize = value;
		if (DetermineMetadataTypeForDefaultValueBasedOnTypeOfConstant(declaredParameterOrFieldType.MetadataType, constantValueToSerialize) != declaredParameterOrFieldType.MetadataType)
		{
			constantValueToSerialize = ChangePrimitiveType(constantValueToSerialize, declaredParameterOrFieldType);
		}
		return GetBytesForConstantValue(constantValueToSerialize, declaredParameterOrFieldType, name);
	}

	private static MetadataType DetermineMetadataTypeForDefaultValueBasedOnTypeOfConstant(MetadataType metadataType, object constant)
	{
		if (constant is byte)
		{
			return MetadataType.Byte;
		}
		if (constant is sbyte)
		{
			return MetadataType.SByte;
		}
		if (constant is ushort)
		{
			return MetadataType.UInt16;
		}
		if (constant is short)
		{
			return MetadataType.Int16;
		}
		if (constant is uint)
		{
			return MetadataType.UInt32;
		}
		if (constant is int)
		{
			return MetadataType.Int32;
		}
		if (constant is ulong)
		{
			return MetadataType.UInt64;
		}
		if (constant is long)
		{
			return MetadataType.Int64;
		}
		if (constant is float)
		{
			return MetadataType.Single;
		}
		if (constant is double)
		{
			return MetadataType.Double;
		}
		if (constant is char)
		{
			return MetadataType.Char;
		}
		if (constant is bool)
		{
			return MetadataType.Boolean;
		}
		return metadataType;
	}

	public static object ChangePrimitiveType(object o, TypeReference type)
	{
		if (o is uint && type.MetadataType == MetadataType.Int32)
		{
			return (int)(uint)o;
		}
		if (o is int && type.MetadataType == MetadataType.UInt32)
		{
			return (uint)(int)o;
		}
		return Convert.ChangeType(o, DetermineTypeForDefaultValueBasedOnDeclaredType(type, o));
	}

	private static Type DetermineTypeForDefaultValueBasedOnDeclaredType(TypeReference type, object constant)
	{
		return type.MetadataType switch
		{
			MetadataType.Byte => typeof(byte), 
			MetadataType.SByte => typeof(sbyte), 
			MetadataType.UInt16 => typeof(ushort), 
			MetadataType.Int16 => typeof(short), 
			MetadataType.UInt32 => typeof(uint), 
			MetadataType.Int32 => typeof(int), 
			MetadataType.UInt64 => typeof(ulong), 
			MetadataType.Int64 => typeof(long), 
			MetadataType.Single => typeof(float), 
			MetadataType.Double => typeof(double), 
			_ => constant.GetType(), 
		};
	}

	internal static bool TypesDoNotExceedMaximumRecursion(ReadOnlyContext context, IEnumerable<TypeReference> types)
	{
		return types.All((TypeReference t) => TypeDoesNotExceedMaximumRecursion(context, t));
	}

	internal static bool TypeDoesNotExceedMaximumRecursion(ReadOnlyContext context, TypeReference type)
	{
		return !GenericsUtilities.CheckForMaximumRecursion(context, type);
	}

	public static uint GetEncodedMethodMetadataUsageIndex(MethodReference method, IMetadataCollectionResults metadataCollection, IGenericMethodCollectorResults genericMethods)
	{
		if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
		{
			if (genericMethods.TryGetValue(method, out var index))
			{
				return GetEncodedMetadataUsageIndex(index, Il2CppMetadataUsage.MethodRef);
			}
			return GetEncodedMetadataUsageIndex(0u, Il2CppMetadataUsage.Invalid);
		}
		return GetEncodedMetadataUsageIndex((uint)metadataCollection.GetMethodIndex(method.Resolve()), Il2CppMetadataUsage.MethodInfo);
	}

	public static uint GetEncodedMethodMetadataUsageIndex(VTableSlot vTableSlot, IMetadataCollectionResults metadataCollection, IGenericMethodCollectorResults genericMethods)
	{
		if (vTableSlot.Attr == VTableSlotAttr.AmbiguousDefaultInterfaceMethod)
		{
			return GetEncodedMetadataUsageIndex(1u, Il2CppMetadataUsage.Invalid);
		}
		if (vTableSlot.Method == null)
		{
			return GetEncodedMetadataUsageIndex(0u, Il2CppMetadataUsage.Invalid);
		}
		return GetEncodedMethodMetadataUsageIndex(vTableSlot.Method, metadataCollection, genericMethods);
	}

	internal static uint GetEncodedMetadataUsageIndex(uint index, Il2CppMetadataUsage type)
	{
		return (uint)((int)type << 29) | (index << 1) | 1;
	}

	internal static void WriteCompressedUInt32(Stream writer, uint value)
	{
		if (value < 128)
		{
			writer.WriteByte((byte)value);
			return;
		}
		if (value < 16384)
		{
			writer.WriteByte((byte)(0x80 | (byte)(value >> 8)));
			writer.WriteByte((byte)(value & 0xFF));
			return;
		}
		if (value < 536870912)
		{
			writer.WriteByte((byte)(0xC0 | (byte)(value >> 24)));
			writer.WriteByte((byte)((value >> 16) & 0xFF));
			writer.WriteByte((byte)((value >> 8) & 0xFF));
			writer.WriteByte((byte)(value & 0xFF));
			return;
		}
		switch (value)
		{
		case 4294967294u:
			writer.WriteByte(254);
			break;
		case uint.MaxValue:
			writer.WriteByte(byte.MaxValue);
			break;
		default:
			writer.WriteByte(240);
			writer.WriteUInt(value);
			break;
		}
	}

	internal static void WriteCompressedInt32(Stream writer, int value)
	{
		uint encoded;
		if (value < 0)
		{
			if (value == int.MinValue)
			{
				encoded = uint.MaxValue;
			}
			else
			{
				encoded = (uint)(-value - 1);
				encoded <<= 1;
				encoded |= 1;
			}
		}
		else
		{
			encoded = (uint)value;
			encoded <<= 1;
		}
		WriteCompressedUInt32(writer, encoded);
	}

	public static byte[] GetCompressedInt32(int value)
	{
		MemoryStream obj = new MemoryStream
		{
			Capacity = 5
		};
		WriteCompressedInt32(obj, value);
		return obj.ToArray();
	}

	public static byte[] GetCompressedUInt32(uint value)
	{
		MemoryStream obj = new MemoryStream
		{
			Capacity = 5
		};
		WriteCompressedUInt32(obj, value);
		return obj.ToArray();
	}
}
