using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class StringMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	public const NativeType kNativeTypeHString = (NativeType)47;

	private readonly string _marshaledTypeName;

	private readonly NativeType _nativeType;

	private readonly bool _isStringBuilder;

	private readonly MarshalInfo _marshalInfo;

	private readonly bool _useUnicodeCharSet;

	private readonly MarshaledType[] _marshaledTypes;

	private readonly bool _canReferenceOriginalManagedString;

	public NativeType NativeType => _nativeType;

	private bool IsFixedSizeString => _nativeType == NativeType.FixedSysString;

	private bool IsWideString
	{
		get
		{
			if (_nativeType != NativeType.LPWStr && _nativeType != NativeType.BStr && _nativeType != (NativeType)47)
			{
				if (IsFixedSizeString)
				{
					return _useUnicodeCharSet;
				}
				return false;
			}
			return true;
		}
	}

	private int BytesPerCharacter
	{
		get
		{
			if (!IsWideString)
			{
				return 1;
			}
			return 2;
		}
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public override int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		if (IsFixedSizeString)
		{
			return ((FixedSysStringMarshalInfo)_marshalInfo).Size * BytesPerCharacter;
		}
		return base.GetNativeSizeWithoutPointers(context);
	}

	public static NativeType DetermineNativeTypeFor(MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset, bool isStringBuilder)
	{
		NativeType nativeType = (NativeType)(((int?)marshalInfo?.NativeType) ?? ((marshalType != 0) ? 102 : (useUnicodeCharset ? 21 : 20)));
		bool isKnownNativeStringType = false;
		if ((uint)(nativeType - 19) <= 2u || nativeType == NativeType.FixedSysString || nativeType == (NativeType)47)
		{
			isKnownNativeStringType = true;
		}
		if (!isKnownNativeStringType || (isStringBuilder && nativeType != NativeType.LPStr && nativeType != NativeType.LPWStr))
		{
			switch (marshalType)
			{
			case MarshalType.PInvoke:
				nativeType = NativeType.LPStr;
				break;
			case MarshalType.COM:
				nativeType = NativeType.BStr;
				break;
			case MarshalType.WindowsRuntime:
				nativeType = (NativeType)47;
				break;
			}
		}
		return nativeType;
	}

	public StringMarshalInfoWriter(TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling)
		: base(type)
	{
		_isStringBuilder = MarshalingUtils.IsStringBuilder(type);
		_useUnicodeCharSet = useUnicodeCharSet;
		_nativeType = DetermineNativeTypeFor(marshalType, marshalInfo, _useUnicodeCharSet, _isStringBuilder);
		if (_nativeType == (NativeType)47)
		{
			_marshaledTypeName = "Il2CppHString";
		}
		else if (IsWideString)
		{
			_marshaledTypeName = "Il2CppChar*";
		}
		else
		{
			_marshaledTypeName = "char*";
		}
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(_marshaledTypeName, _marshaledTypeName)
		};
		_marshalInfo = marshalInfo;
		_canReferenceOriginalManagedString = !_isStringBuilder && !forByReferenceType && !forFieldMarshaling && (_nativeType == NativeType.LPWStr || _nativeType == (NativeType)47);
	}

	public override void WriteFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
	{
		if (IsFixedSizeString)
		{
			string fieldName = field.CppName + fieldNameSuffix;
			writer.WriteLine($"{_marshaledTypeName.Replace("*", "")} {fieldName}[{((FixedSysStringMarshalInfo)_marshalInfo).Size}];");
		}
		else
		{
			base.WriteFieldDeclaration(writer, field, fieldNameSuffix);
		}
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		WriteMarshalVariableToNative(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, isMarshalingReturnValue: false);
	}

	public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
	{
		string marshaledVariableName = "_" + sourceVariable.GetNiceName(writer.Context) + "_marshaled";
		WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		WriteMarshalVariableToNative(writer, sourceVariable, marshaledVariableName, null, metadataAccess, isMarshalingReturnValue: true);
		return marshaledVariableName;
	}

	private void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess, bool isMarshalingReturnValue)
	{
		if (_nativeType == (NativeType)47)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} == {"NULL"})");
			using (new BlockWriter(writer))
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_argument_null_exception(\"" + (string.IsNullOrEmpty(managedVariableName) ? sourceVariable.GetNiceName(writer.Context) : managedVariableName) + "\")"));
			}
		}
		if (IsFixedSizeString)
		{
			string marshalFunc = (IsWideString ? "il2cpp_codegen_marshal_wstring_fixed" : "il2cpp_codegen_marshal_string_fixed");
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{marshalFunc}({sourceVariable.Load(writer.Context)}, ({_marshaledTypeName})&{destinationVariable}, {((FixedSysStringMarshalInfo)_marshalInfo).Size.ToString()});");
		}
		else if (_canReferenceOriginalManagedString && !isMarshalingReturnValue)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter;
			if (_nativeType == NativeType.LPWStr)
			{
				string loadedSourceVariable = sourceVariable.Load(writer.Context);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"if ({loadedSourceVariable} != {"NULL"})");
				using (new BlockWriter(writer))
				{
					FieldDefinition stringCharField = writer.Context.Global.Services.TypeProvider.SystemString.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Char);
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{destinationVariable} = &{sourceVariable.Load(writer.Context)}->{stringCharField.CppName};");
					return;
				}
			}
			if (_nativeType != (NativeType)47)
			{
				throw new InvalidOperationException($"StringMarshalInfoWriter doesn't know how to marshal {_nativeType} while maintaining reference to original managed string.");
			}
			string niceName = sourceVariable.GetNiceName(writer.Context);
			string nativeViewName = niceName + "NativeView";
			string stringReferenceName = niceName + "HStringReference";
			writer.WriteLine();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"DECLARE_IL2CPP_STRING_AS_STRING_VIEW_OF_NATIVE_CHARS({nativeViewName}, reinterpret_cast<Il2CppString*>({sourceVariable.Load(writer.Context)}));");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp::vm::Il2CppHStringReference {stringReferenceName}({nativeViewName});");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {stringReferenceName};");
		}
		else
		{
			string marshalFunc = (_isStringBuilder ? (IsWideString ? "il2cpp_codegen_marshal_wstring_builder" : "il2cpp_codegen_marshal_string_builder") : ((_nativeType == NativeType.BStr) ? "il2cpp_codegen_marshal_bstring" : ((_nativeType != (NativeType)47) ? (IsWideString ? "il2cpp_codegen_marshal_wstring" : "il2cpp_codegen_marshal_string") : "il2cpp_codegen_create_hstring")));
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {marshalFunc}({sourceVariable.Load(writer.Context)});");
		}
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		return "NULL";
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		if (_isStringBuilder)
		{
			string marshalFunc = (IsWideString ? "il2cpp_codegen_marshal_wstring_builder_result" : "il2cpp_codegen_marshal_string_builder_result");
			writer.WriteLine($"{marshalFunc}({destinationVariable.Load(writer.Context)}, {variableName});");
		}
		else
		{
			string marshalFunc = _nativeType switch
			{
				NativeType.BStr => "il2cpp_codegen_marshal_bstring_result", 
				(NativeType)47 => "il2cpp_codegen_marshal_hstring_result", 
				_ => IsWideString ? "il2cpp_codegen_marshal_wstring_result" : "il2cpp_codegen_marshal_string_result", 
			};
			destinationVariable.WriteStore(writer, "{0}({1})", marshalFunc, variableName);
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		if (!_canReferenceOriginalManagedString)
		{
			FreeMarshaledString(writer, variableName);
		}
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		FreeMarshaledString(writer, variableName);
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		if (_isStringBuilder)
		{
			string marshaledVariableName = "_" + variableName.GetNiceName(writer.Context) + "_marshaled";
			string marshalFunc = (IsWideString ? "il2cpp_codegen_marshal_empty_wstring_builder" : "il2cpp_codegen_marshal_empty_string_builder");
			writer.WriteLine($"{_marshaledTypeName} {marshaledVariableName} = {marshalFunc}({variableName.Load(writer.Context)});");
			return marshaledVariableName;
		}
		return base.WriteMarshalEmptyVariableToNative(writer, variableName, methodParameters);
	}

	private void FreeMarshaledString(IGeneratedCodeWriter writer, string variableName)
	{
		if (!IsFixedSizeString)
		{
			IGeneratedCodeWriter generatedCodeWriter;
			switch (_nativeType)
			{
			case NativeType.BStr:
				generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"il2cpp_codegen_marshal_free_bstring({variableName});");
				break;
			case (NativeType)47:
				generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"il2cpp_codegen_marshal_free_hstring({variableName});");
				break;
			default:
				generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"il2cpp_codegen_marshal_free({variableName});");
				break;
			}
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}
}
