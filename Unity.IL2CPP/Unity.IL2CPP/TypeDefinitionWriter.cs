using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public class TypeDefinitionWriter
{
	public enum FieldType
	{
		Instance,
		Static,
		ThreadStatic
	}

	private struct FieldWriteInstruction
	{
		public FieldDefinition Field { get; private set; }

		public string FieldName { get; private set; }

		public string FieldTypeName { get; private set; }

		public TypeReference FieldType { get; private set; }

		public FieldWriteInstruction(ReadOnlyContext context, FieldDefinition field, string fieldTypeName, TypeReference fieldType)
		{
			this = default(FieldWriteInstruction);
			Field = field;
			FieldName = field.CppName;
			FieldTypeName = fieldTypeName;
			FieldType = fieldType;
		}
	}

	private struct ComFieldWriteInstruction
	{
		public TypeReference InterfaceType { get; private set; }

		public ComFieldWriteInstruction(TypeReference interfaceType)
		{
			this = default(ComFieldWriteInstruction);
			InterfaceType = interfaceType;
		}
	}

	[Flags]
	private enum AlignmentType
	{
		None = 0,
		PointerSize = 1,
		EightBytes = 2,
		FourBytes = 4,
		TwoBytes = 8,
		OneByte = 0x10
	}

	private static readonly Func<InflatedFieldType, bool> IsInstanceField = (InflatedFieldType fd) => !fd.Field.IsStatic;

	private static readonly Func<InflatedFieldType, bool> IsNormalStaticField = (InflatedFieldType fd) => fd.Field.IsNormalStatic;

	private static readonly Func<InflatedFieldType, bool> IsThreadStaticField = (InflatedFieldType fd) => fd.Field.IsThreadStatic;

	private const char kArrayFirstIndexName = 'i';

	public static void WriteTypeDefinitionFor(ReadOnlyContext context, TypeReference type, IReadOnlyContextGeneratedCodeWriter writer, FieldType fieldType, out TypeReference[] typesRequiringInteropGuids)
	{
		TypeDefinition typeDefinition = type.Resolve();
		typesRequiringInteropGuids = null;
		if ((!(type is TypeDefinition) || !type.HasGenericParameters) && !type.IsFunctionPointer && !type.IsSystemObject && !type.IsSystemArray && !type.Is(Il2CppCustomType.Il2CppComObject) && !type.IsIl2CppFullySharedGenericType)
		{
			context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
			if (context.Global.Parameters.EnableErrorMessageTest)
			{
				ErrorTypeAndMethod.ThrowIfIsErrorType(context, type.Resolve());
			}
			VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinition(typeDefinition);
			CollectIncludes(writer, type, typeDefinition, fieldType);
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteLine();
				writer.WriteCommentedLine(type.FullName);
			}
			if (fieldType == FieldType.Instance)
			{
				WriteInstanceDefinition(context, type, writer, ref typesRequiringInteropGuids, typeDefinition);
			}
			if (fieldType == FieldType.Static && (typeDefinition.Fields.Any((FieldDefinition f) => f.IsNormalStatic) || typeDefinition.StoresNonFieldsInStaticFields()) && type.GetRuntimeStaticFieldLayout(context) == RuntimeFieldLayoutKind.Fixed)
			{
				WriteDefinition(context, writer, type, context.Global.Services.Naming.ForStaticFieldsStruct(context, type), FieldType.Static);
			}
			if (fieldType == FieldType.ThreadStatic && typeDefinition.Fields.Any((FieldDefinition f) => f.IsThreadStatic) && type.GetRuntimeStaticFieldLayout(context) == RuntimeFieldLayoutKind.Fixed)
			{
				WriteDefinition(context, writer, type, context.Global.Services.Naming.ForThreadFieldsStruct(context, type), FieldType.ThreadStatic);
			}
		}
	}

	private static void WriteInstanceDefinition(ReadOnlyContext context, TypeReference type, IReadOnlyContextGeneratedCodeWriter writer, ref TypeReference[] typesRequiringInteropGuids, TypeDefinition typeDefinition)
	{
		IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter;
		if (type.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable)
		{
			if (type.IsValueType)
			{
				readOnlyContextGeneratedCodeWriter = writer;
				readOnlyContextGeneratedCodeWriter.WriteLine($"typedef {"Il2CppFullySharedGenericStruct"} {type.CppName};");
			}
			else
			{
				TypeReference nonSharedBaseType = BaseTypeHelper.GetFirstNonVariableLayoutBaseType(context, type);
				readOnlyContextGeneratedCodeWriter = writer;
				readOnlyContextGeneratedCodeWriter.WriteLine($"struct {type.CppName} : public {nonSharedBaseType.CppName} {{}};");
			}
			return;
		}
		bool isUnmanagedType = MarshalingUtils.IsBlittable(context, type, null, MarshalType.ManagedLayout, useUnicodeCharset: true);
		bool num = !typeDefinition.IsExplicitLayout && NeedsPackingForManaged(typeDefinition, isUnmanagedType);
		bool needsPackingForClassSize = isUnmanagedType && IsExplicitLayoutWithClassSize(typeDefinition);
		if (num)
		{
			readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"#pragma pack(push, tp, {AlignmentPackingSizeFor(typeDefinition)})");
		}
		else if (needsPackingForClassSize)
		{
			writer.WriteLine("#pragma pack(push, tp, 1)");
		}
		readOnlyContextGeneratedCodeWriter = writer;
		readOnlyContextGeneratedCodeWriter.WriteLine($"struct {type.CppName} {GetBaseTypeDeclaration(context, type)}");
		writer.BeginBlock();
		WriteGuid(writer, type, out typesRequiringInteropGuids);
		WriteFieldsWithAccessors(context, writer, type, isUnmanagedType);
		writer.EndBlock(semicolon: true);
		if (num || needsPackingForClassSize)
		{
			writer.WriteLine("#pragma pack(pop, tp)");
		}
		WriteNativeStructDefinitions(type, writer);
	}

	private static void WriteDefinition(ReadOnlyContext context, IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, string structName, FieldType fieldType)
	{
		writer.WriteLine($"struct {structName}");
		writer.BeginBlock();
		WriteFieldsWithAccessors(context, writer, type, isUnmanagedType: false, fieldType);
		writer.EndBlock(semicolon: true);
	}

	internal static bool IsExplicitLayoutWithClassSize(TypeDefinition typeDefinition)
	{
		if (typeDefinition.IsExplicitLayout)
		{
			return typeDefinition.ClassSize > 0;
		}
		return false;
	}

	private static void VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinition(TypeDefinition typeDefinition)
	{
		VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(typeDefinition, new HashSet<TypeDefinition>());
	}

	private static void VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(TypeDefinition typeDefinition, HashSet<TypeDefinition> parentTypes)
	{
		if (typeDefinition == null || parentTypes.Contains(typeDefinition) || !typeDefinition.IsValueType)
		{
			return;
		}
		foreach (FieldDefinition field in typeDefinition.Fields)
		{
			parentTypes.Add(typeDefinition);
			if (field.IsStatic && field.FieldType.IsNullableGenericInstance)
			{
				GenericInstanceType nullableStaticFieldType = (GenericInstanceType)field.FieldType;
				if (parentTypes.Contains(nullableStaticFieldType.GenericArguments[0]))
				{
					throw new NotSupportedException($"The type '{typeDefinition}' contains a static field which has a type of '{field.FieldType}'. IL2CPP does not support conversion of this recursively defined type.");
				}
			}
			else
			{
				VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(field.FieldType.Resolve(), parentTypes);
			}
			parentTypes.Remove(typeDefinition);
		}
	}

	public static void WriteArrayTypeDefinition(ReadOnlyContext context, ArrayType type, ICodeWriter writer)
	{
		context.Global.Services.ErrorInformation.CurrentType = type.Resolve();
		if (context.Global.Parameters.EnableErrorMessageTest)
		{
			ErrorTypeAndMethod.ThrowIfIsErrorType(context, type.Resolve());
		}
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(type.FullName);
		}
		writer.WriteLine($"struct {type.CppName} {GetBaseTypeDeclaration(context, type)}");
		writer.BeginBlock();
		WriteArrayFieldsWithAccessors(writer, type);
		writer.EndBlock(semicolon: true);
	}

	private static void WriteNativeStructDefinitions(TypeReference type, IReadOnlyContextGeneratedCodeWriter writer)
	{
		MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(writer.Context, type);
		foreach (MarshalType marshalType in marshalTypesForMarshaledType)
		{
			MarshalDataCollector.MarshalInfoWriterFor(writer.Context, type, marshalType, null, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(type)).WriteNativeStructDefinition(writer);
		}
	}

	private static void CollectIncludes(IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, TypeDefinition typeDefinition, FieldType fieldTypeFilter)
	{
		if (type.HasGenericParameters)
		{
			return;
		}
		if (type is ArrayType)
		{
			writer.AddIncludeForTypeDefinition(writer.Context, writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.Array));
		}
		else
		{
			if (type is FunctionPointerType)
			{
				return;
			}
			if (fieldTypeFilter == FieldType.Instance)
			{
				TypeReference baseType = type.GetBaseType(writer.Context);
				if (baseType != null)
				{
					writer.AddIncludeForTypeDefinition(writer.Context, baseType);
				}
			}
			foreach (InflatedFieldType fieldType in type.GetInflatedFieldTypes(writer.Context).Where(GetFieldFilter(fieldTypeFilter)))
			{
				if (fieldType.InflatedType != type)
				{
					if (fieldType.InflatedType is PointerType pointerType)
					{
						writer.AddForwardDeclaration(pointerType.ElementType);
					}
					else
					{
						writer.AddIncludesForTypeReference(writer.Context, fieldType.InflatedType);
					}
				}
			}
			foreach (TypeReference activationFactoryType in type.GetAllFactoryTypes(writer.Context))
			{
				writer.AddForwardDeclaration(activationFactoryType);
			}
			if (!typeDefinition.IsDelegate)
			{
				return;
			}
			List<MethodReference> delegateMethods = new List<MethodReference>(3);
			LazilyInflatedMethod[] methods = type.IterateLazilyInflatedMethods(writer.Context).ToArray();
			delegateMethods.Add(methods.Single((LazilyInflatedMethod m) => m.Name == "Invoke").InflatedMethod);
			LazilyInflatedMethod[] array = methods;
			for (int i = 0; i < array.Length; i++)
			{
				LazilyInflatedMethod method = array[i];
				if (method.Name == "BeginInvoke" || method.Name == "EndInvoke")
				{
					delegateMethods.Add(method.InflatedMethod);
				}
			}
			foreach (MethodReference method2 in delegateMethods)
			{
				writer.AddIncludesForTypeReference(writer.Context, method2.GetResolvedReturnType(writer.Context.Global.Services.TypeFactory));
				foreach (ParameterDefinition resolvedParameter in method2.GetResolvedParameters(writer.Context.Global.Services.TypeFactory))
				{
					TypeReference parameterType = resolvedParameter.ParameterType;
					writer.AddIncludesForTypeReference(writer.Context, parameterType);
					if (parameterType.IsByReference)
					{
						ByReferenceType byRefType = (ByReferenceType)parameterType;
						if (byRefType.ElementType.IsValueType)
						{
							parameterType = byRefType.ElementType;
						}
					}
					if (parameterType.IsValueType)
					{
						writer.AddIncludeForTypeDefinition(writer.Context, parameterType);
					}
				}
			}
		}
	}

	private static bool NeedsPackingForManaged(TypeDefinition typeDefinition, bool isUnmanaged)
	{
		if (isUnmanaged)
		{
			return NeedsPacking(typeDefinition);
		}
		return false;
	}

	internal static bool NeedsPackingForNative(TypeDefinition typeDefinition)
	{
		if (!NeedsPacking(typeDefinition))
		{
			return typeDefinition.IsExplicitLayout;
		}
		return true;
	}

	private static bool NeedsPacking(TypeDefinition typeDefinition)
	{
		if ((typeDefinition.IsSequentialLayout || typeDefinition.IsExplicitLayout) && typeDefinition.PackingSize != 0)
		{
			return typeDefinition.PackingSize != -1;
		}
		return false;
	}

	internal static int FieldLayoutPackingSizeFor(TypeDefinition typeDefinition)
	{
		if (typeDefinition.IsExplicitLayout)
		{
			return 1;
		}
		return typeDefinition.PackingSize;
	}

	internal static int AlignmentPackingSizeFor(TypeDefinition typeDefinition)
	{
		return typeDefinition.PackingSize;
	}

	private static void WriteGuid(IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, out TypeReference[] typesRequiringInteropGuids)
	{
		if (!type.HasCLSID() && !type.HasIID(writer.Context))
		{
			typesRequiringInteropGuids = null;
			return;
		}
		string variableName = (type.HasCLSID() ? "CLSID" : "IID");
		writer.WriteLine($"static const Il2CppGuid {variableName};");
		writer.WriteLine();
		typesRequiringInteropGuids = new TypeReference[1] { type };
	}

	private static Func<InflatedFieldType, bool> GetFieldFilter(FieldType fieldType)
	{
		return fieldType switch
		{
			FieldType.Instance => IsInstanceField, 
			FieldType.Static => IsNormalStaticField, 
			FieldType.ThreadStatic => IsThreadStaticField, 
			_ => throw new NotImplementedException($"Unimplemented field type {fieldType}"), 
		};
	}

	private static void WriteFieldsWithAccessors(ReadOnlyContext context, IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, bool isUnmanagedType, FieldType fieldType = FieldType.Instance)
	{
		TypeDefinition typeDefinition = type.Resolve();
		List<FieldWriteInstruction> fieldWriteInstructions = MakeFieldWriteInstructionsForType(context, type, GetFieldFilter(fieldType));
		List<ComFieldWriteInstruction> comFieldWriteInstructions = MakeComFieldWriteInstructionsForType(context, type, typeDefinition, fieldType);
		if (fieldType == FieldType.Instance)
		{
			using (new TypeDefinitionPaddingWriter(writer, typeDefinition))
			{
				WriteFields(writer, typeDefinition, isUnmanagedType, fieldType, fieldWriteInstructions, comFieldWriteInstructions);
			}
		}
		else
		{
			WriteFields(writer, typeDefinition, isUnmanagedType, fieldType, fieldWriteInstructions, comFieldWriteInstructions);
		}
		WriteComFieldGetters(writer, type, comFieldWriteInstructions);
	}

	private static void WriteArrayFieldsWithAccessors(ICodeWriter writer, ArrayType arrayType)
	{
		TypeReference elementType = arrayType.ElementType;
		bool isFullySharedGeneric = elementType.GetRuntimeStorage(writer.Context).IsVariableSized();
		string elementTypeName = (isFullySharedGeneric ? "uint8_t" : elementType.CppNameForVariable);
		writer.WriteLine($"ALIGN_FIELD (8) {elementTypeName} {ArrayNaming.ForArrayItems()}[1];");
		writer.WriteLine();
		Func<string, string> arrayIndexExpression = (isFullySharedGeneric ? ((Func<string, string>)((string name) => "il2cpp_array_calc_byte_offset(this, " + name + ")")) : ((Func<string, string>)((string name) => name)));
		WriteArrayAccessors(writer, arrayType, elementType, elementTypeName, arrayIndexExpression, isFullySharedGeneric, emitArrayBoundsCheck: true);
		WriteArrayAccessors(writer, arrayType, elementType, elementTypeName, arrayIndexExpression, isFullySharedGeneric, emitArrayBoundsCheck: false);
		if (arrayType.Rank > 1)
		{
			WriteArrayAccessorsForMultiDimensionalArray(writer, arrayType.Rank, elementType, elementTypeName, arrayIndexExpression, isFullySharedGeneric, emitArrayBoundsCheck: true);
			WriteArrayAccessorsForMultiDimensionalArray(writer, arrayType.Rank, elementType, elementTypeName, arrayIndexExpression, isFullySharedGeneric, emitArrayBoundsCheck: false);
		}
	}

	private static string AccessItems(ReadOnlyContext context, TypeReference elementType)
	{
		return ArrayNaming.ForArrayItems();
	}

	private static void WriteArrayAccessors(ICodeWriter writer, ArrayType arrayType, TypeReference elementType, string elementTypeName, Func<string, string> arrayIndexExpression, bool isFullySharedGeneric, bool emitArrayBoundsCheck)
	{
		string boundsCheck = (arrayType.IsVector ? Emit.ArrayBoundsCheck("this", "index") : Emit.MultiDimensionalArrayBoundsCheck(writer.Context, "this", "index", arrayType.Rank));
		ICodeWriter codeWriter;
		if (!isFullySharedGeneric)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"inline {elementTypeName} {ArrayNaming.ForArrayItemGetter(emitArrayBoundsCheck)}({ArrayNaming.ForArrayIndexType()} {ArrayNaming.ForArrayIndexName()}) const");
			using (new BlockWriter(writer))
			{
				if (emitArrayBoundsCheck)
				{
					writer.WriteLine(boundsCheck);
				}
				codeWriter = writer;
				codeWriter.WriteLine($"return {AccessItems(writer.Context, elementType)}[{arrayIndexExpression(ArrayNaming.ForArrayIndexName())}];");
			}
		}
		codeWriter = writer;
		codeWriter.WriteLine($"inline {elementTypeName}* {ArrayNaming.ForArrayItemAddressGetter(emitArrayBoundsCheck)}({ArrayNaming.ForArrayIndexType()} {ArrayNaming.ForArrayIndexName()})");
		using (new BlockWriter(writer))
		{
			if (emitArrayBoundsCheck)
			{
				writer.WriteLine(boundsCheck);
			}
			codeWriter = writer;
			codeWriter.WriteLine($"return {AccessItems(writer.Context, elementType)} + {arrayIndexExpression(ArrayNaming.ForArrayIndexName())};");
		}
		if (isFullySharedGeneric)
		{
			return;
		}
		codeWriter = writer;
		codeWriter.WriteLine($"inline void {ArrayNaming.ForArrayItemSetter(emitArrayBoundsCheck)}({ArrayNaming.ForArrayIndexType()} {ArrayNaming.ForArrayIndexName()}, {elementTypeName} value)");
		using (new BlockWriter(writer))
		{
			if (emitArrayBoundsCheck)
			{
				writer.WriteLine(boundsCheck);
			}
			codeWriter = writer;
			codeWriter.WriteLine($"{AccessItems(writer.Context, elementType)}[{arrayIndexExpression(ArrayNaming.ForArrayIndexName())}] = value;");
			writer.WriteWriteBarrierIfNeeded(elementType, $"{AccessItems(writer.Context, elementType)} + {ArrayNaming.ForArrayIndexName()}", "value");
		}
	}

	private static void WriteArrayAccessorsForMultiDimensionalArray(ICodeWriter writer, int rank, TypeReference elementType, string elementTypeName, Func<string, string> arrayIndexExpression, bool isFullySharedGeneric, bool emitArrayBoundsCheck)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string indexParameters = BuildArrayIndexParameters(stringBuilder, rank);
		string indexCalculation = BuildArrayIndexCalculation(stringBuilder, rank, arrayIndexExpression);
		string boundsVariables = BuildArrayBoundsVariables(writer.Context, stringBuilder, rank, emitArrayBoundsCheck, writer.IndentationLevel + 1);
		ICodeWriter codeWriter;
		if (!isFullySharedGeneric)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"inline {elementTypeName} {ArrayNaming.ForArrayItemGetter(emitArrayBoundsCheck)}({indexParameters}) const");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(boundsVariables);
				writer.WriteLine(indexCalculation);
				codeWriter = writer;
				codeWriter.WriteLine($"return {AccessItems(writer.Context, elementType)}[{ArrayNaming.ForArrayIndexName()}];");
			}
		}
		codeWriter = writer;
		codeWriter.WriteLine($"inline {elementTypeName}* {ArrayNaming.ForArrayItemAddressGetter(emitArrayBoundsCheck)}({indexParameters})");
		using (new BlockWriter(writer))
		{
			writer.WriteLine(boundsVariables);
			writer.WriteLine(indexCalculation);
			codeWriter = writer;
			codeWriter.WriteLine($"return {AccessItems(writer.Context, elementType)} + {ArrayNaming.ForArrayIndexName()};");
		}
		if (!isFullySharedGeneric)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"inline void {ArrayNaming.ForArrayItemSetter(emitArrayBoundsCheck)}({indexParameters}, {elementTypeName} value)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(boundsVariables);
				writer.WriteLine(indexCalculation);
				codeWriter = writer;
				codeWriter.WriteLine($"{AccessItems(writer.Context, elementType)}[{ArrayNaming.ForArrayIndexName()}] = value;");
				writer.WriteWriteBarrierIfNeeded(elementType, $"{AccessItems(writer.Context, elementType)} + {ArrayNaming.ForArrayIndexName()}", "value");
			}
		}
	}

	private static string BuildArrayIndexParameters(StringBuilder stringBuilder, int rank)
	{
		stringBuilder.Clear();
		char endName = (char)(105 + rank);
		for (char indexName = 'i'; indexName < endName; indexName = (char)(indexName + 1))
		{
			stringBuilder.AppendFormat("{0} {1}", ArrayNaming.ForArrayIndexType(), indexName);
			if (indexName != endName - 1)
			{
				stringBuilder.Append(", ");
			}
		}
		return stringBuilder.ToString();
	}

	private static string BuildArrayBoundsVariables(ReadOnlyContext context, StringBuilder stringBuilder, int rank, bool emitArrayBoundsCheck, int indentationLevel)
	{
		stringBuilder.Clear();
		string indentation = new string('\t', indentationLevel);
		bool needsIndentation = false;
		for (int i = 0; i < rank; i++)
		{
			if (i != 0 || emitArrayBoundsCheck)
			{
				string boundVariableName = BoundVariableNameFor(i);
				if (needsIndentation)
				{
					stringBuilder.Append(indentation);
				}
				stringBuilder.AppendFormat("{0} {1} = bounds[{2}].length;{3}", ArrayNaming.ForArrayIndexType(), boundVariableName, i, "\n");
				if (emitArrayBoundsCheck)
				{
					stringBuilder.AppendFormat("{0}{1}{2}", indentation, Emit.MultiDimensionalArrayBoundsCheck(boundVariableName, ((char)(105 + i)).ToString()), "\n");
				}
				needsIndentation = true;
			}
		}
		return stringBuilder.ToString();
	}

	private static string BoundVariableNameFor(int i)
	{
		return $"{(char)(105 + i)}Bound";
	}

	private static string BuildArrayIndexCalculation(StringBuilder stringBuilder, int rank, Func<string, string> arrayIndexCalculation)
	{
		stringBuilder.Clear();
		for (int i = 0; i < rank - 2; i++)
		{
			stringBuilder.Append('(');
		}
		for (int j = 0; j < rank; j++)
		{
			stringBuilder.Append((char)(105 + j));
			if (j != 0 && j != rank - 1)
			{
				stringBuilder.Append(')');
			}
			if (j != rank - 1)
			{
				stringBuilder.AppendFormat(" * {0} + ", BoundVariableNameFor(j + 1));
			}
		}
		string calculation = stringBuilder.ToString();
		stringBuilder.Clear();
		stringBuilder.AppendFormat("{0} {1} = ", ArrayNaming.ForArrayIndexType(), ArrayNaming.ForArrayIndexName());
		stringBuilder.Append(arrayIndexCalculation(calculation));
		stringBuilder.Append(';');
		return stringBuilder.ToString();
	}

	private static List<FieldWriteInstruction> MakeFieldWriteInstructionsForType(ReadOnlyContext context, TypeReference type, Func<InflatedFieldType, bool> fieldFilter)
	{
		List<FieldWriteInstruction> instructions = new List<FieldWriteInstruction>();
		foreach (InflatedFieldType item in type.GetInflatedFieldTypes(context).Where(fieldFilter))
		{
			FieldDefinition field = item.Field;
			TypeReference fieldTypeReference = item.InflatedType;
			string typeName = fieldTypeReference.CppNameForVariable;
			instructions.Add(new FieldWriteInstruction(context, field, typeName, fieldTypeReference));
		}
		return instructions;
	}

	private static List<ComFieldWriteInstruction> MakeComFieldWriteInstructionsForType(ReadOnlyContext context, TypeReference type, TypeDefinition typeDefinition, FieldType fieldType)
	{
		if (fieldType != FieldType.Static || !typeDefinition.IsComOrWindowsRuntimeType() || !type.DerivesFrom(context, context.Global.Services.TypeProvider.Il2CppComObjectTypeReference, checkInterfaces: false))
		{
			return new List<ComFieldWriteInstruction>();
		}
		TypeReference[] array = type.GetAllFactoryTypes(context).ToArray();
		List<ComFieldWriteInstruction> result = new List<ComFieldWriteInstruction>(array.Length);
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(type);
		bool hasIActivationFactory = false;
		TypeReference[] array2 = array;
		foreach (TypeReference iface in array2)
		{
			if (iface.Is(Il2CppCustomType.IActivationFactory))
			{
				hasIActivationFactory = true;
			}
			result.Add(new ComFieldWriteInstruction(typeResolver.Resolve(iface)));
		}
		if (!hasIActivationFactory && result.Count > 0)
		{
			result.Insert(0, new ComFieldWriteInstruction(context.Global.Services.TypeProvider.IActivationFactoryTypeReference));
		}
		return result;
	}

	private static void WriteFields(ICppCodeWriter writer, TypeDefinition typeDefinition, bool isUnmanagedType, FieldType fieldType, List<FieldWriteInstruction> fieldWriteInstructions, List<ComFieldWriteInstruction> comFieldWriteInstructions)
	{
		bool explicitLayout = typeDefinition.IsExplicitLayout && fieldType == FieldType.Instance;
		if (explicitLayout)
		{
			writer.WriteLine("union");
			writer.BeginBlock();
		}
		foreach (FieldWriteInstruction instruction in fieldWriteInstructions)
		{
			WriteFieldInstruction(writer, typeDefinition, isUnmanagedType, explicitLayout, instruction);
			if (explicitLayout)
			{
				WriteFieldInstruction(writer, typeDefinition, isUnmanagedType, explicitLayout: true, instruction, forAlignmentOnly: true);
			}
		}
		if (explicitLayout)
		{
			writer.EndBlock(semicolon: true);
		}
		foreach (ComFieldWriteInstruction instruction2 in comFieldWriteInstructions)
		{
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine($"Cached pointer to {instruction2.InterfaceType.FullName}");
			}
			writer.WriteLine($"{instruction2.InterfaceType.CppName}* {writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(instruction2.InterfaceType)};");
		}
	}

	private static void WriteFieldInstruction(ICppCodeWriter writer, TypeDefinition typeDefinition, bool isUnmanagedType, bool explicitLayout, FieldWriteInstruction instruction, bool forAlignmentOnly = false)
	{
		int alignmentPackingSize = AlignmentPackingSizeFor(typeDefinition);
		string fieldSuffix = (forAlignmentOnly ? "_forAlignmentOnly" : string.Empty);
		bool shouldWritePragma = !forAlignmentOnly || NeedsPackingForManaged(typeDefinition, isUnmanagedType);
		if (explicitLayout)
		{
			if (shouldWritePragma)
			{
				ICppCodeWriter cppCodeWriter = writer;
				cppCodeWriter.WriteLine($"#pragma pack(push, tp, {(forAlignmentOnly ? alignmentPackingSize : FieldLayoutPackingSizeFor(typeDefinition))})");
			}
			writer.WriteLine("struct");
			writer.BeginBlock();
			int offset = instruction.Field.Offset;
			if (offset > 0)
			{
				ICppCodeWriter cppCodeWriter = writer;
				cppCodeWriter.WriteLine($"char {writer.Context.Global.Services.Naming.ForFieldPadding(instruction.Field) + fieldSuffix}[{offset}];");
			}
		}
		if (!forAlignmentOnly && writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(instruction.Field.FullName);
		}
		string alignmentDirective = string.Empty;
		if (isUnmanagedType && !explicitLayout)
		{
			alignmentDirective = GetAlignmentDirective(writer.Context, instruction.FieldType);
		}
		writer.WriteStatement((!string.IsNullOrEmpty(alignmentDirective)) ? $"{alignmentDirective} {instruction.FieldTypeName} {instruction.FieldName + fieldSuffix}" : (instruction.FieldTypeName + " " + instruction.FieldName + fieldSuffix));
		if (explicitLayout)
		{
			writer.EndBlock(semicolon: true);
			if (shouldWritePragma)
			{
				writer.WriteLine("#pragma pack(pop, tp)");
			}
		}
	}

	internal static string GetAlignmentDirective(ReadOnlyContext context, TypeReference fieldType)
	{
		TypeDefinition fieldTypeDefinition = fieldType.Resolve();
		if (fieldTypeDefinition == null)
		{
			return string.Empty;
		}
		if (!IsExplicitLayoutWithClassSize(fieldTypeDefinition))
		{
			return string.Empty;
		}
		string alignmentValue = ((HasDefaultPackingSize(fieldTypeDefinition) || fieldType.IsPointer) ? ChooseAlignmentForDefaultPacking(context, fieldTypeDefinition, fieldType.IsPointer) : fieldTypeDefinition.PackingSize.ToString());
		return "alignas(" + alignmentValue + ")";
	}

	private static string ChooseAlignmentForDefaultPacking(ReadOnlyContext context, TypeDefinition typeDefinition, bool isPointer)
	{
		if (!typeDefinition.IsValueType || isPointer)
		{
			return "IL2CPP_SIZEOF_VOID_P";
		}
		HashSet<TypeReference> visitedTypes = new HashSet<TypeReference>();
		AlignmentType alignment = ChooseAlignmentForDefaultPackingRecursive(context, typeDefinition, visitedTypes);
		if (alignment.HasFlag(AlignmentType.EightBytes))
		{
			return "8";
		}
		if (alignment.HasFlag(AlignmentType.PointerSize))
		{
			return "IL2CPP_SIZEOF_VOID_P";
		}
		if (alignment.HasFlag(AlignmentType.FourBytes))
		{
			return "4";
		}
		if (alignment.HasFlag(AlignmentType.TwoBytes))
		{
			return "2";
		}
		alignment.HasFlag(AlignmentType.OneByte);
		return "1";
	}

	private static AlignmentType ChooseAlignmentForDefaultPackingRecursive(ReadOnlyContext context, TypeReference typeReference, HashSet<TypeReference> visitedTypes)
	{
		if (visitedTypes.Contains(typeReference))
		{
			return AlignmentType.None;
		}
		visitedTypes.Add(typeReference);
		AlignmentType alignment = AlignmentType.None;
		foreach (InflatedFieldType field in from f in typeReference.GetInflatedFieldTypes(context.Global.Services.TypeFactory)
			where !f.Field.IsStatic
			select f)
		{
			switch (field.Field.FieldType.MetadataType)
			{
			case MetadataType.ValueType:
				alignment |= ChooseAlignmentForDefaultPackingRecursive(context, field.Field.FieldType, visitedTypes);
				break;
			case MetadataType.GenericInstance:
				alignment |= ((!field.Field.FieldType.IsValueType) ? AlignmentType.PointerSize : ChooseAlignmentForDefaultPackingRecursive(context, field.Field.FieldType, visitedTypes));
				break;
			case MetadataType.String:
			case MetadataType.Pointer:
			case MetadataType.Array:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
			case MetadataType.FunctionPointer:
			case MetadataType.Object:
				alignment |= AlignmentType.PointerSize;
				break;
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Double:
				alignment |= AlignmentType.EightBytes;
				break;
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Single:
				alignment |= AlignmentType.FourBytes;
				break;
			case MetadataType.Char:
			case MetadataType.Int16:
			case MetadataType.UInt16:
				alignment |= AlignmentType.TwoBytes;
				break;
			case MetadataType.Boolean:
			case MetadataType.SByte:
			case MetadataType.Byte:
				alignment |= AlignmentType.OneByte;
				break;
			}
		}
		visitedTypes.Remove(typeReference);
		return alignment;
	}

	private static bool HasDefaultPackingSize(TypeDefinition type)
	{
		if (type.PackingSize != 0)
		{
			return type.PackingSize == -1;
		}
		return true;
	}

	private static void WriteComFieldGetters(IReadOnlyContextGeneratedCodeWriter writer, TypeReference declaringType, List<ComFieldWriteInstruction> fieldWriteInstructions)
	{
		for (int i = 0; i < fieldWriteInstructions.Count; i++)
		{
			TypeReference interfaceType = fieldWriteInstructions[i].InterfaceType;
			string fieldTypeName = interfaceType.CppName;
			string fieldName = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(interfaceType);
			string resultVariableName = writer.Context.Global.Services.Naming.ForInteropReturnValue();
			if (i != 0)
			{
				writer.WriteLine();
			}
			writer.AddIncludeForTypeDefinition(writer.Context, interfaceType);
			IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"inline {fieldTypeName}* {writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(interfaceType)}()");
			using (new BlockWriter(writer))
			{
				readOnlyContextGeneratedCodeWriter = writer;
				readOnlyContextGeneratedCodeWriter.WriteLine($"{fieldTypeName}* {resultVariableName} = {fieldName};");
				readOnlyContextGeneratedCodeWriter = writer;
				readOnlyContextGeneratedCodeWriter.WriteLine($"if ({resultVariableName} == {"NULL"})");
				using (new BlockWriter(writer))
				{
					if (interfaceType.Is(Il2CppCustomType.IActivationFactory))
					{
						readOnlyContextGeneratedCodeWriter = writer;
						readOnlyContextGeneratedCodeWriter.WriteLine($"il2cpp::utils::StringView<Il2CppNativeChar> className(IL2CPP_NATIVE_STRING(\"{declaringType.FullName}\"));");
						writer.WriteAssignStatement(resultVariableName, "il2cpp_codegen_windows_runtime_get_activation_factory(className)");
					}
					else
					{
						string sourceInterfaceExpression = Emit.Call(writer.Context, writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(writer.Context.Global.Services.TypeProvider.IActivationFactoryTypeReference));
						string left = string.Format("const il2cpp_hresult_t " + writer.Context.Global.Services.Naming.ForInteropHResultVariable());
						string right = string.Format($"{sourceInterfaceExpression}->QueryInterface({fieldTypeName}::IID, reinterpret_cast<void**>(&{resultVariableName}))");
						writer.WriteAssignStatement(left, right);
						writer.WriteStatement(Emit.Call(writer.Context, "il2cpp_codegen_com_raise_exception_if_failed", writer.Context.Global.Services.Naming.ForInteropHResultVariable(), interfaceType.IsComInterface ? "true" : "false"));
					}
					writer.WriteLine();
					readOnlyContextGeneratedCodeWriter = writer;
					readOnlyContextGeneratedCodeWriter.WriteLine($"if (il2cpp_codegen_atomic_compare_exchange_pointer((void**){Emit.AddressOf(fieldName)}, {resultVariableName}, {"NULL"}) != {"NULL"})");
					using (new BlockWriter(writer))
					{
						readOnlyContextGeneratedCodeWriter = writer;
						readOnlyContextGeneratedCodeWriter.WriteLine($"{resultVariableName}->Release();");
						writer.WriteAssignStatement(resultVariableName, fieldName);
					}
				}
				readOnlyContextGeneratedCodeWriter = writer;
				readOnlyContextGeneratedCodeWriter.WriteLine($"return {resultVariableName};");
			}
		}
	}

	private static string GetDeclaringTypeStructName(ReadOnlyContext context, TypeReference declaringType, FieldReference field)
	{
		if (field.IsThreadStatic)
		{
			return context.Global.Services.Naming.ForThreadFieldsStruct(context, declaringType);
		}
		if (field.IsNormalStatic)
		{
			return context.Global.Services.Naming.ForStaticFieldsStruct(context, declaringType);
		}
		return declaringType.CppName;
	}

	private static string GetBaseTypeDeclaration(ReadOnlyContext context, TypeReference type)
	{
		if (type.IsArray)
		{
			return " : public RuntimeArray";
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition.BaseType != null && typeDefinition.BaseType != context.Global.Services.TypeProvider.GetSystemType(SystemType.Enum) && (typeDefinition.BaseType != context.Global.Services.TypeProvider.GetSystemType(SystemType.ValueType) || typeDefinition == context.Global.Services.TypeProvider.GetSystemType(SystemType.Enum)))
		{
			return string.Format(" : public " + context.Global.Services.Naming.ForType(type.GetBaseType(context)));
		}
		return string.Empty;
	}

	internal static void WriteStaticFieldDefinitionsForTinyProfile(IGeneratedMethodCodeWriter writer, TypeReference type)
	{
		TypeDefinition typeDef = type.Resolve();
		if (typeDef.Fields.Any((FieldDefinition f) => f.IsNormalStatic) || typeDef.StoresNonFieldsInStaticFields())
		{
			writer.WriteLine($"void* {writer.Context.Global.Services.Naming.ForStaticFieldsStructStorage(writer.Context, type)} = (void*)sizeof({writer.Context.Global.Services.Naming.ForStaticFieldsStruct(writer.Context, type)});");
		}
	}

	internal static void WriteStaticFieldRVAExternsForTinyProfile(IGeneratedMethodCodeWriter writer, TypeReference type)
	{
		foreach (FieldDefinition field in type.Resolve().Fields)
		{
			if (field.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
			{
				writer.WriteLine($"extern const uint8_t {writer.Context.Global.Services.Naming.ForStaticFieldsRVAStructStorage(writer.Context, field)}[];");
			}
		}
	}
}
