using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

internal class IReferenceComCallableWrapperMethodBodyWriter : ComCallableWrapperMethodBodyWriter
{
	private enum PropertyType
	{
		Empty = 0,
		UInt8 = 1,
		Int16 = 2,
		UInt16 = 3,
		Int32 = 4,
		UInt32 = 5,
		Int64 = 6,
		UInt64 = 7,
		Single = 8,
		Double = 9,
		Char16 = 10,
		Boolean = 11,
		String = 12,
		Inspectable = 13,
		DateTime = 14,
		TimeSpan = 15,
		Guid = 16,
		Point = 17,
		Size = 18,
		Rect = 19,
		Other = 20,
		UInt8Array = 1025,
		Int16Array = 1026,
		UInt16Array = 1027,
		Int32Array = 1028,
		UInt32Array = 1029,
		Int64Array = 1030,
		UInt64Array = 1031,
		SingleArray = 1032,
		DoubleArray = 1033,
		Char16Array = 1034,
		BooleanArray = 1035,
		StringArray = 1036,
		InspectableArray = 1037,
		DateTimeArray = 1038,
		TimeSpanArray = 1039,
		GuidArray = 1040,
		PointArray = 1041,
		SizeArray = 1042,
		RectArray = 1043,
		OtherArray = 1044
	}

	private readonly TypeReference _boxedType;

	private readonly TypeReference _overflowException;

	private readonly bool _isIPropertyArrayMethod;

	private readonly TypeReference _desiredConvertedType;

	public IReferenceComCallableWrapperMethodBodyWriter(ReadOnlyContext context, MethodReference interfaceMethod, TypeReference boxedType)
		: base(context, interfaceMethod, interfaceMethod, MarshalType.WindowsRuntime)
	{
		_boxedType = boxedType;
		_overflowException = context.Global.Services.TypeProvider.GetSystemType(SystemType.OverflowException);
		_isIPropertyArrayMethod = IsIPropertyArrayMethod(_managedMethod);
		_desiredConvertedType = (_isIPropertyArrayMethod ? ((ByReferenceType)_managedMethod.Parameters[0].ParameterType).ElementType : _managedMethod.ReturnType);
	}

