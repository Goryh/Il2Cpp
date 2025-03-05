using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class KeyValuePairMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly DefaultMarshalInfoWriter _keyMarshalInfoWriter;

	private readonly DefaultMarshalInfoWriter _valueMarshalInfoWriter;

	private readonly TypeReference _iKeyValuePair;

	private readonly string _iKeyValuePairTypeName;

	private readonly MarshaledType[] _marshaledTypes;

	public sealed override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public KeyValuePairMarshalInfoWriter(ReadOnlyContext context, GenericInstanceType type)
		: base(type)
	{
		_keyMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type.GenericArguments[0], MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
		_valueMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type.GenericArguments[1], MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
		_iKeyValuePair = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
		_iKeyValuePairTypeName = _iKeyValuePair.CppName;
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(_iKeyValuePairTypeName + "*", _iKeyValuePairTypeName + "*")
		};
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddForwardDeclaration("struct " + _iKeyValuePairTypeName);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
		writer.AddIncludeForTypeDefinition(writer.Context, _iKeyValuePair);
		_keyMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		_valueMarshalInfoWriter.WriteIncludesForMarshaling(writer);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteStatement($"{_typeRef.CppNameForVariable} {sourceVariable.GetNiceName(writer.Context)}Copy = {sourceVariable.Load(writer.Context)};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteStatement($"{destinationVariable} = il2cpp_codegen_com_get_or_create_ccw<{_iKeyValuePairTypeName}>({Emit.Box(writer.Context, _typeRef, sourceVariable.GetNiceName(writer.Context) + "Copy", metadataAccess)})");
	}

	public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		string hr = writer.Context.Global.Services.Naming.ForInteropHResultVariable();
		TypeDefinition keyValuePairTypeDef = _typeRef.Resolve();
		TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(_typeRef);
		FieldReference keyField = typeResolver.Resolve(keyValuePairTypeDef.Fields.Single((FieldDefinition f) => f.Name == "key"));
		FieldReference valueField = typeResolver.Resolve(keyValuePairTypeDef.Fields.Single((FieldDefinition f) => f.Name == "value"));
		TypeDefinition iKeyValuePairTypeDef = _iKeyValuePair.Resolve();
		TypeResolver typeResolver2 = writer.Context.Global.Services.TypeFactory.ResolverFor(_iKeyValuePair);
		MethodReference keyGetter = typeResolver2.Resolve(iKeyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Key"));
		MethodReference valueGetter = typeResolver2.Resolve(iKeyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Value"));
		string cleanVariableName = CleanVariableName(writer.Context, variableName);
		string keyVariableName = cleanVariableName + "KeyNative";
		string valueVariableName = cleanVariableName + "ValueNative";
		string stagingKeyValuePairVariableName = cleanVariableName + "Staging";
		writer.WriteStatement(Emit.NullCheck(variableName));
		writer.WriteLine();
		using (new BlockWriter(writer))
		{
			string imanagedObjectVariableName = cleanVariableName + "_imanagedObject";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"Il2CppIManagedObjectHolder* {imanagedObjectVariableName} = {"NULL"};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {hr} = ({variableName})->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&{imanagedObjectVariableName}));");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IL2CPP_HR_SUCCEEDED({hr}))");
			using (new BlockWriter(writer))
			{
				destinationVariable.WriteStore(writer, $"*static_cast<{_typeRef.CppNameForVariable}*>(UnBox({imanagedObjectVariableName}->GetManagedObject(), {metadataAccess.TypeInfoFor(_typeRef)}))");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{imanagedObjectVariableName}->Release();");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{_typeRef.CppNameForVariable} {stagingKeyValuePairVariableName};");
				_keyMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, keyVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{hr} = ({variableName})->{keyGetter.CppName}(&{keyVariableName});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_raise_exception_if_failed({hr}, false);");
				writer.WriteLine();
				_keyMarshalInfoWriter.WriteMarshalVariableFromNative(writer, keyVariableName, new ManagedMarshalValue(stagingKeyValuePairVariableName, keyField), methodParameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor, metadataAccess);
				writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					bodyWriter.WriteLine();
				}, delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					_keyMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, keyVariableName, metadataAccess);
				}, null);
				writer.WriteLine();
				_valueMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, valueVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{hr} = ({variableName})->{valueGetter.CppName}(&{valueVariableName});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_raise_exception_if_failed({hr}, false);");
				writer.WriteLine();
				_valueMarshalInfoWriter.WriteMarshalVariableFromNative(writer, valueVariableName, new ManagedMarshalValue(stagingKeyValuePairVariableName, valueField), methodParameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor, metadataAccess);
				writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					bodyWriter.WriteLine();
				}, delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					_valueMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, valueVariableName, metadataAccess);
				}, null);
				writer.WriteLine();
				destinationVariable.WriteStore(writer, stagingKeyValuePairVariableName);
			}
		}
	}

	public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
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

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		if (_keyMarshalInfoWriter.CanMarshalTypeFromNative(context))
		{
			return _valueMarshalInfoWriter.CanMarshalTypeFromNative(context);
		}
		return false;
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		if (!_keyMarshalInfoWriter.CanMarshalTypeFromNative(context))
		{
			return _keyMarshalInfoWriter.GetMarshalingException(context, metadataAccess);
		}
		return _valueMarshalInfoWriter.GetMarshalingException(context, metadataAccess);
	}
}
