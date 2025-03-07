using System;
using System.Collections.ObjectModel;
using System.Text;

namespace Unity.IL2CPP.DataModel.BuildLogic.Naming;

internal static class LazyNameHelpers
{
	public static string GetFullName(FieldReference field)
	{
		using Returnable<StringBuilder> builderContext = field.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		builder.Append(field.FieldType.FullName);
		builder.Append(' ');
		AppendMemberFullName(field, builder);
		return builder.ToString();
	}

	private static void AppendMemberFullName(MemberReference member, StringBuilder builder)
	{
		if (member.DeclaringType == null)
		{
			builder.Append(member.Name);
			return;
		}
		builder.Append(member.DeclaringType.FullName);
		builder.Append("::");
		builder.Append(member.Name);
	}

	public static string GetFullName(TypeDefinition type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendTypeDefFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(GenericInstanceType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendGenericInstanceTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(ArrayType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendArrayTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(ByReferenceType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendByRefTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(PinnedType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendPinnedTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(PointerType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendPointerTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(RequiredModifierType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendRequiredTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(OptionalModifierType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendOptionalTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetFullName(FunctionPointerType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		AppendFunctionPointerTypeFullName(type, builder);
		return builder.ToString();
	}

	public static string GetName(RequiredModifierType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder value = builderContext.Value;
		value.Append(type.ElementType.Name);
		value.Append(" modreq(");
		value.Append(type.ModifierType.FullName);
		value.Append(')');
		return value.ToString();
	}

	public static string GetName(OptionalModifierType type)
	{
		using Returnable<StringBuilder> builderContext = type.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder value = builderContext.Value;
		value.Append(type.ElementType.Name);
		value.Append(" modopt(");
		value.Append(type.ModifierType.FullName);
		value.Append(')');
		return value.ToString();
	}

	private static void AppendPointerTypeFullName(PointerType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
		builder.Append('*');
	}

	private static void AppendPinnedTypeFullName(PinnedType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
	}

	private static void AppendGenericInstanceTypeFullName(GenericInstanceType type, StringBuilder builder)
	{
		builder.Append(type.TypeDef.FullName);
		builder.Append('<');
		for (int i = 0; i < type.GenericInst.Length; i++)
		{
			if (i != 0)
			{
				builder.Append(',');
			}
			builder.Append(type.GenericInst[i].FullName);
		}
		builder.Append('>');
	}

	private static void AppendOptionalTypeFullName(OptionalModifierType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
		builder.Append(" modopt(");
		builder.Append(type.ModifierType.FullName);
		builder.Append(')');
	}

	private static void AppendRequiredTypeFullName(RequiredModifierType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
		builder.Append(" modreq(");
		builder.Append(type.ModifierType.FullName);
		builder.Append(')');
	}

	private static void AppendFunctionPointerTypeFullName(FunctionPointerType type, StringBuilder builder)
	{
		BuildMethodSigFullName(type, builder, "*", null, "method ");
	}

	private static void AppendByRefTypeFullName(ByReferenceType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
		builder.Append('&');
	}

	private static void AppendArrayTypeFullName(ArrayType type, StringBuilder builder)
	{
		builder.Append(type.ElementType.FullName);
		NamingUtils.AppendArraySuffix(type, builder);
	}

	private static void AppendTypeDefFullName(TypeDefinition typeDef, StringBuilder builder)
	{
		if (typeDef.DeclaringType != null)
		{
			if (string.IsNullOrEmpty(typeDef.DeclaringType.FullName))
			{
				throw new InvalidOperationException("The declaring type of nested types must have their name computed first.  This should happen seamlessly as long as nested types are added to the definition table after their parent");
			}
			builder.Append(typeDef.DeclaringType.FullName);
			builder.Append('/');
		}
		if (string.IsNullOrEmpty(typeDef.Namespace))
		{
			builder.Append(typeDef.Name);
			return;
		}
		builder.Append(typeDef.Namespace);
		builder.Append('.');
		builder.Append(typeDef.Name);
	}

	internal static void BuildMethodSigFullName(IMethodSignature method, StringBuilder builder, string name, TypeReference declaringType, string prefix)
	{
		if (prefix != null)
		{
			builder.Append(prefix);
		}
		if (method.CallingConvention != 0)
		{
			builder.Append(method.CallingConvention);
			builder.Append(' ');
		}
		if (method.HasThis)
		{
			builder.Append("[HasThis] ");
		}
		if (method.ExplicitThis)
		{
			builder.Append("[ExplicitThis] ");
		}
		BuildMethodSigReturnAndParameters(method, builder, name, declaringType);
	}

	internal static void BuildMethodReferenceFullName(MethodReference method, StringBuilder builder, TypeReference declaringType)
	{
		BuildMethodSigReturnAndParameters(method, builder, method.Name, declaringType);
	}

	private static void BuildMethodSigReturnAndParameters(IMethodSignature method, StringBuilder builder, string name, TypeReference declaringType)
	{
		builder.Append(method.ReturnType.FullName);
		builder.Append(' ');
		if (declaringType != null)
		{
			builder.Append(declaringType.FullName);
			builder.Append("::");
		}
		builder.Append(name);
		if (method is GenericInstanceMethod { GenericArguments: var genericArguments })
		{
			builder.Append('<');
			for (int i = 0; i < genericArguments.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}
				builder.Append(genericArguments[i].FullName);
			}
			builder.Append('>');
		}
		builder.Append('(');
		if (method.HasParameters)
		{
			ReadOnlyCollection<ParameterDefinition> parameters = method.Parameters;
			for (int index = 0; index < parameters.Count; index++)
			{
				ParameterDefinition param = parameters[index];
				if (index > 0)
				{
					builder.Append(',');
				}
				if (param.ParameterType.IsSentinel)
				{
					builder.Append("...,");
				}
				builder.Append(param.ParameterType.FullName);
			}
		}
		builder.Append(')');
	}

	public static string GetFullName(CallSite callSite)
	{
		using Returnable<StringBuilder> builderContext = callSite.ReturnType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		BuildMethodSigFullName(callSite, builder, "*", null, null);
		return builder.ToString();
	}

	public static string GetFullName(MethodDefinition method)
	{
		using Returnable<StringBuilder> builderContext = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		PopulateMethodDefFullName(method, builder);
		return builder.ToString();
	}

	public static string GetFullName(MethodRefOnTypeInst method)
	{
		using Returnable<StringBuilder> builderContext = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		PopulateMethodRefOnTypeInstFullName(method, builder);
		return builder.ToString();
	}

	public static string GetFullName(SystemImplementedArrayMethod method)
	{
		using Returnable<StringBuilder> builderContext = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		PopulateSystemArrayImplementedMethodNameAndFullName(method, builder);
		return builder.ToString();
	}

	public static string GetFullName(GenericInstanceMethod method)
	{
		using Returnable<StringBuilder> builderContext = method.DeclaringType.Context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		PopulateGenericInstanceMethodFullName(method, builder);
		return builder.ToString();
	}

	public static void PopulateMethodDefFullName(MethodDefinition def, StringBuilder builder)
	{
		BuildMethodReferenceFullName(def, builder, def.DeclaringType);
	}

	private static void PopulateGenericInstanceMethodFullName(GenericInstanceMethod genericInstanceMethod, StringBuilder builder)
	{
		BuildMethodReferenceFullName(genericInstanceMethod, builder, genericInstanceMethod.DeclaringType);
	}

	private static void PopulateMethodRefOnTypeInstFullName(MethodRefOnTypeInst method, StringBuilder builder)
	{
		BuildMethodReferenceFullName(method, builder, method.DeclaringType);
	}

	private static void PopulateSystemArrayImplementedMethodNameAndFullName(SystemImplementedArrayMethod method, StringBuilder builder)
	{
		BuildMethodReferenceFullName(method, builder, method.DeclaringType);
	}
}
