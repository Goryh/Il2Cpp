using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Naming;

public static class NamingExtensions
{
	public static string Clean(this INamingService naming, ReadOnlyContext context, string value)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder value2 = builderContext.Value;
		value2.AppendClean(value);
		return value2.ToString();
	}

	public static string ForThreadFieldsStruct(this INamingService naming, ReadOnlyContext context, TypeReference type)
	{
		return naming.TypeMember(context, type, "ThreadStaticFields");
	}

	internal static string ForMethodInfoInternal(INamingService naming, MethodReference method, string suffix)
	{
		return method.CppName + "_" + suffix;
	}

	internal static string ForRuntimeMethodInfoInternal(INamingService naming, ReadOnlyContext context, MethodReference method, string suffix)
	{
		return naming.ForRuntimeUniqueMethodNameOnly(context, method) + "_" + suffix;
	}

	public static string ForGenericInst(this INamingService naming, ReadOnlyContext context, IList<IIl2CppRuntimeType> types)
	{
		string name = context.Global.Services.ContextScope.ForMetadataGlobalVar("GenInst");
		for (int i = 0; i < types.Count; i++)
		{
			name = name + "_" + types[i].Type.CppName;
		}
		return NamingUtils.ValueOrHashIfTooLong(name, "GenInst_");
	}

	public static string ForGenericClass(this INamingService naming, ReadOnlyContext context, TypeReference type)
	{
		return naming.TypeMember(context, type, "GenericClass");
	}

	public static string ForStaticFieldsStruct(this INamingService naming, ReadOnlyContext context, TypeReference type)
	{
		return naming.TypeMember(context, type, "StaticFields");
	}

	public static string ForStaticFieldsStructStorage(this INamingService naming, ReadOnlyContext context, TypeReference type)
	{
		return naming.TypeMember(context, type, "StaticFields") + "_Storage";
	}

	public static string ForStaticFieldsRVAStructStorage(this INamingService naming, ReadOnlyContext context, FieldReference field)
	{
		return naming.TypeMember(context, field.DeclaringType, "StaticFields") + "_" + field.CppName + "_RVAStorage";
	}

	public static string ForRuntimeIl2CppType(this INamingService naming, ReadOnlyContext context, IIl2CppRuntimeType type)
	{
		return naming.ForIl2CppType(context, type) + "_var";
	}

	public static string ForRuntimeTypeInfo(this INamingService naming, ReadOnlyContext context, IIl2CppRuntimeType type)
	{
		return naming.ForTypeInfo(context, type.Type) + "_var";
	}

	public static string ForRuntimeMethodInfo(this INamingService naming, ReadOnlyContext context, MethodReference method)
	{
		return ForRuntimeMethodInfoInternal(naming, context, method, "RuntimeMethod") + "_var";
	}

	public static string ForRuntimeFieldInfo(this INamingService naming, ReadOnlyContext context, Il2CppRuntimeFieldReference il2CppRuntimeField)
	{
		return naming.ForFieldInfo(context, il2CppRuntimeField.Field) + "_var";
	}

	public static string ForRuntimeFieldRvaStructStorage(this INamingService naming, ReadOnlyContext context, Il2CppRuntimeFieldReference il2CppRuntimeField)
	{
		return naming.ForStaticFieldsRVAStructStorage(context, il2CppRuntimeField.Field);
	}

	public static string ForPadding(this INamingService naming, TypeDefinition typeDefinition)
	{
		return naming.ForType(typeDefinition) + "__padding";
	}

	public static string ForComTypeInterfaceFieldName(this INamingService naming, TypeReference interfaceType)
	{
		return naming.ForInteropInterfaceVariable(interfaceType);
	}

	public static string ForInteropHResultVariable(this INamingService naming)
	{
		return "hr";
	}

	public static string ForInteropReturnValue(this INamingService naming)
	{
		return "returnValue";
	}

	public static string ForComInterfaceReturnParameterName(this INamingService naming)
	{
		return "comReturnValue";
	}

	public static string ForPInvokeFunctionPointerTypedef(this INamingService naming)
	{
		return "PInvokeFunc";
	}

	public static string ForPInvokeFunctionPointerVariable(this INamingService naming)
	{
		return "il2cppPInvokeFunc";
	}

	public static string ForDelegatePInvokeWrapper(this INamingService naming, TypeReference type)
	{
		return "DelegatePInvokeWrapper_" + naming.ForType(type);
	}

	public static string ForReversePInvokeWrapperMethod(this INamingService naming, ReadOnlyContext context, MethodReference method)
	{
		return context.Global.Services.ContextScope.ForMetadataGlobalVar("ReversePInvokeWrapper_") + method.CppName;
	}

	public static string ForIl2CppComObjectIdentityField(this INamingService naming)
	{
		return "identity";
	}

	public static string ForMethodExecutionContextVariable(this INamingService naming)
	{
		return "methodExecutionContext";
	}

	public static string ForMethodExecutionContextThisVariable(this INamingService naming)
	{
		return "methodExecutionContextThis";
	}

	public static string ForMethodExecutionContextParametersVariable(this INamingService naming)
	{
		return "methodExecutionContextParameters";
	}

	public static string ForMethodExecutionContextLocalsVariable(this INamingService naming)
	{
		return "methodExecutionContextLocals";
	}

	public static string ForMethodNextSequencePointStorageVariable(this INamingService naming)
	{
		return "nextSequencePoint";
	}

	public static string ForMethodExitSequencePointChecker(this INamingService naming)
	{
		return "methodExitChecker";
	}

	private static string ForTypeInfo(this INamingService naming, ReadOnlyContext context, TypeReference typeReference)
	{
		return naming.TypeMember(context, typeReference, "il2cpp_TypeInfo");
	}

	private static string ForFieldInfo(this INamingService naming, ReadOnlyContext context, FieldReference field)
	{
		return naming.TypeMember(context, field.DeclaringType, field.CppName + "_FieldInfo");
	}

	public static string TypeMember(this INamingService naming, ReadOnlyContext context, TypeReference type, string memberName)
	{
		return (type.IsGenericParameter ? naming.ForGenericParameter((GenericParameter)type) : naming.ForRuntimeType(context, type)) + "_" + memberName;
	}

	public static string ForCodeGenModule(this INamingService naming, AssemblyDefinition assembly)
	{
		return "g_" + assembly.CleanFileName + "_CodeGenModule";
	}

	public static string ForComTypeInterfaceFieldGetter(this INamingService naming, TypeReference interfaceType)
	{
		return "get_" + naming.ForInteropInterfaceVariable(interfaceType);
	}

	public static string ForInteropInterfaceVariable(this INamingService naming, TypeReference interfaceType)
	{
		if (interfaceType.Is(Il2CppCustomType.IActivationFactory))
		{
			return "activationFactory";
		}
		string capitalizedName = interfaceType.CppName.TrimStart('_');
		return "____" + capitalizedName.Substring(0, 2).ToLower() + capitalizedName.Substring(2);
	}

	public static string ForRuntimeUniqueTypeNameOnly(this INamingService naming, ReadOnlyContext context, TypeReference type)
	{
		string name = type.CppName;
		string uniqueId = context.Global.Services.ContextScope.UniqueIdentifier;
		if (uniqueId != null)
		{
			return uniqueId + "_" + name;
		}
		return name;
	}

	public static string ForRuntimeUniqueMethodNameOnly(this INamingService naming, ReadOnlyContext context, MethodReference method)
	{
		string name = method.CppName;
		string uniqueId = context.Global.Services.ContextScope.UniqueIdentifier;
		if (uniqueId != null)
		{
			return uniqueId + "_" + name;
		}
		return name;
	}

	public static string ForRuntimeUniqueStringLiteralIdentifier(this INamingService naming, ReadOnlyContext context, string literal)
	{
		string name = naming.ForStringLiteralIdentifier(literal);
		string uniqueId = context.Global.Services.ContextScope.UniqueIdentifier;
		if (uniqueId != null)
		{
			return uniqueId + name;
		}
		return name;
	}
}