	public override void WriteMethodBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (WillCertainlyThrowException())
		{
			WriteReturnFailedConversion(writer);
		}
		else
		{
			base.WriteMethodBody(writer, metadataAccess);
		}
	}

	protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		switch (_managedMethod.Name)
		{
		case "get_Value":
			WriteGetValueMethod(writer, metadataAccess);
			break;
		case "get_Type":
			WriteGetTypeMethod(writer, metadataAccess);
			break;
		case "get_IsNumericScalar":
			WriteGetIsNumericScalar(writer, metadataAccess);
			break;
		default:
		{
			ManagedMarshalValue resultVariable = (_isIPropertyArrayMethod ? new ManagedMarshalValue(localVariableNames[0]).Dereferenced : new ManagedMarshalValue(writer.Context.Global.Services.Naming.ForInteropReturnValue()));
			WriteGetTypedValueMethod(writer, resultVariable, metadataAccess);
			break;
		}
		}
	}

	private static bool IsIPropertyArrayMethod(MethodReference method)
	{
		switch (method.Name)
		{
		case "GetSingle":
		case "GetDouble":
		case "GetString":
		case "GetUInt16":
		case "GetUInt32":
		case "GetUInt64":
		case "get_Value":
		case "GetChar16":
		case "GetPoint":
		case "get_Type":
		case "GetUInt8":
		case "GetInt16":
		case "GetInt32":
		case "GetInt64":
		case "get_IsNumericScalar":
		case "GetGuid":
		case "GetRect":
		case "GetSize":
		case "GetDateTime":
		case "GetTimeSpan":
		case "GetBoolean":
			return false;
		case "GetInspectableArray":
		case "GetInt64Array":
		case "GetPointArray":
		case "GetUInt8Array":
		case "GetInt16Array":
		case "GetInt32Array":
		case "GetSingleArray":
		case "GetDoubleArray":
		case "GetStringArray":
		case "GetUInt16Array":
		case "GetUInt32Array":
		case "GetUInt64Array":
		case "GetChar16Array":
		case "GetGuidArray":
		case "GetRectArray":
		case "GetSizeArray":
		case "GetDateTimeArray":
		case "GetTimeSpanArray":
		case "GetBooleanArray":
			return true;
		default:
			throw new NotSupportedException("IReferenceComCallableWrapperMethodBodyWriter does not support writing body for " + method.FullName + ".");
		}
	}

	private void WriteGetValueMethod(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = {GetUnboxedValueExpression(writer.Context, metadataAccess)};");
	}

	private string GetUnboxedValueExpression(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		string unboxed = GetPointerToValueExpression(context, metadataAccess);
		if (_boxedType.IsValueType)
		{
			return "*" + unboxed;
		}
		return unboxed;
	}

	private string GetPointerToValueExpression(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		string resultVariableType = _boxedType.CppNameForVariable;
		if (!_boxedType.IsValueType)
		{
			return $"static_cast<{resultVariableType}>({ManagedObjectExpression})";
		}
		return $"static_cast<{resultVariableType}*>(UnBox({ManagedObjectExpression}, {metadataAccess.TypeInfoFor(_boxedType)}))";
	}

	private void WriteGetTypeMethod(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = {GetBoxedPropertyType(_boxedType)};");
	}

	private void WriteGetIsNumericScalar(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string isNumericScaleString = (IsNumericScalar(_boxedType) ? "true" : "false");
		writer.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = {isNumericScaleString};");
	}

	private static bool IsNumericScalar(TypeReference type)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Byte:
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
		case MetadataType.Int64:
		case MetadataType.UInt64:
		case MetadataType.Single:
		case MetadataType.Double:
			return true;
		case MetadataType.ValueType:
			return type.IsEnum;
		default:
			return false;
		}
	}

	private void GetFailedConversionTypeNamesForExceptionMessage(out string fromType, out string toType)
	{
		fromType = GetBoxedPropertyType(_boxedType).ToString();
		if (_desiredConvertedType is ArrayType { ElementType: var desiredElementType })
		{
			string desiredElementTypeName = (desiredElementType.IsWindowsRuntimePrimitiveType() ? GetDesiredTypeNameInExceptionMessage(desiredElementType) : desiredElementType.Name);
			toType = desiredElementTypeName + "[]";
		}
		else
		{
			toType = GetDesiredTypeNameInExceptionMessage(_desiredConvertedType).ToString();
		}
	}

	private bool WillCertainlyThrowException()
	{
		switch (_managedMethod.Name)
		{
		case "get_Value":
		case "get_Type":
		case "get_IsNumericScalar":
			return false;
		default:
		{
			TypeReference boxedUnderlyingType = _boxedType;
			if (boxedUnderlyingType.IsEnum)
			{
				boxedUnderlyingType = boxedUnderlyingType.GetUnderlyingEnumType();
			}
			if (_desiredConvertedType == boxedUnderlyingType)
			{
				return false;
			}
			if (_desiredConvertedType.MetadataType == MetadataType.String && _boxedType.FullName == "System.Guid")
			{
				return false;
			}
			if (_desiredConvertedType is ArrayType desiredArrayType && _boxedType is ArrayType && CanConvertArray(desiredArrayType))
			{
				return false;
			}
			if (CanConvertNumber(_desiredConvertedType, boxedUnderlyingType))
			{
				return false;
			}
			if (_desiredConvertedType.Namespace == "System" && _desiredConvertedType.Name == "Guid" && _boxedType.MetadataType == MetadataType.String)
			{
				return false;
			}
			return true;
		}
		}
	}

	private void WriteGetTypedValueMethod(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess)
	{
		TypeReference boxedUnderlyingType = _boxedType;
		if (boxedUnderlyingType.IsEnum)
		{
			boxedUnderlyingType = boxedUnderlyingType.GetUnderlyingEnumType();
		}
		if (_desiredConvertedType == boxedUnderlyingType)
		{
			resultVariable.WriteStore(writer, GetUnboxedValueExpression(writer.Context, metadataAccess));
			return;
		}
		if (_desiredConvertedType.MetadataType == MetadataType.String && _boxedType.FullName == "System.Guid")
		{
			MethodDefinition toStringMethod = _boxedType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToString" && m.Parameters.Count == 0);
			writer.AddIncludeForMethodDeclaration(toStringMethod);
			WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, toStringMethod, MethodCallType.Normal, GetPointerToValueExpression(writer.Context, metadataAccess));
			return;
		}
		if (_desiredConvertedType is ArrayType desiredArrayType && _boxedType is ArrayType boxedArrayType && CanConvertArray(desiredArrayType))
		{
			WriteConvertArray(writer, resultVariable, metadataAccess, desiredArrayType, boxedArrayType);
			return;
		}
		if (CanConvertNumber(_desiredConvertedType, boxedUnderlyingType))
		{
			WriteConvertNumber(writer, resultVariable, metadataAccess, boxedUnderlyingType);
			return;
		}
		if (_desiredConvertedType.Namespace == "System" && _desiredConvertedType.Name == "Guid" && _boxedType.MetadataType == MetadataType.String)
		{
			WriteGuidParse(writer, resultVariable, GetPointerToValueExpression(writer.Context, metadataAccess), metadataAccess, _desiredConvertedType, delegate(TypeReference exceptionType)
			{
				WriteReturnFailedConversion(writer, exceptionType);
			});
			return;
		}
		throw new InvalidOperationException($"Cannot write conversion from {_boxedType} to {_desiredConvertedType}!");
	}

	private static bool CanConvertNumber(TypeReference desiredType, TypeReference boxedUnderlyingType)
	{
		if (!IsNumericScalar(desiredType))
		{
			return false;
		}
		if (!IsNumericScalar(boxedUnderlyingType) && boxedUnderlyingType.MetadataType != MetadataType.String)
		{
			return boxedUnderlyingType.MetadataType == MetadataType.Object;
		}
		return true;
	}

	private static bool CanConvertArray(ArrayType desiredType)
	{
		TypeReference desiredElementType = desiredType.ElementType;
		if (!desiredElementType.IsWindowsRuntimePrimitiveType())
		{
			return false;
		}
		MetadataType metadataType = desiredElementType.MetadataType;
		if (metadataType - 2 <= MetadataType.Void || metadataType == MetadataType.Object)
		{
			return false;
		}
		return true;
	}

	private void WriteAssignMethodCallExpressionToReturnVariable(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, MethodReference methodToCall, MethodCallType methodCallType, params string[] args)
	{
		List<string> methodArgs = new List<string>();
		methodArgs.AddRange(args);
		writer.WriteLine($"{methodToCall.ReturnType.CppNameForVariable} {"il2cppRetVal"};");
		MethodBodyWriter.WriteMethodCallExpression("il2cppRetVal", writer, _managedMethod, methodToCall, writer.Context.Global.Services.TypeFactory.EmptyResolver(), methodCallType, metadataAccess.MethodMetadataFor(methodToCall), writer.Context.Global.Services.VTable, methodArgs, useArrayBoundsCheck: false);
		resultVariable.WriteStore(writer, "il2cppRetVal");
	}

	private void WriteGuidParse(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, string sourceVariable, IRuntimeMetadataAccess metadataAccess, TypeReference desiredType, Action<TypeReference> translateExceptionAction)
	{
		writer.WriteLine("try");
		using (new BlockWriter(writer))
		{
			MethodDefinition guidParseMethod = desiredType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "Parse" && m.Parameters.Count > 0 && m.Parameters[0].ParameterType.Name == "String");
			writer.AddIncludeForMethodDeclaration(guidParseMethod);
			WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, guidParseMethod, MethodCallType.Normal, sourceVariable);
		}
		writer.WriteLine("catch (const Il2CppExceptionWrapper&)");
		using (new BlockWriter(writer))
		{
			translateExceptionAction(writer.Context.Global.Services.TypeProvider.SystemException);
		}
	}

	private void WriteConvertArray(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, ArrayType desiredArrayType, ArrayType boxedArrayType)
	{
		TypeReference desiredElementType = desiredArrayType.ElementType;
		TypeReference boxedUnderlyingElementTypeType = boxedArrayType.ElementType;
		if (boxedUnderlyingElementTypeType.IsEnum)
		{
			boxedUnderlyingElementTypeType = boxedUnderlyingElementTypeType.GetUnderlyingEnumType();
		}
		bool convertGuidsToStrings = desiredElementType.MetadataType == MetadataType.String && boxedUnderlyingElementTypeType.Namespace == "System" && boxedUnderlyingElementTypeType.Name == "Guid";
		bool convertStringsToGuids = desiredElementType.Namespace == "System" && desiredElementType.Name == "Guid" && boxedUnderlyingElementTypeType.MetadataType == MetadataType.String;
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{_boxedType.CppNameForVariable} {"managedArray"} = {GetPointerToValueExpression(writer.Context, metadataAccess)};");
		writer.WriteLine("il2cpp_array_size_t arrayLength = managedArray->max_length;");
		if (!convertStringsToGuids && !convertGuidsToStrings && !CanConvertNumber(desiredElementType, boxedUnderlyingElementTypeType))
		{
			writer.WriteLine("if (arrayLength > 0)");
			using (new BlockWriter(writer))
			{
				WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedArrayType.ElementType, "0", metadataAccess, _overflowException);
			}
			writer.WriteLine();
			resultVariable.WriteStore(writer, "NULL");
			return;
		}
		resultVariable.WriteStore(writer, Emit.NewSZArray(writer.Context, desiredArrayType, "static_cast<uint32_t>(arrayLength)", metadataAccess));
		writer.WriteLine("for (il2cpp_array_size_t i = 0; i < arrayLength; i++)");
		using (new BlockWriter(writer))
		{
			ManagedMarshalValue destinationItem = new ManagedMarshalValue(resultVariable, "i");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{boxedUnderlyingElementTypeType.CppNameForVariable} item = {Emit.LoadArrayElement("managedArray", "i", useArrayBoundsCheck: false)};");
			if (convertGuidsToStrings)
			{
				MethodDefinition toStringMethod = boxedUnderlyingElementTypeType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToString" && m.Parameters.Count == 0);
				writer.AddIncludeForMethodDeclaration(toStringMethod);
				WriteAssignMethodCallExpressionToReturnVariable(writer, destinationItem, metadataAccess, toStringMethod, MethodCallType.Normal, "&item");
			}
			else if (convertStringsToGuids)
			{
				WriteGuidParse(writer, destinationItem, "item", metadataAccess, desiredElementType, delegate(TypeReference exceptionType)
				{
					WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedUnderlyingElementTypeType, "i", metadataAccess, exceptionType);
				});
			}
			else
			{
				WriteConvertNumber(writer, destinationItem, "item", metadataAccess, desiredElementType, boxedUnderlyingElementTypeType, delegate(TypeReference exceptionType)
				{
					WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedUnderlyingElementTypeType, "i", metadataAccess, exceptionType);
				});
			}
		}
	}

	private void WriteConvertNumber(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, TypeReference boxedUnderlyingType)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{boxedUnderlyingType.CppNameForVariable} value = {GetUnboxedValueExpression(writer.Context, metadataAccess)};");
		WriteConvertNumber(writer, resultVariable, "value", metadataAccess, _desiredConvertedType, boxedUnderlyingType, delegate(TypeReference exceptionType)
		{
			WriteReturnFailedConversion(writer, exceptionType);
		});
	}

	private void WriteConvertNumber(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, string sourceVariable, IRuntimeMetadataAccess metadataAccess, TypeReference desiredType, TypeReference boxedUnderlyingType, Action<TypeReference> translateExceptionAction)
	{
		if (desiredType.IsUnsignedIntegralType && (boxedUnderlyingType.IsSignedIntegralType || boxedUnderlyingType.MetadataType == MetadataType.Single || boxedUnderlyingType.MetadataType == MetadataType.Double))
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({sourceVariable} < 0)");
			using (new BlockWriter(writer))
			{
				translateExceptionAction(_overflowException);
			}
			writer.WriteLine();
		}
		writer.WriteLine("try");
		using (new BlockWriter(writer))
		{
			MethodDefinition conversionMethod = boxedUnderlyingType.Resolve().Methods.SingleOrDefault((MethodDefinition m) => m.Name == $"System.IConvertible.To{desiredType.MetadataType}");
			string thisArg = (boxedUnderlyingType.IsValueType ? Emit.AddressOf(sourceVariable) : sourceVariable);
			MethodCallType methodCallType;
			if (conversionMethod != null)
			{
				writer.AddIncludeForMethodDeclaration(conversionMethod);
				methodCallType = MethodCallType.Normal;
			}
			else
			{
				conversionMethod = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.IConvertible).Resolve().Methods.Single((MethodDefinition m) => m.Name == $"To{desiredType.MetadataType}");
				methodCallType = MethodCallType.Virtual;
			}
			WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, conversionMethod, methodCallType, thisArg, "NULL");
		}
		writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
		using (new BlockWriter(writer))
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IsInst((RuntimeObject*)ex.ex, {metadataAccess.TypeInfoFor(_overflowException)}))");
			using (new BlockWriter(writer))
			{
				translateExceptionAction(_overflowException);
			}
			writer.WriteLine();
			translateExceptionAction(writer.Context.Global.Services.TypeProvider.SystemException);
		}
	}

	private void WriteReturnFailedConversion(IGeneratedMethodCodeWriter writer)
	{
		GetFailedConversionTypeNamesForExceptionMessage(out var fromType, out var toType);
		writer.WriteStatement($"return il2cpp_codegen_com_handle_invalid_iproperty_conversion({fromType.InQuotes()}, {toType.InQuotes()})");
	}

	private void WriteReturnFailedConversion(IGeneratedMethodCodeWriter writer, TypeReference exceptionType)
	{
		if (CanConvertNumber(_desiredConvertedType, _boxedType) && exceptionType == _overflowException)
		{
			GetFailedConversionTypeNamesForExceptionMessage(out var fromType, out var toType);
			string hresult = Emit.Call(writer.Context, "il2cpp_codegen_com_handle_invalid_iproperty_conversion", ManagedObjectExpression, fromType.InQuotes(), toType.InQuotes());
			writer.WriteLine($"return {hresult};");
		}
		WriteReturnFailedConversion(writer);
	}

	private void WriteThrowInvalidCastExceptionForArray(IGeneratedMethodCodeWriter writer, string arrayExpression, TypeReference desiredElementType, TypeReference boxedElementType, string index, IRuntimeMetadataAccess metadataAccess, TypeReference exceptionType)
	{
		string desiredElementTypeName = GetDesiredTypeNameInExceptionMessage(desiredElementType);
		List<string> args = new List<string>
		{
			GetBoxedPropertyType(_boxedType).ToString().InQuotes(),
			GetBoxedPropertyType(boxedElementType).ToString().InQuotes(),
			desiredElementTypeName.InQuotes(),
			index
		};
		if (CanConvertNumber(desiredElementType, boxedElementType) && exceptionType == _overflowException)
		{
			args.Insert(0, Emit.LoadArrayElement(arrayExpression, index, useArrayBoundsCheck: false));
			if (boxedElementType.IsValueType)
			{
				args[0] = Emit.Call(writer.Context, "Box", metadataAccess.TypeInfoFor(boxedElementType), Emit.LoadArrayElementAddress(arrayExpression, index, useArrayBoundsCheck: false));
			}
		}
		string hresult = Emit.Call(writer.Context, "il2cpp_codegen_com_handle_invalid_ipropertyarray_conversion", args);
		writer.WriteLine($"return {hresult};");
	}

	private static PropertyType GetBoxedPropertyType(TypeReference type)
	{
		return type.MetadataType switch
		{
			MetadataType.Byte => PropertyType.UInt8, 
			MetadataType.Int16 => PropertyType.Int16, 
			MetadataType.UInt16 => PropertyType.UInt16, 
			MetadataType.Int32 => PropertyType.Int32, 
			MetadataType.UInt32 => PropertyType.UInt32, 
			MetadataType.Int64 => PropertyType.Int64, 
			MetadataType.UInt64 => PropertyType.UInt64, 
			MetadataType.Single => PropertyType.Single, 
			MetadataType.Double => PropertyType.Double, 
			MetadataType.Char => PropertyType.Char16, 
			MetadataType.Boolean => PropertyType.Boolean, 
			MetadataType.String => PropertyType.String, 
			MetadataType.Object => PropertyType.Inspectable, 
			MetadataType.ValueType => type.FullName switch
			{
				"System.Guid" => PropertyType.Guid, 
				"System.DateTimeOffset" => PropertyType.DateTime, 
				"System.TimeSpan" => PropertyType.TimeSpan, 
				"Windows.Foundation.Point" => PropertyType.Point, 
				"Windows.Foundation.Size" => PropertyType.Size, 
				"Windows.Foundation.Rect" => PropertyType.Rect, 
				_ => PropertyType.Other, 
			}, 
			MetadataType.Array => GetBoxedPropertyType(((ArrayType)type).ElementType) + 1024, 
			_ => PropertyType.Other, 
		};
	}

	private static string GetDesiredTypeNameInExceptionMessage(TypeReference desiredType)
	{
		if (desiredType.MetadataType == MetadataType.Byte)
		{
			return "Byte";
		}
		return GetBoxedPropertyType(desiredType).ToString();
	}
}
