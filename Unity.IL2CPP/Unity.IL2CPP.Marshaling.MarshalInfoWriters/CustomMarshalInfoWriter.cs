using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public abstract class CustomMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	protected readonly TypeDefinition _type;

	protected readonly MarshalType _marshalType;

	private readonly MethodDefinition _defaultConstructor;

	private readonly bool _forFieldMarshaling;

	private readonly bool _forByReferenceType;

	private readonly bool _forReturnValue;

	private readonly bool _forNativeToManagedWrapper;

	private string _marshaledTypeName;

	private string _marshaledDecoratedTypeName;

	private FieldDefinition[] _fields;

	private DefaultMarshalInfoWriter[] _fieldMarshalInfoWriters;

	private (FieldDefinition, DefaultMarshalInfoWriter)[] _currentTypefieldMarshalInfoWriters;

	private DefaultMarshalInfoWriter[] _baseTypesFieldMarshalInfoWriters;

	public sealed override string MarshalToNativeFunctionName => _type.CppName + "_marshal_" + MarshalingUtils.MarshalTypeToString(_marshalType);

	public sealed override string MarshalFromNativeFunctionName => MarshalToNativeFunctionName + "_back";

	public sealed override string MarshalCleanupFunctionName => MarshalToNativeFunctionName + "_cleanup";

	public sealed override bool HasNativeStructDefinition => true;

	protected string MarshaledTypeName
	{
		get
		{
			if (_marshaledTypeName == null)
			{
				_marshaledTypeName = GetMarshaledTypeName(_type, _marshalType);
			}
			return _marshaledTypeName;
		}
	}

	protected string MarshaledDecoratedTypeName
	{
		get
		{
			if (_marshaledDecoratedTypeName == null)
			{
				_marshaledDecoratedTypeName = (TreatAsValueType() ? MarshaledTypeName : (MarshaledTypeName + "*"));
			}
			return _marshaledDecoratedTypeName;
		}
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(MarshaledTypeName, MarshaledDecoratedTypeName)
		};
	}

	protected FieldDefinition[] GetFields(ReadOnlyContext context)
	{
		if (_fields == null)
		{
			_fields = MarshalingUtils.GetMarshaledFields(context, _type, _marshalType).ToArray();
		}
		return _fields;
	}

	protected DefaultMarshalInfoWriter[] GetFieldMarshalInfoWriters(ReadOnlyContext context)
	{
		if (_fieldMarshalInfoWriters == null)
		{
			_fieldMarshalInfoWriters = (from pair in GetCurrentTypeFieldMarshalInfoWriters(context)
				select pair.Writer).Concat(GetBaseTypesFieldMarshalInfoWriters(context)).ToArray();
		}
		return _fieldMarshalInfoWriters;
	}

	protected (FieldDefinition Field, DefaultMarshalInfoWriter Writer)[] GetCurrentTypeFieldMarshalInfoWriters(ReadOnlyContext context)
	{
		if (_currentTypefieldMarshalInfoWriters == null)
		{
			_currentTypefieldMarshalInfoWriters = (from f in MarshalingUtils.NonStaticFieldsOf(_type)
				select (f: f, MarshalDataCollector.MarshalInfoWriterFor(context, f.FieldType, _marshalType, f.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(_type), forByReferenceType: false, forFieldMarshaling: true))).ToArray();
		}
		return _currentTypefieldMarshalInfoWriters;
	}

	protected DefaultMarshalInfoWriter[] GetBaseTypesFieldMarshalInfoWriters(ReadOnlyContext context)
	{
		if (_baseTypesFieldMarshalInfoWriters == null)
		{
			_baseTypesFieldMarshalInfoWriters = (from f in (from t in _type.GetTypeHierarchy().Skip(1)
					where MarshalDataCollector.MarshalInfoWriterFor(context, t, _marshalType).HasNativeStructDefinition
					select t).SelectMany((TypeDefinition t) => MarshalingUtils.NonStaticFieldsOf(t))
				select MarshalDataCollector.MarshalInfoWriterFor(context, f.FieldType, _marshalType, f.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(_type), forByReferenceType: false, forFieldMarshaling: true)).ToArray();
		}
		return _baseTypesFieldMarshalInfoWriters;
	}

	protected CustomMarshalInfoWriter(TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
		: base(type)
	{
		_type = type;
		_marshalType = marshalType;
		_forFieldMarshaling = forFieldMarshaling;
		_forByReferenceType = forByReferenceType;
		_forReturnValue = forReturnValue;
		_forNativeToManagedWrapper = forNativeToManagedWrapper;
		_defaultConstructor = _type.Methods.SingleOrDefault((MethodDefinition ctor) => ctor.Name == ".ctor" && ctor.Parameters.Count == 0);
	}

	private static string GetMarshaledTypeName(TypeReference type, MarshalType marshalType)
	{
		return type.CppName + "_marshaled_" + MarshalingUtils.MarshalTypeToString(marshalType);
	}

	protected string MarshalCleanupFunctionDeclaration()
	{
		return $"IL2CPP_EXTERN_C void {MarshalCleanupFunctionName}({MarshaledTypeName}& marshaled)";
	}

	protected string MarshalFromNativeFunctionDeclaration()
	{
		return $"IL2CPP_EXTERN_C void {MarshalFromNativeFunctionName}(const {MarshaledTypeName}& marshaled, {_type.CppName}& unmarshaled)";
	}

	protected string MarshalToNativeFunctionDeclaration()
	{
		return $"IL2CPP_EXTERN_C void {MarshalToNativeFunctionName}(const {_type.CppName}& unmarshaled, {MarshaledTypeName}& marshaled)";
	}

	public override bool TreatAsValueType()
	{
		if (!_type.IsValueType)
		{
			if (_type.MetadataType == MetadataType.Class && _marshalType == MarshalType.PInvoke)
			{
				return _forFieldMarshaling;
			}
			return false;
		}
		return true;
	}

	public override void WriteNativeStructDefinition(IReadOnlyContextGeneratedCodeWriter writer)
	{
		TypeReference baseType = _type.BaseType;
		DefaultMarshalInfoWriter baseTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, baseType, _marshalType);
		string baseTypeMarshaledTypeName = GetMarshaledTypeName(baseType, _marshalType);
		if (baseType.IsGenericInstance && baseTypeMarshalInfoWriter is CustomMarshalInfoWriter baseCustomMarshalInfoWriter)
		{
			baseTypeMarshaledTypeName = baseCustomMarshalInfoWriter.MarshaledTypeName;
		}
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("Native definition for " + MarshalingUtils.MarshalTypeToNiceString(_marshalType) + " marshalling of " + _type.FullName);
		}
		(FieldDefinition, DefaultMarshalInfoWriter)[] currentTypeFieldMarshalInfoWriters = GetCurrentTypeFieldMarshalInfoWriters(writer.Context);
		for (int i = 0; i < currentTypeFieldMarshalInfoWriters.Length; i++)
		{
			currentTypeFieldMarshalInfoWriters[i].Item2.WriteIncludesForFieldDeclaration(writer);
		}
		bool needsPackingForWholeStruct = TypeDefinitionWriter.NeedsPackingForNative(_type) && !_type.IsExplicitLayout;
		IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter;
		if (needsPackingForWholeStruct)
		{
			readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"#pragma pack(push, tp, {TypeDefinitionWriter.FieldLayoutPackingSizeFor(_type)})");
		}
		if (_type.HasGenericParameters)
		{
			readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"#ifndef {MarshaledTypeName}_define");
			readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"#define {MarshaledTypeName}_define");
		}
		string baseTypeDeclaration = ((baseType != null && !baseType.IsSpecialSystemBaseType() && baseTypeMarshalInfoWriter.HasNativeStructDefinition) ? (" : public " + baseTypeMarshaledTypeName) : string.Empty);
		readOnlyContextGeneratedCodeWriter = writer;
		readOnlyContextGeneratedCodeWriter.WriteLine($"struct {MarshaledTypeName}{baseTypeDeclaration}");
		writer.BeginBlock();
		using (new TypeDefinitionPaddingWriter(writer, _type))
		{
			if (!_type.IsExplicitLayout)
			{
				currentTypeFieldMarshalInfoWriters = GetCurrentTypeFieldMarshalInfoWriters(writer.Context);
				for (int i = 0; i < currentTypeFieldMarshalInfoWriters.Length; i++)
				{
					(FieldDefinition, DefaultMarshalInfoWriter) fieldAndWriter = currentTypeFieldMarshalInfoWriters[i];
					fieldAndWriter.Item2.WriteFieldDeclaration(writer, fieldAndWriter.Item1);
				}
			}
			else
			{
				writer.WriteLine("union");
				writer.BeginBlock();
				currentTypeFieldMarshalInfoWriters = GetCurrentTypeFieldMarshalInfoWriters(writer.Context);
				for (int i = 0; i < currentTypeFieldMarshalInfoWriters.Length; i++)
				{
					(FieldDefinition, DefaultMarshalInfoWriter) field = currentTypeFieldMarshalInfoWriters[i];
					WriteFieldWithExplicitLayout(writer, field.Item1, field.Item2, forAlignmentOnly: false);
					WriteFieldWithExplicitLayout(writer, field.Item1, field.Item2, forAlignmentOnly: true);
				}
				writer.EndBlock(semicolon: true);
			}
		}
		writer.EndBlock(semicolon: true);
		if (_type.HasGenericParameters)
		{
			writer.WriteLine("#endif");
		}
		if (needsPackingForWholeStruct)
		{
			writer.WriteLine("#pragma pack(pop, tp)");
		}
	}

	public override void WriteFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
	{
		MarshaledType[] marshaledTypes = GetMarshaledTypes(writer.Context);
		foreach (MarshaledType type in marshaledTypes)
		{
			string fieldName = field.CppName + type.VariableName + fieldNameSuffix;
			string alignmentDirective = TypeDefinitionWriter.GetAlignmentDirective(writer.Context, field.FieldType);
			writer.WriteLine((!string.IsNullOrEmpty(alignmentDirective)) ? $"{alignmentDirective} {type.DecoratedName} {fieldName};" : (type.DecoratedName + " " + fieldName + ";"));
		}
	}

	private void WriteFieldWithExplicitLayout(IReadOnlyContextGeneratedCodeWriter writer, FieldDefinition field, DefaultMarshalInfoWriter marshalInfoWriter, bool forAlignmentOnly)
	{
		int alignmentPackingSize = TypeDefinitionWriter.AlignmentPackingSizeFor(_type);
		bool num = (!forAlignmentOnly && TypeDefinitionWriter.NeedsPackingForNative(_type)) || (alignmentPackingSize != -1 && alignmentPackingSize != 0);
		bool needsPackingForClassSize = TypeDefinitionWriter.IsExplicitLayoutWithClassSize(_type);
		string fieldSuffix = (forAlignmentOnly ? "_forAlignmentOnly" : string.Empty);
		int offset = field.Offset;
		if (num)
		{
			IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"#pragma pack(push, tp, {(forAlignmentOnly ? alignmentPackingSize : TypeDefinitionWriter.FieldLayoutPackingSizeFor(_type))})");
		}
		else if (needsPackingForClassSize)
		{
			writer.WriteLine("#pragma pack(push, tp, 1)");
		}
		writer.WriteLine("struct");
		writer.BeginBlock();
		if (offset > 0)
		{
			IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"char {writer.Context.Global.Services.Naming.ForFieldPadding(field) + fieldSuffix}[{offset}];");
		}
		marshalInfoWriter.WriteFieldDeclaration(writer, field, fieldSuffix);
		writer.EndBlock(semicolon: true);
		if (num || needsPackingForClassSize)
		{
			writer.WriteLine("#pragma pack(pop, tp)");
		}
	}

	public override void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
	{
		if (!_type.HasGenericParameters)
		{
			writer.AddForwardDeclaration(_type);
			writer.AddForwardDeclaration("struct " + MarshaledTypeName);
			writer.WriteLine();
			writer.AddForwardDeclaration("struct " + _type.CppName + ";");
			writer.AddForwardDeclaration("struct " + MarshaledTypeName + ";");
			writer.WriteLine();
			writer.AddMethodForwardDeclaration(MarshalToNativeFunctionDeclaration());
			writer.AddMethodForwardDeclaration(MarshalFromNativeFunctionDeclaration());
			writer.AddMethodForwardDeclaration(MarshalCleanupFunctionDeclaration());
		}
	}

	public override bool WillWriteMarshalFunctionDefinitions()
	{
		return !_type.HasGenericParameters;
	}

	public override void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
	{
		if (!_type.HasGenericParameters)
		{
			for (int i = 0; i < GetFields(writer.Context).Length; i++)
			{
				GetFieldMarshalInfoWriters(writer.Context)[i].WriteIncludesForMarshaling(writer);
			}
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine("Conversion methods for marshalling of: " + _type.FullName);
			}
			WriteMarshalToNativeMethodDefinition(writer);
			WriteMarshalFromNativeMethodDefinition(writer);
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine("Conversion method for clean up from marshalling of: " + _type.FullName);
			}
			WriteMarshalCleanupFunction(writer);
			if (_marshalType == MarshalType.PInvoke)
			{
				writer.Context.Global.Collectors.TypeMarshallingFunctions.Add(writer.Context, _type);
			}
		}
	}

	protected abstract void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer);

	protected abstract void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer);

	protected abstract void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer);

	protected static DefaultMarshalInfoWriter MarshalInfoWriterFor(MinimalContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forFieldMarshaling = false)
	{
		return MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType: false, forFieldMarshaling);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		base.WriteIncludesForMarshaling(writer);
		WriteMarshalFunctionDeclarations(writer);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter;
		if (_type.IsValueType)
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalToNativeFunctionName}({sourceVariable.Load(writer.Context)}, {destinationVariable});");
			return;
		}
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			if (_forByReferenceType && _forNativeToManagedWrapper)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"if ({Emit.AddressOf(destinationVariable)} == {"NULL"})");
				using (new BlockWriter(writer))
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{Emit.AddressOf(destinationVariable)} = il2cpp_codegen_marshal_allocate<{MarshaledTypeName}>();");
				}
				writer.WriteLine();
			}
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalToNativeFunctionName}({Emit.Dereference(sourceVariable.Load(writer.Context))}, {destinationVariable});");
		}
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		if (!TreatAsValueType())
		{
			string emptyVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_type.CppNameForVariable} {emptyVariableName} = ({Emit.AddressOf(variableName)} != {"NULL"})");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"    ? {Emit.NewObj(writer.Context, _type, metadataAccess)}");
			writer.WriteLine("    : NULL;");
			return emptyVariableName;
		}
		return base.WriteMarshalEmptyVariableFromNative(writer, variableName, methodParameters, metadataAccess);
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		if (TreatAsValueType())
		{
			if (_type.MetadataType == MetadataType.Class)
			{
				destinationVariable.WriteStore(writer, Emit.NewObj(writer.Context, _type, metadataAccess));
				if (callConstructor)
				{
					EmitCallToConstructor(writer, _type, _defaultConstructor, destinationVariable, metadataAccess);
				}
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{MarshalFromNativeFunctionName}({variableName}, {destinationVariable.Dereferenced.Load(writer.Context)});");
			}
			else
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{MarshalFromNativeFunctionName}({variableName}, {destinationVariable.Load(writer.Context)});");
			}
		}
		else
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({destinationVariable.Load(writer.Context)} != {"NULL"})");
			writer.BeginBlock();
			if (callConstructor)
			{
				EmitCallToConstructor(writer, _type, _defaultConstructor, destinationVariable, metadataAccess);
			}
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalFromNativeFunctionName}({variableName}, *{destinationVariable.Load(writer.Context)});");
			writer.EndBlock();
		}
	}

	public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
	{
		if (TreatAsValueType())
		{
			return base.WriteMarshalReturnValueToNative(writer, sourceVariable, metadataAccess);
		}
		string marshaledVariableName = "_" + sourceVariable.GetNiceName(writer.Context) + "_marshaled";
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{MarshaledDecoratedTypeName} {marshaledVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{marshaledVariableName} = il2cpp_codegen_marshal_allocate<{MarshaledTypeName}>();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalToNativeFunctionName}({Emit.Dereference(sourceVariable.Load(writer.Context))}, {Emit.Dereference(marshaledVariableName)});");
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{marshaledVariableName} = {"NULL"};");
			return marshaledVariableName;
		}
	}

	public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (_type.IsValueType)
		{
			base.WriteDeclareAndAllocateObject(writer, unmarshaledVariableName, marshaledVariableName, metadataAccess);
		}
		else
		{
			EmitNewObject(writer, _type, unmarshaledVariableName, marshaledVariableName, !TreatAsValueType(), metadataAccess);
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter;
		if (TreatAsValueType())
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalCleanupFunctionName}({variableName});");
			return;
		}
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({Emit.AddressOf(variableName)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalCleanupFunctionName}({variableName});");
		}
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		if (TreatAsValueType())
		{
			base.WriteMarshalCleanupOutVariable(writer, variableName, metadataAccess, managedVariableName);
			return;
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({Emit.AddressOf(variableName)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{MarshalCleanupFunctionName}({variableName});");
			if (_forByReferenceType || _forReturnValue)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_marshal_free({Emit.AddressOf(variableName)});");
			}
		}
	}

	public override string DecorateVariable(ReadOnlyContext context, string unmarshaledParameterName, string marshaledVariableName)
	{
		if (!TreatAsValueType())
		{
			if (unmarshaledParameterName == null)
			{
				throw new InvalidOperationException("CustomMarshalInfoWriter does not support decorating return value parameters.");
			}
			if (_forByReferenceType)
			{
				return Emit.AddressOf(marshaledVariableName);
			}
			return $"{unmarshaledParameterName} != {"NULL"} ? {Emit.AddressOf(marshaledVariableName)} : {"NULL"}";
		}
		return marshaledVariableName;
	}

	public override string UndecorateVariable(ReadOnlyContext context, string variableName)
	{
		if (!TreatAsValueType())
		{
			return "*" + variableName;
		}
		return variableName;
	}

	internal static void EmitCallToConstructor(IGeneratedCodeWriter writer, TypeDefinition typeDefinition, MethodDefinition defaultConstructor, ManagedMarshalValue destinationVariable, IRuntimeMetadataAccess metadataAccess)
	{
		if (defaultConstructor != null)
		{
			if (MethodSignatureWriter.NeedsHiddenMethodInfo(writer.Context, defaultConstructor, MethodCallType.Normal, forFullGenericSharing: false))
			{
				IGeneratedCodeWriter generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"{metadataAccess.Method(defaultConstructor)}({destinationVariable.Load(writer.Context)}, {metadataAccess.HiddenMethodInfo(defaultConstructor)});");
			}
			else
			{
				IGeneratedCodeWriter generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"{metadataAccess.Method(defaultConstructor)}({destinationVariable.Load(writer.Context)});");
			}
		}
		else
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"A parameterless constructor is required for type '" + typeDefinition.FullName + "'.\")"));
		}
	}

	internal static void EmitNewObject(IGeneratedCodeWriter writer, TypeReference typeReference, string unmarshaledVariableName, string marshaledVariableName, bool emitNullCheck, IRuntimeMetadataAccess metadataAccess)
	{
		if (emitNullCheck)
		{
			IGeneratedCodeWriter generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{typeReference.CppNameForVariable} {unmarshaledVariableName} = ({marshaledVariableName} != {"NULL"})");
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"    ? {Emit.NewObj(writer.Context, typeReference, metadataAccess)}");
			writer.WriteLine("    : NULL;");
		}
		else
		{
			IGeneratedCodeWriter generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{typeReference.CppNameForVariable} {unmarshaledVariableName} = {Emit.NewObj(writer.Context, typeReference, metadataAccess)};");
		}
	}
}
