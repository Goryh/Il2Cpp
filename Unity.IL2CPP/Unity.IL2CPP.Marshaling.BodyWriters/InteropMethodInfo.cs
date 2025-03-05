using System.Collections.Generic;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters;

public class InteropMethodInfo
{
	protected readonly ReadOnlyContext _context;

	protected readonly InteropMarshaler _marshaler;

	protected readonly TypeResolver _typeResolver;

	public readonly MarshaledParameter[] Parameters;

	public readonly MarshaledType[] MarshaledParameterTypes;

	public readonly MarshaledType MarshaledReturnType;

	protected virtual MethodReference InteropMethod { get; }

	public static InteropMethodInfo ForComCallableWrapper(ReadOnlyContext context, MethodReference managedMethod, MethodReference interfaceMethod, MarshalType marshalType)
	{
		return ForNativeToManaged(context, managedMethod, interfaceMethod, marshalType, useUnicodeCharset: true);
	}

	public static InteropMethodInfo ForNativeToManaged(ReadOnlyContext context, MethodReference managedMethod, MethodReference interopMethod, MarshalType marshalType, bool useUnicodeCharset)
	{
		return new InteropMethodInfo(context, interopMethod, managedMethod, new NativeToManagedMarshaler(context.Global.Services.TypeFactory.ResolverFor(interopMethod.DeclaringType, interopMethod), marshalType, useUnicodeCharset));
	}

	protected InteropMethodInfo(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, InteropMarshaler marshaler)
	{
		InteropMethod = interopMethod;
		_context = context;
		_marshaler = marshaler;
		_typeResolver = _context.Global.Services.TypeFactory.ResolverFor(interopMethod.DeclaringType, interopMethod);
		MethodDefinition methodDefinition = interopMethod.Resolve();
		Parameters = new MarshaledParameter[methodDefinition.Parameters.Count];
		for (int i = 0; i < methodDefinition.Parameters.Count; i++)
		{
			ParameterDefinition nameParameter = methodForParameterNames.Parameters[i];
			ParameterDefinition parameter = methodDefinition.Parameters[i];
			TypeReference parameterType = _typeResolver.Resolve(parameter.ParameterType);
			Parameters[i] = new MarshaledParameter(nameParameter.Name, nameParameter.CppName, parameterType, parameter.MarshalInfo, parameter.IsIn, parameter.IsOut);
		}
		List<MarshaledType> marshaledParameterTypes = new List<MarshaledType>();
		MarshaledParameter[] parameters = Parameters;
		foreach (MarshaledParameter parameter2 in parameters)
		{
			MarshaledType[] marshaledTypes = marshaler.MarshalInfoWriterFor(context, parameter2).GetMarshaledTypes(context);
			foreach (MarshaledType type in marshaledTypes)
			{
				marshaledParameterTypes.Add(new MarshaledType(type.Name, type.DecoratedName, parameter2.NameInGeneratedCode + type.VariableName));
			}
		}
		MarshaledType[] returnValueMarshaledTypes = marshaler.MarshalInfoWriterFor(context, interopMethod.MethodReturnType).GetMarshaledTypes(context);
		for (int l = 0; l < returnValueMarshaledTypes.Length - 1; l++)
		{
			MarshaledType type2 = returnValueMarshaledTypes[l];
			marshaledParameterTypes.Add(new MarshaledType(type2.Name + "*", type2.DecoratedName + "*", context.Global.Services.Naming.ForComInterfaceReturnParameterName() + type2.VariableName));
		}
		MarshaledParameterTypes = marshaledParameterTypes.ToArray();
		MarshaledReturnType = returnValueMarshaledTypes[^1];
	}
}
