using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.IL2CPP.CompilerServices;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct CompilerServicesSupport
{
	public static bool HasNullChecksSupportEnabled(ReadOnlyContext context, MethodDefinition methodDefinition, bool globalValue)
	{
		return HasOptionEnabled(methodDefinition, Option.NullChecks, globalValue);
	}

	public static bool HasArrayBoundsChecksSupportEnabled(MethodDefinition methodDefinition, bool globalValue)
	{
		return HasOptionEnabled(methodDefinition, Option.ArrayBoundsChecks, globalValue);
	}

	public static bool HasDivideByZeroChecksSupportEnabled(MethodDefinition methodDefinition, bool globalValue)
	{
		return HasOptionEnabled(methodDefinition, Option.DivideByZeroChecks, globalValue);
	}

	private static bool IsEagerStaticClassConstructionAttribute(CustomAttribute ca)
	{
		TypeReference attributeType = ca.AttributeType;
		if (!(attributeType.Namespace == "Unity.IL2CPP.CompilerServices") || !(attributeType.Name == "Il2CppEagerStaticClassConstructionAttribute"))
		{
			if (attributeType.Namespace == "System.Runtime.CompilerServices")
			{
				return attributeType.Name == "EagerStaticClassConstructionAttribute";
			}
			return false;
		}
		return true;
	}

	public static bool HasEagerStaticClassConstructionEnabled(TypeDefinition type)
	{
		if (!type.HasGenericParameters && type.HasCustomAttributes)
		{
			return type.CustomAttributes.Any(IsEagerStaticClassConstructionAttribute);
		}
		return false;
	}

	private static bool IsGenerateIntoOwnCppFileAttribute(CustomAttribute ca)
	{
		TypeReference attributeType = ca.AttributeType;
		if (attributeType.Namespace == "Unity.IL2CPP.CompilerServices")
		{
			return attributeType.Name == "Il2CppGenerateIntoOwnCppFileAttribute";
		}
		return false;
	}

	public static bool HasGenerateIntoOwnCppFile(ICustomAttributeProvider provider)
	{
		return provider.CustomAttributes.Any(IsGenerateIntoOwnCppFileAttribute);
	}

	public static IEnumerable<CustomAttribute> SetOptionAttributes(ICustomAttributeProvider provider)
	{
		return provider.CustomAttributes.Where(IsSetOptionAttribute);
	}

	public static bool IsSetOptionAttribute(CustomAttribute attribute)
	{
		TypeReference attributeType = attribute.AttributeType;
		if (attributeType.Namespace == "Unity.IL2CPP.CompilerServices")
		{
			return attributeType.Name == "Il2CppSetOptionAttribute";
		}
		return false;
	}

	public static bool HasIgnoredByDeepProfilerAttribute(MethodDefinition method)
	{
		if (!method.HasAttribute("Unity.Profiling", "IgnoredByDeepProfilerAttribute"))
		{
			return method.DeclaringType.HasAttribute("Unity.Profiling", "IgnoredByDeepProfilerAttribute");
		}
		return true;
	}

	private static bool HasOptionEnabled(IMemberDefinition methodDefinition, Option option, bool globalValue)
	{
		bool result = globalValue;
		if (GetBooleanOptionValue(methodDefinition.CustomAttributes, option, ref result))
		{
			return result;
		}
		TypeDefinition typeDefinition = methodDefinition.DeclaringType;
		foreach (PropertyDefinition propertyDefinition in typeDefinition.Properties)
		{
			if ((propertyDefinition.GetMethod == methodDefinition || propertyDefinition.SetMethod == methodDefinition) && GetBooleanOptionValue(propertyDefinition.CustomAttributes, option, ref result))
			{
				return result;
			}
		}
		if (GetBooleanOptionValue(typeDefinition.CustomAttributes, option, ref result))
		{
			return result;
		}
		if (GetBooleanOptionValue(typeDefinition.Assembly.CustomAttributes, option, ref result))
		{
			return result;
		}
		return globalValue;
	}

	private static bool GetBooleanOptionValue(IEnumerable<CustomAttribute> attributes, Option option, ref bool result)
	{
		return GetOptionValue(attributes, option, ref result);
	}

	private static bool GetOptionValue<T>(IEnumerable<CustomAttribute> attributes, Option option, ref T result)
	{
		foreach (CustomAttribute customAttribute in attributes)
		{
			if (!IsSetOptionAttribute(customAttribute))
			{
				continue;
			}
			ReadOnlyCollection<CustomAttributeArgument> arguments = customAttribute.ConstructorArguments;
			if ((int)arguments[0].Value == (int)option)
			{
				try
				{
					result = (T)((CustomAttributeArgument)arguments[1].Value).Value;
				}
				catch (InvalidCastException)
				{
					continue;
				}
				return true;
			}
		}
		return false;
	}
}
