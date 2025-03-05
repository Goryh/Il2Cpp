using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters;

public abstract class InteropMethodBodyWriter : InteropMethodInfo
{
	protected sealed override MethodReference InteropMethod => base.InteropMethod;

	protected virtual bool AreParametersMarshaled { get; } = true;

	protected virtual bool IsReturnValueMarshaled { get; } = true;

	protected InteropMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, InteropMarshaler marshaler)
		: base(context, interopMethod, methodForParameterNames, marshaler)
	{
	}

	protected virtual void WriteScopedAllocationCheck(IGeneratedMethodCodeWriter writer)
	{
	}

	protected virtual void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
	}

	protected abstract void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess);

	public virtual void WriteMethodBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		DefaultMarshalInfoWriter firstUnmarshalableMarshalInfoWriter = FirstOrDefaultUnmarshalableMarshalInfoWriter(writer.Context);
		if (firstUnmarshalableMarshalInfoWriter != null)
		{
			writer.WriteStatement(Emit.RaiseManagedException(firstUnmarshalableMarshalInfoWriter.GetMarshalingException(writer.Context, metadataAccess)));
			return;
		}
		MarshaledParameter[] parameters = Parameters;
		foreach (MarshaledParameter parameter in parameters)
		{
			MarshalInfoWriterFor(writer.Context, parameter).WriteIncludesForMarshaling(writer);
		}
		MarshalInfoWriterFor(writer.Context, GetMethodReturnType()).WriteIncludesForMarshaling(writer);
		WriteMethodBodyImpl(writer, metadataAccess);
	}

	private void WriteMethodBodyImpl(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		WriteScopedAllocationCheck(writer);
		WriteMethodPrologue(writer, metadataAccess);
		string[] localVariableNames = (AreParametersMarshaled ? WriteMarshalInputParameters(writer, metadataAccess) : null);
		string unmarshaledReturnValueVariableName = null;
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(_marshaler.GetPrettyCalleeName() + " invocation");
		}
		WriteInteropCallStatement(writer, localVariableNames, metadataAccess);
		writer.WriteLine();
		MethodReturnType methodReturnType = GetMethodReturnType();
		if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			if (IsReturnValueMarshaled)
			{
				unmarshaledReturnValueVariableName = _marshaler.WriteMarshalReturnValue(writer, methodReturnType, Parameters, metadataAccess);
				_marshaler.WriteMarshalCleanupReturnValue(writer, methodReturnType, metadataAccess);
			}
			else
			{
				unmarshaledReturnValueVariableName = writer.Context.Global.Services.Naming.ForInteropReturnValue();
			}
		}
		if (AreParametersMarshaled)
		{
			WriteMarshalOutputParameters(writer, localVariableNames, metadataAccess);
		}
		WriteReturnStatement(writer, unmarshaledReturnValueVariableName, metadataAccess);
	}

	protected DefaultMarshalInfoWriter FirstOrDefaultUnmarshalableMarshalInfoWriter(ReadOnlyContext context)
	{
		MarshaledParameter[] parameters = Parameters;
		foreach (MarshaledParameter parameter in parameters)
		{
			if (!_marshaler.CanMarshalAsInputParameter(context, parameter, out var marshalInfoWriter))
			{
				return marshalInfoWriter;
			}
			if (IsOutParameter(parameter) && !_marshaler.CanMarshalAsOutputParameter(context, parameter, marshalInfoWriter))
			{
				return marshalInfoWriter;
			}
		}
		MethodReturnType returnType = GetMethodReturnType();
		if (returnType.ReturnType.MetadataType != MetadataType.Void && !_marshaler.CanMarshalAsOutputParameter(context, returnType, out var marshalInfoWriter2))
		{
			return marshalInfoWriter2;
		}
		return null;
	}

	private string[] WriteMarshalInputParameters(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string[] variableNames = new string[Parameters.Length];
		for (int i = 0; i < Parameters.Length; i++)
		{
			variableNames[i] = WriteMarshalInputParameter(writer, Parameters[i], metadataAccess);
		}
		return variableNames;
	}

	private string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
		if (IsInParameter(parameter))
		{
			return _marshaler.WriteMarshalInputParameter(writer, parameter, Parameters, metadataAccess);
		}
		return _marshaler.WriteMarshalEmptyInputParameter(writer, parameter, Parameters, metadataAccess);
	}

	private void WriteMarshalOutputParameters(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		for (int i = 0; i < Parameters.Length; i++)
		{
			WriteMarshalOutputParameter(writer, localVariableNames[i], Parameters[i], metadataAccess);
			WriteCleanupParameter(writer, localVariableNames[i], Parameters[i], metadataAccess);
		}
	}

	private void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
		if (IsOutParameter(parameter))
		{
			_marshaler.WriteMarshalOutputParameter(writer, valueName, parameter, Parameters, metadataAccess);
		}
	}

	private void WriteCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
		if (ParameterRequiresCleanup(parameter))
		{
			if (IsInParameter(parameter))
			{
				_marshaler.WriteMarshalCleanupParameter(writer, valueName, parameter, metadataAccess);
			}
			else
			{
				_marshaler.WriteMarshalCleanupEmptyParameter(writer, valueName, parameter, metadataAccess);
			}
		}
	}

	protected virtual void WriteReturnStatement(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
		{
			writer.WriteReturnStatement(unmarshaledReturnValueVariableName);
		}
	}

	protected MethodReturnType GetMethodReturnType()
	{
		return InteropMethod.MethodReturnType;
	}

	protected string GetMethodName()
	{
		return InteropMethod.Name;
	}

	protected virtual string GetMethodNameInGeneratedCode(ReadOnlyContext context)
	{
		return InteropMethod.CppName;
	}

	protected IList<CustomAttribute> GetCustomMethodAttributes()
	{
		return InteropMethod.Resolve().CustomAttributes;
	}

	protected DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MarshaledParameter parameter)
	{
		return _marshaler.MarshalInfoWriterFor(context, parameter);
	}

	protected DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MethodReturnType methodReturnType)
	{
		return _marshaler.MarshalInfoWriterFor(context, methodReturnType);
	}

	protected bool IsInParameter(MarshaledParameter parameter)
	{
		TypeReference parameterType = parameter.ParameterType;
		if (parameter.IsOut && !parameter.IsIn)
		{
			if (parameterType.IsValueType)
			{
				return !parameterType.IsByReference;
			}
			return false;
		}
		return true;
	}

	protected bool IsOutParameter(MarshaledParameter parameter)
	{
		TypeReference parameterType = parameter.ParameterType;
		if (parameter.IsOut && !parameterType.IsValueType)
		{
			return true;
		}
		if (parameter.IsIn && !parameter.IsOut)
		{
			return false;
		}
		if (parameter.ParameterType.IsByReference)
		{
			return true;
		}
		if (MarshalingUtils.IsStringBuilder(parameterType))
		{
			return true;
		}
		if (!parameter.ParameterType.IsValueType && MarshalingUtils.IsBlittable(_context, parameter.ParameterType, null, MarshalType.PInvoke, useUnicodeCharset: false))
		{
			return true;
		}
		return false;
	}

	protected static string GetDelegateCallingConvention(TypeDefinition delegateTypedef)
	{
		CustomAttribute unmanagedFunctionPointerAttribute = delegateTypedef.CustomAttributes.FirstOrDefault((CustomAttribute attribute) => attribute.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");
		if (unmanagedFunctionPointerAttribute == null || !unmanagedFunctionPointerAttribute.HasConstructorArguments)
		{
			return "DEFAULT_CALL";
		}
		if (!(unmanagedFunctionPointerAttribute.ConstructorArguments[0].Value is int))
		{
			return "DEFAULT_CALL";
		}
		return (CallingConvention)(int)unmanagedFunctionPointerAttribute.ConstructorArguments[0].Value switch
		{
			CallingConvention.Cdecl => "CDECL", 
			CallingConvention.StdCall => "STDCALL", 
			_ => "DEFAULT_CALL", 
		};
	}

	private bool ParameterRequiresCleanup(MarshaledParameter parameter)
	{
		if (!IsInParameter(parameter))
		{
			return parameter.IsOut;
		}
		return true;
	}
}
