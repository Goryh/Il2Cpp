using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class ManagedToNativeMarshaler : InteropMarshaler
{
	public ManagedToNativeMarshaler(TypeResolver typeResolver, MarshalType marshalType, bool useUnicodeCharset)
		: base(typeResolver, marshalType, useUnicodeCharset)
	{
	}

	public override bool CanMarshalAsInputParameter(ReadOnlyContext context, MarshaledParameter parameter, out DefaultMarshalInfoWriter marshalInfoWriter)
	{
		marshalInfoWriter = MarshalInfoWriterFor(context, parameter);
		return marshalInfoWriter.CanMarshalTypeToNative(context);
	}

	public override bool CanMarshalAsOutputParameter(ReadOnlyContext context, MarshaledParameter parameter, DefaultMarshalInfoWriter marshalInfoWriter)
	{
		return marshalInfoWriter.CanMarshalTypeFromNative(context);
	}

	public override bool CanMarshalAsOutputParameter(ReadOnlyContext context, MethodReturnType methodReturnType, out DefaultMarshalInfoWriter marshalInfoWriter)
	{
		marshalInfoWriter = MarshalInfoWriterFor(context, methodReturnType);
		return marshalInfoWriter.CanMarshalTypeFromNativeAsReturnValue(context);
	}

	public override string GetPrettyCalleeName()
	{
		return "Native function";
	}

	public override string WriteMarshalEmptyInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
	{
		return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' to native representation");
			}
		}, (IGeneratedMethodCodeWriter bodyWriter) => MarshalInfoWriterFor(writer.Context, parameter).WriteMarshalEmptyVariableToNative(bodyWriter, new ManagedMarshalValue(parameter.NameInGeneratedCode), parameters), delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
	{
		return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' to native representation");
			}
		}, (IGeneratedMethodCodeWriter bodyWriter) => MarshalInfoWriterFor(writer.Context, parameter).WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue(parameter.NameInGeneratedCode), parameter.Name, metadataAccess), delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
	{
		if (valueName == parameter.NameInGeneratedCode)
		{
			return;
		}
		writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' back from native representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, parameter);
			ManagedMarshalValue destinationVariable = new ManagedMarshalValue(parameter.NameInGeneratedCode);
			if (parameter.IsOut)
			{
				defaultMarshalInfoWriter.WriteMarshalOutParameterFromNative(bodyWriter, valueName, destinationVariable, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, parameter.IsIn, metadataAccess);
			}
			else
			{
				defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, valueName, destinationVariable, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
			}
			if (parameter.ParameterType is ByReferenceType)
			{
				bodyWriter.WriteWriteBarrierIfNeeded((parameter.ParameterType as ByReferenceType).ElementType, destinationVariable.GetNiceName(writer.Context), valueName);
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override string WriteMarshalReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
	{
		return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling of return value back from native representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, methodReturnType);
			string variableName = defaultMarshalInfoWriter.UndecorateVariable(writer.Context, writer.Context.Global.Services.Naming.ForInteropReturnValue());
			return defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, variableName, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, metadataAccess);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override void WriteMarshalCleanupEmptyParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of parameter '" + parameter.NameInGeneratedCode + "' native representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			MarshalInfoWriterFor(writer.Context, parameter).WriteMarshalCleanupOutVariable(bodyWriter, valueName, metadataAccess, parameter.NameInGeneratedCode);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override void WriteMarshalCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of parameter '" + parameter.NameInGeneratedCode + "' native representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			MarshalInfoWriterFor(writer.Context, parameter).WriteMarshalCleanupVariable(bodyWriter, valueName, metadataAccess, parameter.NameInGeneratedCode);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override void WriteMarshalCleanupReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of return value native representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, methodReturnType);
			string variableName = defaultMarshalInfoWriter.UndecorateVariable(writer.Context, writer.Context.Global.Services.Naming.ForInteropReturnValue());
			defaultMarshalInfoWriter.WriteMarshalCleanupOutVariable(bodyWriter, variableName, metadataAccess);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}
}
