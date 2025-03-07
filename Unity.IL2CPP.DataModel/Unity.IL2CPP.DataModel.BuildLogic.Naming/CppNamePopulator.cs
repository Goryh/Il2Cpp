using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.DataModel.BuildLogic.Naming;

internal static class CppNamePopulator
{
	private interface ICppNameTask
	{
		void Invoke();
	}

	private readonly struct CppMethodNameTask : ICppNameTask
	{
		private readonly MethodReference _method;

		public CppMethodNameTask(MethodReference method)
		{
			_method = method;
		}

		public void Invoke()
		{
			_ = _method.CppName;
		}
	}

	private readonly struct CppTypeNameTask : ICppNameTask
	{
		private readonly TypeReference _type;

		public CppTypeNameTask(TypeReference type)
		{
			_type = type;
		}

		public void Invoke()
		{
			_ = _type.CppName;
		}
	}

	private readonly struct MethodBuildListTask : ICppNameTask
	{
		private readonly TypeContext _context;

		private readonly ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> _allNonDefinitionMethods;

		private readonly MethodTaskListContainer _methodListContainer;

		private readonly int _methodChunks;

		public MethodBuildListTask(TypeContext context, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods, MethodTaskListContainer methodTaskListContainer, int methodChunks)
		{
			_context = context;
			_allNonDefinitionMethods = allNonDefinitionMethods;
			_methodListContainer = methodTaskListContainer;
			_methodChunks = methodChunks;
		}

		public void Invoke()
		{
			_methodListContainer.MethodTasks = BuildMethodTaskList(_context, _allNonDefinitionMethods).ToArray().ChunkRoundRobin(_methodChunks);
		}
	}

	private class MethodTaskListContainer
	{
		public ReadOnlyCollection<ReadOnlyCollection<ICppNameTask>> MethodTasks;
	}

