using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal abstract class ManagedToNativeInteropMethodBodyWriter : InteropMethodBodyWriter
{
	public ManagedToNativeInteropMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, MarshalType marshalType, bool useUnicodeCharset)
		: base(context, interopMethod, methodForParameterNames, new ManagedToNativeMarshaler(context.Global.Services.TypeFactory.ResolverFor(interopMethod.DeclaringType, interopMethod), marshalType, useUnicodeCharset))
	{
	}

	protected override void WriteScopedAllocationCheck(IGeneratedMethodCodeWriter writer)
	{
	}

	protected string GetFunctionCallParametersExpression(ReadOnlyContext context, string[] localVariableNames, bool includesRetVal)
	{
		List<string> parameterList = new List<string>();
		for (int i = 0; i < localVariableNames.Length; i++)
		{
			DefaultMarshalInfoWriter parameterMarshalInfoWriter = MarshalInfoWriterFor(context, Parameters[i]);
			MarshaledType[] marshaledTypes = parameterMarshalInfoWriter.GetMarshaledTypes(context);
			foreach (MarshaledType type in marshaledTypes)
			{
				string name = localVariableNames[i] + type.VariableName;
				string decoratedName = parameterMarshalInfoWriter.DecorateVariable(context, Parameters[i].NameInGeneratedCode, name);
				parameterList.Add(decoratedName);
			}
		}
		MethodReturnType returnType = GetMethodReturnType();
		DefaultMarshalInfoWriter returnValueMarshalInfoWriter = MarshalInfoWriterFor(context, returnType);
		MarshaledType[] returnValueMarshaledTypes = returnValueMarshalInfoWriter.GetMarshaledTypes(context);
		for (int k = 0; k < returnValueMarshaledTypes.Length - 1; k++)
		{
			string name2 = context.Global.Services.Naming.ForInteropReturnValue() + returnValueMarshaledTypes[k].VariableName;
			string decoratedName2 = returnValueMarshalInfoWriter.DecorateVariable(context, null, name2);
			parameterList.Add("&" + decoratedName2);
		}
		if (includesRetVal && returnType.ReturnType.MetadataType != MetadataType.Void)
		{
			parameterList.Add("&" + context.Global.Services.Naming.ForInteropReturnValue());
		}
		return parameterList.AggregateWithComma(context);
	}
}
