using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public abstract class ArrayMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	protected enum ArraySizeOptions
	{
		UseArraySize,
		UseSizeParameterIndex,
		UseFirstMarshaledType
	}

	protected readonly ArrayType _arrayType;

	protected readonly int _arraySize;

	protected readonly int _sizeParameterIndex;

	protected readonly ArraySizeOptions _arraySizeSelection;

	protected readonly TypeReference _elementType;

	protected readonly MarshalInfo _marshalInfo;

	protected readonly MarshalType _marshalType;

	protected readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

	protected readonly NativeType _nativeElementType;

	protected readonly string _arrayMarshaledTypeName;

	private readonly MarshaledType[] _marshaledTypes;

	protected bool NeedsTrailingNullElement
	{
		get
		{
			if (_elementTypeMarshalInfoWriter is StringMarshalInfoWriter)
			{
				return _marshalType != MarshalType.WindowsRuntime;
			}
			return false;
		}
	}

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public override string GetNativeSize(ReadOnlyContext context)
	{
		return "-1";
	}

	protected ArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset = false)
		: base(type)
	{
		_marshalInfo = marshalInfo;
		_marshalType = marshalType;
		_arrayType = type;
		_elementType = type.ElementType;
		MarshalInfo elementTypeMarshalInfo = null;
		ArrayMarshalInfo arrayMarshalInfo = marshalInfo as ArrayMarshalInfo;
		FixedArrayMarshalInfo fixedArrayMarshalInfo = marshalInfo as FixedArrayMarshalInfo;
		_arraySize = 1;
		_nativeElementType = NativeType.None;
		if (arrayMarshalInfo != null)
		{
			_arraySize = arrayMarshalInfo.Size;
			_sizeParameterIndex = arrayMarshalInfo.SizeParameterIndex;
			if (_arraySize == 0 || (_arraySize == -1 && _sizeParameterIndex >= 0))
			{
				_arraySizeSelection = ArraySizeOptions.UseSizeParameterIndex;
			}
			else
			{
				_arraySizeSelection = ArraySizeOptions.UseArraySize;
			}
			_nativeElementType = arrayMarshalInfo.ElementType;
			elementTypeMarshalInfo = new MarshalInfo(_nativeElementType);
		}
		else if (fixedArrayMarshalInfo != null)
		{
			_arraySize = fixedArrayMarshalInfo.Size;
			_nativeElementType = fixedArrayMarshalInfo.ElementType;
			elementTypeMarshalInfo = new MarshalInfo(_nativeElementType);
		}
		if (_arraySize == -1)
		{
			_arraySize = 1;
		}
		_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, marshalType, elementTypeMarshalInfo, useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: true);
		if (_elementTypeMarshalInfoWriter.GetMarshaledTypes(context).Length > 1)
		{
			throw new InvalidOperationException("ArrayMarshalInfoWriter cannot marshal arrays of " + _elementType.FullName + ".");
		}
		_arrayMarshaledTypeName = _elementTypeMarshalInfoWriter.GetMarshaledTypes(context)[0].DecoratedName + "*";
		if (marshalType == MarshalType.WindowsRuntime)
		{
			string indexTypeName = context.Global.Services.TypeProvider.UInt32TypeReference.CppNameForVariable;
			_arraySizeSelection = ArraySizeOptions.UseFirstMarshaledType;
			_marshaledTypes = new MarshaledType[2]
			{
				new MarshaledType(indexTypeName, indexTypeName, "ArraySize"),
				new MarshaledType(_arrayMarshaledTypeName, _arrayMarshaledTypeName)
			};
		}
		else
		{
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_arrayMarshaledTypeName, _arrayMarshaledTypeName)
			};
		}
		if (_elementTypeMarshalInfoWriter is StringMarshalInfoWriter stringMarshalInfoWriter)
		{
			_nativeElementType = stringMarshalInfoWriter.NativeType;
		}
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _arrayType);
		_elementTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		base.WriteIncludesForMarshaling(writer);
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		string marshaledVariableName = "_" + variableName.GetNiceName(writer.Context) + "_marshaled";
		WriteNativeVariableDeclarationOfType(writer, marshaledVariableName);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string arraySize = WriteArraySizeFromManagedArray(writer, variableName, marshaledVariableName);
			string allocationSize = (NeedsTrailingNullElement ? ("(" + arraySize + " + 1)") : arraySize);
			MarshaledType[] elementMarshaledTypes = _elementTypeMarshalInfoWriter.GetMarshaledTypes(writer.Context);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{marshaledVariableName} = il2cpp_codegen_marshal_allocate_array<{elementMarshaledTypes[0].DecoratedName}>({allocationSize});");
			writer.WriteStatement(Emit.Memset(writer.Context, marshaledVariableName, 0, allocationSize + " * sizeof(" + elementMarshaledTypes[0].DecoratedName + ")"));
			return marshaledVariableName;
		}
	}

	public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		if (_marshalType != MarshalType.WindowsRuntime)
		{
			return;
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => MarshaledArraySizeFor(writer.Context, destinationVariable, methodParameters));
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			WriteAssignNullArray(writer, destinationVariable);
		}
	}

	public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		string emptyVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
		ManagedMarshalValue emptyVariable = new ManagedMarshalValue(emptyVariableName);
		writer.WriteVariable(writer.Context, _typeRef, emptyVariableName);
		writer.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string arraySize = MarshaledArraySizeFor(writer.Context, variableName, methodParameters);
			emptyVariable.WriteStore(writer, $"reinterpret_cast<{_arrayType.CppNameForVariable}>({Emit.NewSZArray(writer.Context, _arrayType, arraySize, metadataAccess)})");
			return emptyVariableName;
		}
	}

	private void WriteLoop(IGeneratedMethodCodeWriter outerWriter, Func<IGeneratedMethodCodeWriter, string> writeLoopCountVariable, Action<IGeneratedMethodCodeWriter> writeLoopBody)
	{
		outerWriter.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			string value = writeLoopCountVariable(bodyWriter);
			bodyWriter.WriteLine($"for (int32_t i = 0; i < ARRAY_LENGTH_AS_INT32({value}); i++)");
			bodyWriter.BeginBlock();
		}, writeLoopBody, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.EndBlock();
		});
	}

	protected void WriteMarshalToNativeLoop(IGeneratedMethodCodeWriter outerWriter, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
	{
		WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue(sourceVariable, "i"), _elementTypeMarshalInfoWriter.UndecorateVariable(bodyWriter.Context, "(" + destinationVariable + ")[i]"), managedVariableName, metadataAccess);
		});
	}

	protected void WriteMarshalFromNativeLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
	{
		WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			string variableName2 = _elementTypeMarshalInfoWriter.UndecorateVariable(bodyWriter.Context, "(" + variableName + ")[i]");
			string value = _elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, variableName2, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess);
			bodyWriter.WriteStatement(Emit.StoreArrayElement(destinationVariable.Load(outerWriter.Context), "i", value, useArrayBoundsCheck: false));
		});
	}

	protected void WriteCleanupLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
	{
		WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, _elementTypeMarshalInfoWriter.UndecorateVariable(bodyWriter.Context, "(" + variableName + ")[i]"), metadataAccess);
		});
	}

	protected void WriteCleanupOutVariableLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
	{
		WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalCleanupOutVariable(bodyWriter, _elementTypeMarshalInfoWriter.UndecorateVariable(bodyWriter.Context, "(" + variableName + ")[i]"), metadataAccess);
		});
	}

	protected void AllocateAndStoreManagedArray(ICodeWriter writer, ManagedMarshalValue destinationVariable, IRuntimeMetadataAccess metadataAccess, string arraySizeVariable)
	{
		destinationVariable.WriteStore(writer, $"reinterpret_cast<{_arrayType.CppNameForVariable}>({Emit.NewSZArray(writer.Context, _arrayType, arraySizeVariable, metadataAccess)})");
	}

	protected void AllocateAndStoreNativeArray(ICodeWriter writer, string destinationVariable, string arraySize)
	{
		if (NeedsTrailingNullElement)
		{
			ICodeWriter codeWriter = writer;
			codeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_marshal_allocate_array<{_elementTypeMarshalInfoWriter.GetMarshaledTypes(writer.Context)[0].DecoratedName}>({arraySize} + 1);");
			codeWriter = writer;
			codeWriter.WriteLine($"({destinationVariable})[{arraySize}] = {"NULL"};");
		}
		else
		{
			ICodeWriter codeWriter = writer;
			codeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_marshal_allocate_array<{_elementTypeMarshalInfoWriter.GetMarshaledTypes(writer.Context)[0].DecoratedName}>({arraySize});");
		}
	}

	protected void WriteAssignNullArray(ICodeWriter writer, string destinationVariable)
	{
		ICodeWriter codeWriter;
		if (_arraySizeSelection == ArraySizeOptions.UseFirstMarshaledType)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"{destinationVariable}{_marshaledTypes[0].VariableName} = 0;");
		}
		codeWriter = writer;
		codeWriter.WriteLine($"{destinationVariable} = {"NULL"};");
	}

	protected string WriteArraySizeFromManagedArray(IGeneratedCodeWriter writer, ManagedMarshalValue managedArray, string nativeArray)
	{
		string variableName;
		IGeneratedCodeWriter generatedCodeWriter;
		if (_arraySizeSelection != ArraySizeOptions.UseFirstMarshaledType)
		{
			variableName = "_" + managedArray.GetNiceName(writer.Context) + "_Length";
			generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"{"il2cpp_array_size_t"} {variableName} = ({managedArray.Load(writer.Context)})->max_length;");
			return variableName;
		}
		variableName = nativeArray + _marshaledTypes[0].VariableName;
		generatedCodeWriter = writer;
		generatedCodeWriter.WriteLine($"{variableName} = static_cast<uint32_t>(({managedArray.Load(writer.Context)})->max_length);");
		return "static_cast<int32_t>(" + variableName + ")";
	}

	public abstract override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess);

	public abstract override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess);

	public abstract override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess);

	public abstract override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null);

	protected string MarshaledArraySizeFor(ReadOnlyContext context, string nativeArray, IList<MarshaledParameter> methodParameters)
	{
		switch (_arraySizeSelection)
		{
		case ArraySizeOptions.UseArraySize:
			return _arraySize.ToString();
		case ArraySizeOptions.UseSizeParameterIndex:
		{
			if (methodParameters == null)
			{
				return _arraySize.ToString();
			}
			MarshaledParameter sizeParameter = methodParameters[_sizeParameterIndex];
			if (sizeParameter.ParameterType.MetadataType != MetadataType.Int32)
			{
				if (sizeParameter.ParameterType.MetadataType == MetadataType.ByReference)
				{
					return "static_cast<int32_t>(" + Emit.Dereference(sizeParameter.NameInGeneratedCode) + ")";
				}
				return "static_cast<int32_t>(" + sizeParameter.NameInGeneratedCode + ")";
			}
			return sizeParameter.NameInGeneratedCode;
		}
		case ArraySizeOptions.UseFirstMarshaledType:
			return "static_cast<int32_t>(" + nativeArray + GetMarshaledTypes(context)[0].VariableName + ")";
		default:
			throw new InvalidOperationException($"Unknown ArraySizeOptions: {_arraySizeSelection}");
		}
	}

	public override bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return _elementTypeMarshalInfoWriter.CanMarshalTypeToNative(context);
	}

	public override bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return _elementTypeMarshalInfoWriter.CanMarshalTypeFromNative(context);
	}

	public override string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		return _elementTypeMarshalInfoWriter.GetMarshalingException(context, metadataAccess);
	}
}