	private static readonly char[] HashLookup = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'A', 'B', 'C', 'D', 'E', 'F'
	};

	private static readonly int HashSize = SHA1.Create().HashSize / 8;

	private static IEnumerable<ICppNameTask> BuildTaskList(TypeContext context, ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods, MethodTaskListContainer methodTaskListContainer, int methodChunks)
	{
		return new ICppNameTask[1]
		{
			new MethodBuildListTask(context, allNonDefinitionMethods, methodTaskListContainer, methodChunks)
		}.Concat(context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.GetAllTypes()).Cast<TypeReference>().Concat(context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.AllGenericParameters()))
			.Concat(allNonDefinitionTypes.Select((UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference> pair) => pair.Ours))
			.Select((Func<TypeReference, ICppNameTask>)((TypeReference t) => new CppTypeNameTask(t))));
	}

	private static IEnumerable<ICppNameTask> BuildMethodTaskList(TypeContext context, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		return context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.AllMethods()).Concat(allNonDefinitionMethods.Select((UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference> pair) => pair.Ours)).Select((Func<MethodReference, ICppNameTask>)((MethodReference m) => new CppMethodNameTask(m)));
	}

	public static void ComputeAllNames(TypeContext context, TinyProfiler2 tinyProfiler, ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		int methodChunkCount = context.Parameters.JobCount;
		MethodTaskListContainer methodTasks = new MethodTaskListContainer();
		using (tinyProfiler.Section("Types"))
		{
			ParallelHelpers.ForEachChunkedRoundRobin(BuildTaskList(context, allNonDefinitionTypes, allNonDefinitionMethods, methodTasks, methodChunkCount).ToArray(), delegate(ReadOnlyCollection<ICppNameTask> chunk)
			{
				using (tinyProfiler.Section("Chunk"))
				{
					foreach (ICppNameTask item in chunk)
					{
						item.Invoke();
					}
				}
			}, context.Parameters, context.Parameters.JobCount * 4);
		}
		using (tinyProfiler.Section("Methods"))
		{
			ParallelHelpers.ForEach(methodTasks.MethodTasks, delegate(ReadOnlyCollection<ICppNameTask> chunk)
			{
				using (tinyProfiler.Section("Chunk"))
				{
					foreach (ICppNameTask item2 in chunk)
					{
						item2.Invoke();
					}
				}
			}, context.Parameters.EnableSerial);
		}
	}

	public static string GetMethodRefCppName(MethodReference method)
	{
		using Returnable<StringBuilder> builder = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		return GetMethodRefCppName(method, builder.Value);
	}

	private static string GetMethodRefCppName(MethodReference method, StringBuilder builder)
	{
		builder.Clear();
		GenericInstanceMethod asGenericInstanceMethod = method as GenericInstanceMethod;
		builder.AppendClean(method.DeclaringType.Name);
		builder.Append("_");
		builder.AppendClean(method.Name);
		if (asGenericInstanceMethod != null)
		{
			using Returnable<StringBuilder> genParamBuilder = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
			foreach (TypeReference arg in asGenericInstanceMethod.GenericArguments)
			{
				genParamBuilder.Value.Append("_Tis");
				genParamBuilder.Value.Append(arg.CppName);
			}
			builder.Append(NamingUtils.ValueOrHashIfTooLong(genParamBuilder.Value, "_GHsh"));
		}
		builder.Append("_m");
		builder.Append(method.UniqueHash);
		return builder.ToString();
	}

	public static string GetTypeRefCppName(TypeReference type)
	{
		using Returnable<StringBuilder> builder = type.Context.PerThreadObjects.CheckoutStringBuilder();
		return GetTypeRefCppName(type, builder.Value);
	}

	public static string GetTypeRefUniqueHash(TypeReference type)
	{
		string wellKnownName = GetWellKnownNameFor(type);
		if (wellKnownName != null)
		{
			return wellKnownName;
		}
		return GenerateForString(type.UniqueName);
	}

	private static string GetTypeRefCppName(TypeReference type, StringBuilder builder)
	{
		string wellKnownName = GetWellKnownNameFor(type);
		if (wellKnownName != null)
		{
			return wellKnownName;
		}
		builder.Clear();
		string typeName = ((!type.IsArray) ? type.Name : ((ArrayType)type).RankOnlyName());
		builder.AppendClean(typeName);
		builder.Append("_t");
		builder.Append(type.UniqueHash);
		return builder.ToString();
	}

	public static string GetFieldRefCppName(FieldReference field)
	{
		using Returnable<StringBuilder> builderContext = field.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		builder.AppendClean("___");
		builder.AppendClean(field.Name, skipFirstCharacterSafeCheck: true);
		if (field.DeclaringType.FieldDuplication == FieldDuplication.Names)
		{
			builder.Append('_');
			builder.Append(field.FieldType.CppName);
		}
		else if (field.DeclaringType.FieldDuplication == FieldDuplication.Signatures)
		{
			builder.Append("PST");
			builder.Append(field.MetadataToken.RID.ToString("X8"));
		}
		return builder.ToString();
	}

	public static string GetParameterDefinitionCppName(ParameterDefinition parameter)
	{
		using Returnable<StringBuilder> builderContext = parameter.ParameterType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		string parameterName = parameter.Name;
		StringBuilder stringBuilder = builder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder);
		handler.AppendLiteral("___");
		handler.AppendFormatted(parameter.Index);
		handler.AppendLiteral("_");
		stringBuilder.Append(ref handler);
		if (string.IsNullOrEmpty(parameterName))
		{
			builder.Append('p');
		}
		else
		{
			builder.AppendClean(parameterName);
		}
		return builder.ToString();
	}

	internal unsafe static string GenerateForString(string str)
	{
		return string.Create(HashSize * 2, str, delegate(Span<char> span, string str)
		{
			byte* pointer = stackalloc byte[(int)(uint)HashSize];
			Span<byte> destination = new Span<byte>(pointer, HashSize);
			SHA1.HashData(MemoryMarshal.AsBytes(str.AsSpan()), destination);
			for (int i = 0; i < HashSize; i++)
			{
				byte b = destination[i];
				span[i * 2] = HashLookup[b / 16];
				span[i * 2 + 1] = HashLookup[b % 16];
			}
		});
	}

	internal static string GetWellKnownNameFor(TypeReference typeReference)
	{
		switch (typeReference.MetadataType)
		{
		case MetadataType.Object:
			return "RuntimeObject";
		case MetadataType.String:
			return "String_t";
		case MetadataType.IntPtr:
			return "IntPtr_t";
		case MetadataType.UIntPtr:
			return "UIntPtr_t";
		default:
		{
			TypeDefinition typeDef = typeReference.Resolve();
			if (typeDef != null && typeDef.Module != null && typeDef.Assembly == typeDef.Context.SystemAssembly)
			{
				switch (typeReference.Namespace)
				{
				case "System":
					switch (typeReference.Name)
					{
					case "Array":
						return "RuntimeArray";
					case "String":
						return "String_t";
					case "Type":
						return "Type_t";
					case "Delegate":
						return "Delegate_t";
					case "MulticastDelegate":
						return "MulticastDelegate_t";
					case "Exception":
						return "Exception_t";
					case "MonoType":
						return "MonoType_t";
					case "Guid":
						return "Guid_t";
					}
					break;
				case "System.Reflection":
					switch (typeReference.Name)
					{
					case "Assembly":
						return "Assembly_t";
					case "MemberInfo":
						return "MemberInfo_t";
					case "MethodBase":
						return "MethodBase_t";
					case "MethodInfo":
						return "MethodInfo_t";
					case "FieldInfo":
						return "FieldInfo_t";
					case "PropertyInfo":
						return "PropertyInfo_t";
					case "EventInfo":
						return "EventInfo_t";
					case "MonoMethod":
						return "MonoMethod_t";
					case "MonoGenericMethod":
						return "MonoGenericMethod_t";
					case "MonoField":
						return "MonoField_t";
					case "MonoProperty":
						return "MonoProperty_t";
					case "MonoEvent":
						return "MonoEvent_t";
					}
					break;
				case "System.Text":
					if (typeReference.Name == "StringBuilder")
					{
						return "StringBuilder_t";
					}
					break;
				}
			}
			if (typeReference.IsIActivationFactory)
			{
				return "Il2CppIActivationFactory";
			}
			if (typeReference.IsIl2CppComObject)
			{
				return "Il2CppComObject";
			}
			if (typeReference.IsIl2CppFullySharedGenericType)
			{
				if (typeReference.IsValueType)
				{
					return "Il2CppFullySharedGenericStruct";
				}
				return "Il2CppFullySharedGenericAny";
			}
			return null;
		}
		}
	}

	public static string ForVariable(TypeReference variableType)
	{
		using Returnable<StringBuilder> builderScope = variableType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderScope.Value;
		variableType = variableType.WithoutModifiers();
		ArrayType arrayType = variableType as ArrayType;
		PointerType pointerType = variableType as PointerType;
		FunctionPointerType functionPointerType = variableType as FunctionPointerType;
		ByReferenceType byRefType = variableType as ByReferenceType;
		if (arrayType != null)
		{
			int rank = arrayType.Rank;
			if (rank >= 1)
			{
				builder.Append(arrayType.CppName);
				builder.Append('*');
				return builder.ToString();
			}
			throw new NotImplementedException($"Invalid array rank {rank}");
		}
		if (pointerType != null)
		{
			return pointerType.ElementType.CppNameForPointerToVariable;
		}
		if (functionPointerType != null)
		{
			return "void*";
		}
		if (byRefType != null)
		{
			return byRefType.ElementType.CppNameForPointerToVariable;
		}
		switch (variableType.MetadataType)
		{
		case MetadataType.Void:
			return "void";
		case MetadataType.Boolean:
			return "bool";
		case MetadataType.Single:
			return "float";
		case MetadataType.Double:
			return "double";
		case MetadataType.String:
			builder.Append(variableType.CppName);
			builder.Append('*');
			return builder.ToString();
		case MetadataType.SByte:
			return "int8_t";
		case MetadataType.Byte:
			return "uint8_t";
		case MetadataType.Char:
			return "Il2CppChar";
		case MetadataType.Int16:
			return "int16_t";
		case MetadataType.UInt16:
			return "uint16_t";
		case MetadataType.Int32:
			return "int32_t";
		case MetadataType.UInt32:
			return "uint32_t";
		case MetadataType.Int64:
			return "int64_t";
		case MetadataType.UInt64:
			return "uint64_t";
		case MetadataType.IntPtr:
			return "intptr_t";
		case MetadataType.UIntPtr:
			return "uintptr_t";
		default:
		{
			if (variableType.Name == "intptr_t")
			{
				return "intptr_t";
			}
			if (variableType.Name == "uintptr_t")
			{
				return "uintptr_t";
			}
			if (variableType is GenericParameter)
			{
				throw new ArgumentException("Generic parameter encountered as variable type", "variableType");
			}
			TypeDefinition typeDefinition = variableType.Resolve();
			if (typeDefinition.IsEnum)
			{
				return typeDefinition.Fields.Single((FieldDefinition f) => f.Name == "value__").FieldType.CppNameForVariable;
			}
			if (variableType.Resolve().IsInterface)
			{
				return "RuntimeObject*";
			}
			builder.Append(variableType.CppName);
			if (NeedsAsterisk(variableType))
			{
				builder.Append('*');
			}
			return builder.ToString();
		}
		}
	}

	private static bool NeedsAsterisk(TypeReference type)
	{
		TypeReference underlyingType = type.UnderlyingType();
		if (underlyingType.IsIl2CppFullySharedGenericType)
		{
			return false;
		}
		if (underlyingType.IsValueType)
		{
			return type.IsByReference;
		}
		return true;
	}
}
