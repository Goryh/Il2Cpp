using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

internal class NativeToManagedMarshaler : InteropMarshaler
{
	public NativeToManagedMarshaler(TypeResolver typeResolver, MarshalType marshalType, bool useUnicodeCharset)
		: base(typeResolver, marshalType, useUnicodeCharset)
	{
	}

	public override bool CanMarshalAsInputParameter(ReadOnlyContext context, MarshaledParameter parameter, out DefaultMarshalInfoWriter marshalInfoWriter)
	{
		marshalInfoWriter = MarshalInfoWriterFor(context, parameter);
		return marshalInfoWriter.CanMarshalTypeFromNative(context);
	}

	public override bool CanMarshalAsOutputParameter(ReadOnlyContext context, MarshaledParameter parameter, DefaultMarshalInfoWriter marshalInfoWriter)
	{
		return marshalInfoWriter.CanMarshalTypeToNative(context);
	}

	public override bool CanMarshalAsOutputParameter(ReadOnlyContext context, MethodReturnType methodReturnType, out DefaultMarshalInfoWriter marshalInfoWriter)
	{
		marshalInfoWriter = MarshalInfoWriterFor(context, methodReturnType);
		return marshalInfoWriter.CanMarshalTypeToNative(context);
	}

	public override string GetPrettyCalleeName()
	{
		return "Managed method";
	}

	public override string WriteMarshalEmptyInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
	{
		return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			if (bodyWriter.Context.Global.Parameters.EmitComments)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' to managed representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, parameter);
			return defaultMarshalInfoWriter.WriteMarshalEmptyVariableFromNative(bodyWriter, defaultMarshalInfoWriter.UndecorateVariable(writer.Context, parameter.NameInGeneratedCode), parameters, metadataAccess);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
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
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' to managed representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, parameter);
			return defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, defaultMarshalInfoWriter.UndecorateVariable(writer.Context, parameter.NameInGeneratedCode), parameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: true, metadataAccess);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
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
				bodyWriter.WriteCommentedLine("Marshaling of parameter '" + parameter.NameInGeneratedCode + "' back from managed representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			MarshalInfoWriterFor(writer.Context, parameter).WriteMarshalOutParameterToNative(bodyWriter, new ManagedMarshalValue(valueName), parameter.NameInGeneratedCode, parameter.Name, parameters, metadataAccess);
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
				bodyWriter.WriteCommentedLine("Marshaling of return value back from managed representation");
			}
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, methodReturnType);
			string objectVariableName = writer.Context.Global.Services.Naming.ForInteropReturnValue();
			return defaultMarshalInfoWriter.WriteMarshalReturnValueToNative(bodyWriter, new ManagedMarshalValue(objectVariableName), metadataAccess);
		}, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.WriteLine();
		});
	}

	public override void WriteMarshalCleanupEmptyParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
	}

	public override void WriteMarshalCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
	{
	}

	public override void WriteMarshalCleanupReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IRuntimeMetadataAccess metadataAccess)
	{
	}

	public override DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MarshaledParameter parameter)
	{
		return MarshalDataCollector.MarshalInfoWriterFor(context, parameter.ParameterType, _marshalType, parameter.MarshalInfo, _useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: false, forReturnValue: false, forNativeToManagedWrapper: true);
	}

	public override DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MethodReturnType methodReturnType)
	{
		return MarshalDataCollector.MarshalInfoWriterFor(context, _typeResolver.Resolve(methodReturnType.ReturnType), _marshalType, methodReturnType.MarshalInfo, _useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: false, forReturnValue: true, forNativeToManagedWrapper: true);
	}
}
