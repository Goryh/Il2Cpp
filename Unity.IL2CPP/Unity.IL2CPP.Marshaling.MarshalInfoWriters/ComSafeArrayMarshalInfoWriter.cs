using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class ComSafeArrayMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	public enum Il2CppVariantType
	{
		None = 0,
		I2 = 2,
		I4 = 3,
		R4 = 4,
		R8 = 5,
		CY = 6,
		Date = 7,
		BStr = 8,
		Dispatch = 9,
		Error = 10,
		Bool = 11,
		Variant = 12,
		Unknown = 13,
		Decimal = 14,
		I1 = 16,
		UI1 = 17,
		UI2 = 18,
		UI4 = 19,
		I8 = 20,
		UI8 = 21,
		Int = 22,
		UInt = 23
	}

	private readonly TypeReference _elementType;

	private readonly SafeArrayMarshalInfo _marshalInfo;

	private readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

	private readonly MarshaledType[] _marshaledTypes;

	private readonly Il2CppVariantType _elementVariantType;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public override string GetNativeSize(ReadOnlyContext context)
	{
		return "-1";
	}

	public ComSafeArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type, MarshalInfo marshalInfo)
		: base(type)
	{
		_elementType = type.ElementType;
		_marshalInfo = marshalInfo as SafeArrayMarshalInfo;
		_elementVariantType = GetElementVariantType(type.ElementType.MetadataType);
		if (_marshalInfo == null)
		{
			throw new InvalidOperationException("SafeArray type '" + type.FullName + "' has invalid MarshalAsAttribute.");
		}
		if (_marshalInfo.ElementType == VariantType.BStr && _elementType.MetadataType != MetadataType.String)
		{
			throw new InvalidOperationException("SafeArray(BSTR) type '" + type.FullName + "' has invalid MarshalAsAttribute.");
		}
		NativeType nativeElementType = GetNativeElementType();
		_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, MarshalType.COM, new MarshalInfo(nativeElementType));
		string marshaledTypeName = (context.Global.Parameters.EmitComments ? ("Il2CppSafeArray/*" + _marshalInfo.ElementType.ToString().ToUpper() + "*/*") : "Il2CppSafeArray*");
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(marshaledTypeName, marshaledTypeName)
		};
	}

	public ComSafeArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type)
		: this(context, type, new SafeArrayMarshalInfo())
	{
		_elementVariantType = GetElementVariantType(type.ElementType.MetadataType);
	}

	public static bool IsMarshalableAsSafeArray(ReadOnlyContext context, MetadataType metadataType)
	{
		if (metadataType != MetadataType.SByte && metadataType != MetadataType.Byte && metadataType != MetadataType.Int16 && metadataType != MetadataType.UInt16 && metadataType != MetadataType.Int32 && metadataType != MetadataType.UInt32 && metadataType != MetadataType.Int64 && metadataType != MetadataType.UInt64 && metadataType != MetadataType.Single && metadataType != MetadataType.Double && metadataType != MetadataType.IntPtr)
		{
			return metadataType == MetadataType.UIntPtr;
		}
		return true;
	}

	private static Il2CppVariantType GetElementVariantType(MetadataType metadataType)
	{
		return metadataType switch
		{
			MetadataType.Int16 => Il2CppVariantType.I2, 
			MetadataType.Int32 => Il2CppVariantType.I4, 
			MetadataType.Int64 => Il2CppVariantType.I8, 
			MetadataType.Single => Il2CppVariantType.R4, 
			MetadataType.Double => Il2CppVariantType.R8, 
			MetadataType.Byte => Il2CppVariantType.I1, 
			MetadataType.SByte => Il2CppVariantType.UI1, 
			MetadataType.UInt16 => Il2CppVariantType.UI2, 
			MetadataType.UInt32 => Il2CppVariantType.UI4, 
			MetadataType.UInt64 => Il2CppVariantType.UI8, 
			MetadataType.IntPtr => Il2CppVariantType.Int, 
			MetadataType.UIntPtr => Il2CppVariantType.UInt, 
			MetadataType.String => Il2CppVariantType.BStr, 
			_ => throw new NotSupportedException($"SafeArray element type {metadataType} is not supported."), 
		};
	}

	private NativeType GetNativeElementType()
	{
		return _elementVariantType switch
		{
			Il2CppVariantType.I2 => NativeType.I2, 
			Il2CppVariantType.I4 => NativeType.I4, 
			Il2CppVariantType.I8 => NativeType.I8, 
			Il2CppVariantType.R4 => NativeType.R4, 
			Il2CppVariantType.R8 => NativeType.R8, 
			Il2CppVariantType.BStr => NativeType.BStr, 
			Il2CppVariantType.Dispatch => NativeType.IDispatch, 
			Il2CppVariantType.Bool => NativeType.VariantBool, 
			Il2CppVariantType.Unknown => NativeType.IUnknown, 
			Il2CppVariantType.I1 => NativeType.I1, 
			Il2CppVariantType.UI1 => NativeType.U1, 
			Il2CppVariantType.UI2 => NativeType.U2, 
			Il2CppVariantType.UI4 => NativeType.U4, 
			Il2CppVariantType.UI8 => NativeType.U8, 
			Il2CppVariantType.Int => NativeType.Int, 
			Il2CppVariantType.UInt => NativeType.UInt, 
			_ => throw new NotSupportedException($"SafeArray element type {_elementVariantType} is not supported."), 
		};
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		_elementTypeMarshalInfoWriter.WriteIncludesForFieldDeclaration(writer);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		_elementTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		base.WriteIncludesForMarshaling(writer);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (_marshalInfo.ElementType == VariantType.BStr)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_marshal_safe_array_bstring({sourceVariable.Load(writer.Context)});");
		}
		else
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_marshal_safe_array(IL2CPP_VT_{_elementVariantType.ToString().ToUpper()}, {sourceVariable.Load(writer.Context)});");
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		if (_marshalInfo.ElementType == VariantType.BStr)
		{
			destinationVariable.WriteStore(writer, "({0}*)il2cpp_codegen_com_marshal_safe_array_bstring_result({1}, {2})", writer.Context.Global.Services.Naming.ForType(_typeRef), metadataAccess.TypeInfoFor(_elementType), variableName);
		}
		else
		{
			destinationVariable.WriteStore(writer, "({0}*)il2cpp_codegen_com_marshal_safe_array_result(IL2CPP_VT_{1}, {2}, {3})", writer.Context.Global.Services.Naming.ForType(_typeRef), _elementVariantType.ToString().ToUpper(), metadataAccess.TypeInfoFor(_elementType), variableName);
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_destroy_safe_array({variableName});");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
	}
}
