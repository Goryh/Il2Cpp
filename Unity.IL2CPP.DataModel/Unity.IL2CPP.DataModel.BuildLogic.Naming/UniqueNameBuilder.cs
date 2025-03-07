using System;
using System.Collections.ObjectModel;
using System.Text;

namespace Unity.IL2CPP.DataModel.BuildLogic.Naming;

internal static class UniqueNameBuilder
{
	private enum TypeNamingMode
	{
		Full,
		Signature
	}

	public static string GetMethodDefinitionAssemblyQualifiedName(MethodReference method)
	{
		using Returnable<StringBuilder> builder = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		BuildMethodDefinitionAssemblyQualifiedName(method, builder.Value);
		return builder.Value.ToString();
	}

	private static void BuildMethodDefinitionAssemblyQualifiedName(MethodReference method, StringBuilder builder)
	{
		if (method.IsStatic)
		{
			builder.Append("static ");
		}
		if (method.IsWindowsRuntimeProjection)
		{
			builder.Append("WinRT_");
		}
		MethodReference methodParamsSig = method.Resolve() ?? method;
		PopulateMethodSigReturnType(methodParamsSig, builder, TypeNamingMode.Signature);
		if (method.DeclaringType != null)
		{
			builder.Append(method.DeclaringType.UniqueName);
			builder.Append("::");
		}
		builder.Append(method.Name);
		if (method.IsCompilerControlled)
		{
			builder.Append('$');
			if (method.RequiresRidForNameUniqueness)
			{
				builder.Append("PST");
				builder.Append(method.MetadataToken.RID.ToString("X8"));
			}
		}
		if (method.HasGenericParameters)
		{
			builder.Append('<');
			ReadOnlyCollection<GenericParameter> parameters = method.GenericParameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				builder.Append(parameters[i].Name);
				if (i < parameters.Count - 1)
				{
					builder.Append(',');
				}
			}
			builder.Append('>');
		}
		else if (method is GenericInstanceMethod methodInst)
		{
			builder.Append('[');
			ReadOnlyCollection<TypeReference> arguments = methodInst.GenericArguments;
			for (int j = 0; j < arguments.Count; j++)
			{
				builder.Append('[');
				builder.Append(arguments[j].UniqueName);
				builder.Append(']');
				if (j < arguments.Count - 1)
				{
					builder.Append(',');
				}
			}
			builder.Append(']');
		}
		BuildMethodSigParameters(methodParamsSig, builder, TypeNamingMode.Signature);
	}

	private static void PopulateMethodSigReturnType(IMethodSignature method, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		if (method.ReturnType != null)
		{
			BuildAssemblyQualifiedName(builder, method.ReturnType, typeNamingMode);
			builder.Append(' ');
		}
	}

	private static void BuildMethodSigParameters(IMethodSignature method, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		builder.Append('(');
		if (method.HasParameters)
		{
			ReadOnlyCollection<ParameterDefinition> parameters = method.Parameters;
			for (int i = 0; i < parameters.Count && !parameters[i].ParameterType.IsSentinel; i++)
			{
				BuildAssemblyQualifiedName(builder, parameters[i].ParameterType, typeNamingMode);
				if (i < parameters.Count - 1)
				{
					builder.Append(',');
				}
			}
		}
		if (method.CallingConvention == MethodCallingConvention.VarArg)
		{
			builder.Append(" ... ");
		}
		builder.Append(')');
	}

	private static void BuildTypeDefinitionAssemblyQualifiedName(TypeDefinition typeDef, StringBuilder builder)
	{
		if (typeDef.IsNested)
		{
			builder.Append(typeDef.FullName.Replace('/', '+'));
		}
		else
		{
			builder.Append(typeDef.FullName);
		}
		builder.Append(", ");
		builder.Append(typeDef.Assembly.Name.Name);
	}

	private static void BuildGenericParamAssemblyQualifiedName(GenericParameter genericParam, StringBuilder builder)
	{
		BuildGenericParamAssemblyQualifiedName(genericParam, builder, TypeNamingMode.Full);
	}

	private static void BuildGenericParamAssemblyQualifiedName(GenericParameter genericParam, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		builder.Append((genericParam.Type == GenericParameterType.Type) ? "!" : "!!");
		builder.Append(genericParam.Position);
		if (typeNamingMode == TypeNamingMode.Full)
		{
			builder.Append(',');
			if (genericParam.Owner is TypeReference tref)
			{
				builder.Append(tref.UniqueName);
			}
			else if (genericParam.Owner is MethodReference mref)
			{
				builder.Append(mref.UniqueName);
			}
		}
	}

	public static string GetAssemblyQualifiedName(TypeReference typeReference)
	{
		using Returnable<StringBuilder> builder = typeReference.Context.PerThreadObjects.CheckoutStringBuilder();
		if (typeReference is GenericParameter genericParameter)
		{
			BuildGenericParamAssemblyQualifiedName(genericParameter, builder.Value);
		}
		else if (typeReference is TypeDefinition typeDefinition)
		{
			BuildTypeDefinitionAssemblyQualifiedName(typeDefinition, builder.Value);
		}
		else
		{
			BuildAssemblyQualifiedName(builder.Value, typeReference);
		}
		return builder.Value.ToString();
	}

	private static void BuildAssemblyQualifiedName(StringBuilder builder, TypeReference typeRef)
	{
		BuildAssemblyQualifiedName(builder, typeRef, TypeNamingMode.Full);
	}

	private static void BuildAssemblyQualifiedName(StringBuilder builder, TypeReference typeRef, TypeNamingMode typeNamingMode)
	{
		if (!(typeRef is TypeDefinition))
		{
			if (!(typeRef is GenericParameter genericParam))
			{
				if (!(typeRef is ByReferenceType byReferenceType))
				{
					if (!(typeRef is PointerType pointerType))
					{
						if (!(typeRef is ArrayType arrayType))
						{
							if (!(typeRef is PinnedType pinnedType))
							{
								if (!(typeRef is GenericInstanceType genericInstanceType))
								{
									if (!(typeRef is OptionalModifierType optionalModifierType))
									{
										if (!(typeRef is RequiredModifierType requiredModifierType))
										{
											if (!(typeRef is FunctionPointerType functionPtrTypeRef))
											{
												if (typeRef is SentinelType)
												{
													throw new NotSupportedException("SentinelType is not supported");
												}
												throw new ArgumentException($"Unhandled type {typeRef.GetType()}");
											}
											BuildFunctionPointerTypeAssemblyQualifiedName(functionPtrTypeRef, builder, typeNamingMode);
										}
										else
										{
											BuildRequiredTypeAssemblyQualifiedName(requiredModifierType, builder, typeNamingMode);
										}
									}
									else
									{
										BuildOptionalTypeAssemblyQualifiedName(optionalModifierType, builder, typeNamingMode);
									}
								}
								else
								{
									BuildTypeInstTypeAssemblyQualifiedName(genericInstanceType, builder, typeNamingMode);
								}
							}
							else
							{
								BuildPinnedTypeAssemblyQualifiedName(pinnedType, builder, typeNamingMode);
							}
						}
						else
						{
							BuildArrayTypeAssemblyQualifiedName(arrayType, builder, typeNamingMode);
						}
					}
					else
					{
						BuildPointerTypeAssemblyQualifiedName(pointerType, builder, typeNamingMode);
					}
				}
				else
				{
					BuildByRefTypeAssemblyQualifiedName(byReferenceType, builder, typeNamingMode);
				}
			}
			else
			{
				BuildGenericParamAssemblyQualifiedName(genericParam, builder, typeNamingMode);
			}
		}
		else
		{
			builder.Append(typeRef.UniqueName);
		}
	}

	private static void BuildPointerTypeAssemblyQualifiedName(PointerType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append('*');
	}

	private static void BuildPinnedTypeAssemblyQualifiedName(PinnedType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append("(pinned)");
	}

	private static void BuildTypeInstTypeAssemblyQualifiedName(GenericInstanceType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append('[');
		if (type.HasGenericArguments)
		{
			ReadOnlyCollection<TypeReference> args = type.GenericArguments;
			for (int i = 0; i < args.Count; i++)
			{
				builder.Append('[');
				BuildAssemblyQualifiedName(builder, args[i], typeNamingMode);
				builder.Append(']');
				if (i < args.Count - 1)
				{
					builder.Append(',');
				}
			}
		}
		builder.Append(']');
	}

	private static void BuildOptionalTypeAssemblyQualifiedName(OptionalModifierType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append(", optmod ");
		BuildAssemblyQualifiedName(builder, type.ModifierType, typeNamingMode);
	}

	private static void BuildRequiredTypeAssemblyQualifiedName(RequiredModifierType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append(", reqmod ");
		BuildAssemblyQualifiedName(builder, type.ModifierType, typeNamingMode);
	}

	private static void BuildFunctionPointerTypeAssemblyQualifiedName(FunctionPointerType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		if (type.CallingConvention != 0)
		{
			builder.Append(type.CallingConvention);
			builder.Append(' ');
		}
		if (type.HasThis)
		{
			builder.Append("[HasThis] ");
		}
		if (type.ExplicitThis)
		{
			builder.Append("[ExplicitThis] ");
		}
		PopulateMethodSigReturnType(type, builder, typeNamingMode);
		builder.Append('*');
		BuildMethodSigParameters(type, builder, TypeNamingMode.Signature);
	}

	private static void BuildByRefTypeAssemblyQualifiedName(ByReferenceType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		builder.Append("(ref)");
	}

	private static void BuildArrayTypeAssemblyQualifiedName(ArrayType type, StringBuilder builder, TypeNamingMode typeNamingMode)
	{
		BuildAssemblyQualifiedName(builder, type.ElementType, typeNamingMode);
		NamingUtils.AppendArraySuffix(type, builder);
	}
}
