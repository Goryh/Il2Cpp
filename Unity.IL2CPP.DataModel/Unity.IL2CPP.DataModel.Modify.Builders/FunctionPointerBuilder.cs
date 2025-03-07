using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class FunctionPointerBuilder
{
	private readonly EditContext _context;

	private readonly TypeReference _returnType;

	private readonly MethodCallingConvention _callingConvention;

	private bool _hasThis = true;

	private readonly List<ParameterDefinitionBuilder> _parameters = new List<ParameterDefinitionBuilder>();

	internal FunctionPointerBuilder(EditContext context, TypeReference returnType, MethodCallingConvention callingConvention = MethodCallingConvention.Default)
	{
		_context = context;
		_returnType = returnType;
		_callingConvention = callingConvention;
	}

	public FunctionPointerBuilder WithParametersClonedFrom(MethodDefinition sourceMethod)
	{
		foreach (ParameterDefinition parameter in sourceMethod.Parameters)
		{
			AddParameter(parameter.Name, parameter.Attributes, parameter.ParameterType);
		}
		return this;
	}

	public FunctionPointerBuilder AddParameter(string name, ParameterAttributes attributes, TypeReference parameterType)
	{
		_parameters.Add(new ParameterDefinitionBuilder(_context, name, attributes, parameterType));
		return this;
	}

	public FunctionPointerBuilder WithParametersClonedFrom(MethodDefinition sourceMethod, TypeResolver parameterTypeResolver)
	{
		foreach (ParameterDefinition sourceParameter in sourceMethod.Parameters)
		{
			TypeReference parameterType = parameterTypeResolver.Resolve(sourceParameter.ParameterType);
			_parameters.Add(new ParameterDefinitionBuilder(_context, sourceParameter.Name, sourceParameter.Attributes, parameterType));
		}
		return this;
	}

	public FunctionPointerType Complete()
	{
		FunctionPointerType functionPointerType = new FunctionPointerType(_returnType, CompleteParameters(), _callingConvention, _hasThis, explicitThis: false, _context.Context);
		ReferencePopulater.PopulateTypeRefProperties(functionPointerType);
		GenericParameterProviderPopulater.InitializeEmpty(functionPointerType);
		return functionPointerType;
	}

	private ReadOnlyCollection<ParameterDefinition> CompleteParameters()
	{
		if (_parameters.Count == 0)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		List<ParameterDefinition> parameters = new List<ParameterDefinition>(_parameters.Count);
		foreach (ParameterDefinitionBuilder builder in _parameters)
		{
			parameters.Add(builder.Complete(parameters.Count, MetadataToken.ParamZero));
		}
		return parameters.AsReadOnly();
	}
}
