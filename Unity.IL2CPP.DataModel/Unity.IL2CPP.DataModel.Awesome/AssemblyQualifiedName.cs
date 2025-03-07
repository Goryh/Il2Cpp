using System.Collections.ObjectModel;
using System.Text;

namespace Unity.IL2CPP.DataModel.Awesome;

public static class AssemblyQualifiedName
{
	private enum TypeNamingMode
	{
		Full,
		Signature
	}

	public static string BuildAssemblyQualifiedNameSlow(this TypeReference typeref, bool matchSystemTypeAQN = true)
	{
		return typeref.BuildAssemblyQualifiedName(TypeNamingMode.Full, matchSystemTypeAQN);
	}

	private static string BuildAssemblyQualifiedName(this TypeReference typeref, TypeNamingMode typeNamingMode, bool matchSystemTypeAQN, string suffix = "")
	{
		StringBuilder builder = new StringBuilder();
		if (typeref is GenericParameter genericParameter)
		{
			builder.Append((genericParameter.Type == GenericParameterType.Type) ? "!" : "!!");
			builder.Append(genericParameter.Position);
			if (typeNamingMode == TypeNamingMode.Full)
			{
				builder.Append(",");
				if (genericParameter.Owner is TypeReference tref)
				{
					builder.Append(tref.BuildAssemblyQualifiedNameSlow(matchSystemTypeAQN));
				}
				else if (genericParameter.Owner is MethodReference mref)
				{
					builder.Append(mref.BuildAssemblyQualifiedName(matchSystemTypeAQN));
				}
			}
			if (suffix.Length > 0)
			{
				builder.Append(suffix);
			}
			return builder.ToString();
		}
		if (typeref is GenericInstanceType ginst)
		{
			builder.Append("[");
			if (ginst.HasGenericArguments)
			{
				ReadOnlyCollection<TypeReference> args = ginst.GenericArguments;
				for (int i = 0; i < args.Count; i++)
				{
					builder.Append("[");
					builder.Append(args[i].BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN));
					builder.Append("]");
					if (i < args.Count - 1)
					{
						builder.Append(",");
					}
				}
			}
			builder.Append("]");
			return ginst.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + builder) : (builder?.ToString() + suffix));
		}
		if (typeref is ArrayType atype)
		{
			ArraySuffixBuilder(atype, builder, out var elementType);
			return elementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + builder) : (builder?.ToString() + suffix));
		}
		if (typeref is ByReferenceType bref)
		{
			return bref.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + "(ref)") : ("(ref)" + suffix));
		}
		if (typeref is PointerType ptype)
		{
			return ptype.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + "*") : ("*" + suffix));
		}
		if (typeref is RequiredModifierType req)
		{
			builder.Append(req.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN));
			builder.Append(", reqmod ");
			builder.Append(req.ModifierType.BuildAssemblyQualifiedNameSlow(matchSystemTypeAQN));
			return builder.ToString();
		}
		if (typeref is OptionalModifierType opt)
		{
			builder.Append(opt.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN));
			builder.Append(", optmod ");
			builder.Append(opt.ModifierType.BuildAssemblyQualifiedNameSlow(matchSystemTypeAQN));
			return builder.ToString();
		}
		if (typeref is PinnedType pinned)
		{
			return pinned.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + "(pinned)") : ("(pinned)" + suffix));
		}
		if (typeref is SentinelType sentinel)
		{
			return sentinel.ElementType.BuildAssemblyQualifiedName(typeNamingMode, matchSystemTypeAQN, matchSystemTypeAQN ? (suffix + "(pinned)") : ("(sentinel)" + suffix));
		}
		if (typeref is FunctionPointerType functionPointerType)
		{
			builder.Append(functionPointerType.ReturnType.BuildAssemblyQualifiedNameSlow());
			builder.Append(" *");
			BuildMethodSigParameters(functionPointerType, matchSystemTypeAQN, builder);
			return builder.ToString();
		}
		if (matchSystemTypeAQN)
		{
			return typeref.FullName.Replace("/", "+") + suffix + ", " + TyperefAssemblyName(typeref, matchSystemTypeAQN: true);
		}
		return typeref.FullName.Replace("/", "+") + ", " + TyperefAssemblyName(typeref, matchSystemTypeAQN: false) + suffix;
	}

	public static string BuildAssemblyQualifiedName(this MethodReference methodref, bool matchSystemTypeAQN = false)
	{
		StringBuilder builder = new StringBuilder();
		if (methodref.IsWindowsRuntimeProjection)
		{
			builder.Append("WinRT_");
		}
		if (methodref.ReturnType != null)
		{
			builder.Append(methodref.ReturnType.BuildAssemblyQualifiedName(TypeNamingMode.Signature, matchSystemTypeAQN));
			builder.Append(" ");
		}
		if (methodref.DeclaringType != null)
		{
			builder.Append(methodref.DeclaringType.BuildAssemblyQualifiedNameSlow(matchSystemTypeAQN));
			builder.Append("::");
		}
		builder.Append(methodref.Name);
		if (methodref.HasGenericParameters)
		{
			builder.Append("<");
			ReadOnlyCollection<GenericParameter> parameters = methodref.GenericParameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				builder.Append(parameters[i].Name);
				if (i < parameters.Count - 1)
				{
					builder.Append(",");
				}
			}
			builder.Append(">");
		}
		else if (methodref is GenericInstanceMethod { HasGenericArguments: not false } genericInstanceMethod)
		{
			builder.Append("[");
			ReadOnlyCollection<TypeReference> arguments = genericInstanceMethod.GenericArguments;
			for (int j = 0; j < arguments.Count; j++)
			{
				builder.Append("[");
				builder.Append(arguments[j].BuildAssemblyQualifiedNameSlow(matchSystemTypeAQN));
				builder.Append("]");
				if (j < arguments.Count - 1)
				{
					builder.Append(",");
				}
			}
			builder.Append("]");
		}
		BuildMethodSigParameters(methodref, matchSystemTypeAQN, builder);
		return builder.ToString();
	}

	private static void BuildMethodSigParameters(IMethodSignature methodSignature, bool matchSystemTypeAQN, StringBuilder builder)
	{
		builder.Append("(");
		if (methodSignature.HasParameters)
		{
			ReadOnlyCollection<ParameterDefinition> parameters = methodSignature.Parameters;
			for (int i = 0; i < parameters.Count && !parameters[i].ParameterType.IsSentinel; i++)
			{
				builder.Append(parameters[i].ParameterType.BuildAssemblyQualifiedName(TypeNamingMode.Signature, matchSystemTypeAQN));
				if (i < parameters.Count - 1)
				{
					builder.Append(",");
				}
			}
		}
		if (methodSignature.CallingConvention == MethodCallingConvention.VarArg)
		{
			builder.Append(" ... ");
		}
		builder.Append(")");
	}

	private static string TyperefAssemblyName(TypeReference typeref, bool matchSystemTypeAQN)
	{
		AssemblyNameReference assemblyNameRef = typeref.GetAssemblyNameReference();
		if (assemblyNameRef == null)
		{
			return "<NULL>";
		}
		if (matchSystemTypeAQN)
		{
			return assemblyNameRef.FullName;
		}
		return assemblyNameRef.Name;
	}

	private static void ArraySuffixBuilder(ArrayType atype, StringBuilder builder, out TypeReference elementType)
	{
		elementType = atype.ElementType;
		if (elementType.IsArray)
		{
			ArraySuffixBuilder((ArrayType)elementType, builder, out elementType);
		}
		builder.Append("[");
		if (!atype.IsVector)
		{
			if (atype.Rank == 1)
			{
				builder.Append("...");
			}
			else
			{
				builder.Append(',', atype.Rank - 1);
			}
		}
		builder.Append("]");
	}

	public static string RankOnlyName(this ArrayType at)
	{
		StringBuilder builder = new StringBuilder();
		ArraySuffixBuilder(at, builder, out var elementType);
		return elementType.Name + builder;
	}
}
