using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class ComObjectMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	public const NativeType kNativeTypeIInspectable = (NativeType)46;

	private readonly bool _marshalAsInspectable;

	private readonly TypeReference _windowsRuntimeType;

	private readonly bool _isSealedNativeClass;

	private readonly bool _isClass;

	private readonly bool _isManagedWinRTClass;

	private readonly TypeReference _defaultInterface;

	private readonly string _interfaceTypeName;

	private readonly MarshaledType[] _marshaledTypes;

	private readonly bool _forNativeToManagedWrapper;

	public sealed override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public ComObjectMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool forNativeToManagedWrapper)
		: base(type)
	{
		_windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
		TypeDefinition typeDef = _windowsRuntimeType.Resolve();
		_forNativeToManagedWrapper = forNativeToManagedWrapper;
		_marshalAsInspectable = marshalType == MarshalType.WindowsRuntime || typeDef.IsExposedToWindowsRuntime() || (marshalInfo != null && marshalInfo.NativeType == (NativeType)46);
		_isClass = !typeDef.IsInterface && !type.IsSystemObject;
		_isManagedWinRTClass = typeDef.IsWindowsRuntimeProjection && typeDef.Module != null && typeDef.Module.MetadataKind == MetadataKind.ManagedWindowsMetadata;
		_isSealedNativeClass = typeDef.IsSealed && !_isManagedWinRTClass;
		_defaultInterface = (_isClass ? typeDef.ExtractDefaultInterface() : _windowsRuntimeType);
		if (type.IsSystemObject)
		{
			_interfaceTypeName = (_marshalAsInspectable ? "Il2CppIInspectable" : "Il2CppIUnknown");
		}
		else
		{
			_interfaceTypeName = _defaultInterface.CppName;
		}
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(_interfaceTypeName + "*", _interfaceTypeName + "*")
		};
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		if (!_typeRef.IsSystemObject)
		{
			writer.AddForwardDeclaration("struct " + _interfaceTypeName);
		}
	}

	public override void WriteNativeStructDefinition(IReadOnlyContextGeneratedCodeWriter writer)
	{
		WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		if (!_typeRef.IsSystemObject)
		{
			if (_isClass)
			{
				writer.AddIncludeForTypeDefinition(writer.Context, _windowsRuntimeType);
			}
			writer.AddIncludeForTypeDefinition(writer.Context, _defaultInterface);
		}
	}

	public sealed override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			if (_isSealedNativeClass)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_query_interface<{_interfaceTypeName}>(static_cast<{"Il2CppComObject*"}>({sourceVariable.Load(writer.Context)}));");
				if (_forNativeToManagedWrapper)
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"({destinationVariable})->AddRef();");
				}
			}
			else if (_isManagedWinRTClass)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_get_or_create_ccw<{_interfaceTypeName}>({sourceVariable.Load(writer.Context)});");
			}
			else
			{
				WriteMarshalToNativeForNonSealedType(writer, sourceVariable, destinationVariable);
			}
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {"NULL"};");
		}
	}

	private void WriteMarshalToNativeForNonSealedType(IGeneratedCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable)
	{
		IGeneratedCodeWriter generatedCodeWriter = writer;
		generatedCodeWriter.WriteLine($"if (il2cpp_codegen_is_import_or_windows_runtime({sourceVariable.Load(writer.Context)}))");
		using (new BlockWriter(writer))
		{
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_query_interface<{_interfaceTypeName}>(static_cast<{"Il2CppComObject*"}>({sourceVariable.Load(writer.Context)}));");
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"({destinationVariable})->AddRef();");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_get_or_create_ccw<{_interfaceTypeName}>({sourceVariable.Load(writer.Context)});");
		}
	}

	public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string managedTypeName = (_isClass ? _windowsRuntimeType.CppName : writer.Context.Global.Services.TypeProvider.SystemObject.CppName);
			if (_isManagedWinRTClass)
			{
				destinationVariable.WriteStore(writer, Emit.Cast(managedTypeName + "*", $"CastclassSealed(il2cpp_codegen_com_unpack_ccw({variableName}), {metadataAccess.TypeInfoFor(_typeRef)})"));
				return;
			}
			TypeReference fallbackType = ((_typeRef.IsInterface || !_typeRef.Resolve().IsComOrWindowsRuntimeType()) ? writer.Context.Global.Services.TypeProvider.Il2CppComObjectTypeReference : _typeRef);
			if (_isSealedNativeClass)
			{
				destinationVariable.WriteStore(writer, "il2cpp_codegen_com_get_or_create_rcw_for_sealed_class<{0}>({1}, {2})", managedTypeName, variableName, metadataAccess.TypeInfoFor(_typeRef));
			}
			else if (_marshalAsInspectable)
			{
				destinationVariable.WriteStore(writer, "il2cpp_codegen_com_get_or_create_rcw_from_iinspectable<{0}>({1}, {2})", managedTypeName, variableName, metadataAccess.TypeInfoFor(fallbackType));
			}
			else
			{
				destinationVariable.WriteStore(writer, "il2cpp_codegen_com_get_or_create_rcw_from_iunknown<{0}>({1}, {2})", managedTypeName, variableName, metadataAccess.TypeInfoFor(fallbackType));
			}
			writer.WriteLine();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (il2cpp_codegen_is_import_or_windows_runtime({destinationVariable.Load(writer.Context)}))");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_cache_queried_interface(static_cast<Il2CppComObject*>({destinationVariable.Load(writer.Context)}), {_interfaceTypeName}::IID, {variableName});");
			}
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			destinationVariable.WriteStore(writer, "NULL");
		}
	}

	public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
	{
		if (!_isSealedNativeClass)
		{
			WriteMarshalCleanupOutVariable(writer, variableName, metadataAccess, managedVariableName);
		}
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"({variableName})->Release();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}
}
