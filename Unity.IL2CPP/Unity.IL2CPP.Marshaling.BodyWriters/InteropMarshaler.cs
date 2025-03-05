using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Marshaling.BodyWriters;

public abstract class InteropMarshaler
{
	protected readonly TypeResolver _typeResolver;

	protected readonly MarshalType _marshalType;

	protected readonly bool _useUnicodeCharset;

	public InteropMarshaler(TypeResolver typeResolver, MarshalType marshalType, bool useUnicodeCharset)
	{
		_typeResolver = typeResolver;
		_marshalType = marshalType;
		_useUnicodeCharset = useUnicodeCharset;
	}

	public abstract bool CanMarshalAsInputParameter(ReadOnlyContext context, MarshaledParameter parameter, out DefaultMarshalInfoWriter marshalInfoWriter);

	public abstract bool CanMarshalAsOutputParameter(ReadOnlyContext context, MarshaledParameter parameter, DefaultMarshalInfoWriter marshalInfoWriter);

	public abstract bool CanMarshalAsOutputParameter(ReadOnlyContext context, MethodReturnType methodReturnType, out DefaultMarshalInfoWriter marshalInfoWriter);

	public abstract string GetPrettyCalleeName();

	public abstract string WriteMarshalEmptyInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess);

	public abstract string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess);

	public abstract void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess);

	public abstract string WriteMarshalReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess);

	public abstract void WriteMarshalCleanupEmptyParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess);

	public abstract void WriteMarshalCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess);

	public abstract void WriteMarshalCleanupReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IRuntimeMetadataAccess metadataAccess);

	public virtual DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MarshaledParameter parameter)
	{
		return MarshalDataCollector.MarshalInfoWriterFor(context, parameter.ParameterType, _marshalType, parameter.MarshalInfo, _useUnicodeCharset);
	}

	public virtual DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MethodReturnType methodReturnType)
	{
		return MarshalDataCollector.MarshalInfoWriterFor(context, _typeResolver.Resolve(methodReturnType.ReturnType), _marshalType, methodReturnType.MarshalInfo, _useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: false, forReturnValue: true);
	}
}
