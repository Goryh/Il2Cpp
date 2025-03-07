using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Inflation;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class MethodDefinitionBuilder
{
	private readonly EditContext _context;

	private string _name;

	private MethodAttributes _attributes;

	private TypeReference _returnType;

	private MethodImplAttributes _implAttributes;

	private bool _hasThis;

	private readonly List<ParameterDefinitionBuilder> _parameters = new List<ParameterDefinitionBuilder>();

	private readonly List<VariableDefinitionBuilder> _variables = new List<VariableDefinitionBuilder>();

	private List<MethodReference> _overrides = new List<MethodReference>();

	private MethodBodyBuilder _bodyBuilder;

	internal MethodDefinitionBuilder(EditContext context, string name, TypeReference returnType)
		: this(context, name, MethodAttributes.Public, returnType)
	{
	}

	internal MethodDefinitionBuilder(EditContext context, string name, MethodAttributes attributes, TypeReference returnType)
	{
		_context = context;
		_name = name;
		_attributes = attributes;
		_returnType = returnType;
		_hasThis = !attributes.HasFlag(MethodAttributes.Static);
	}

	public ILProcessorBuilder WithBody(Action<ILProcessorBuilder> bodyBuilder)
	{
		_bodyBuilder = new MethodBodyBuilder(this);
		ILProcessorBuilder ilProcessor = _bodyBuilder.IlProcessorBuilder;
		bodyBuilder(ilProcessor);
		return ilProcessor;
	}

	public MethodDefinitionBuilder WithEmptyBody()
	{
		_bodyBuilder = new MethodBodyBuilder(this);
		return this;
	}

	public MethodDefinitionBuilder WithMethodImplAttributes(MethodImplAttributes attributes)
	{
		_implAttributes = attributes;
		return this;
	}

	public MethodDefinitionBuilder WithParametersClonedFrom(MethodDefinition sourceMethod)
	{
		foreach (ParameterDefinition parameter in sourceMethod.Parameters)
		{
			AddParameter(parameter.Name, parameter.Attributes, parameter.ParameterType);
		}
		return this;
	}

	public MethodDefinitionBuilder AddParameter(string name, ParameterAttributes attributes, TypeReference parameterType)
	{
		_parameters.Add(new ParameterDefinitionBuilder(_context, name, attributes, parameterType));
		return this;
	}

	public MethodDefinitionBuilder AddVariable(TypeReference variableType)
	{
		_variables.Add(new VariableDefinitionBuilder(variableType));
		return this;
	}

	public MethodDefinitionBuilder WithParametersClonedFrom(MethodDefinition sourceMethod, TypeResolver parameterTypeResolver)
	{
		foreach (ParameterDefinition sourceParameter in sourceMethod.Parameters)
		{
			TypeReference parameterType = parameterTypeResolver.Resolve(sourceParameter.ParameterType);
			_parameters.Add(new ParameterDefinitionBuilder(_context, sourceParameter.Name, sourceParameter.Attributes, parameterType));
		}
		return this;
	}

	public MethodDefinitionBuilder WithOverride(MethodReference @override)
	{
		_overrides.Add(@override);
		return this;
	}

	public MethodDefinition Complete(TypeDefinition declaringType)
	{
		return Complete(declaringType, _context.Context.CreateThreadSafeFactoryForFullConstruction(), typeUnderConstruction: false, updateInflatedInstances: true);
	}

	internal MethodDefinition CompleteBuildStage(TypeDefinition declaringType, ITypeFactory typeFactory)
	{
		return Complete(declaringType, typeFactory, typeUnderConstruction: false, updateInflatedInstances: false);
	}

	internal MethodDefinition CompleteWithoutUpdatingInflations(TypeDefinition declaringType)
	{
		return Complete(declaringType, _context.Context.CreateThreadSafeFactoryForFullConstruction(), typeUnderConstruction: false, updateInflatedInstances: true);
	}

	internal MethodDefinition Complete(TypeDefinition declaringType, ITypeFactory typeFactory, bool typeUnderConstruction, bool updateInflatedInstances)
	{
		MethodDefinition definition = new MethodDefinition(_name, declaringType, CompleteParameters(declaringType), ReadOnlyCollectionCache<CustomAttribute>.Empty, new MethodReturnType(_returnType), _attributes, _implAttributes, MethodCallingConvention.Default, _hasThis, declaringType.Assembly.IssueNewMethodToken(), PInvokeInfo.None(), null, isWindowsRuntimeProjection: false, requiresRidForNameUniqueness: false);
		((IAssemblyDefinitionUpdater)declaringType.Assembly).AddGeneratedMethod(definition);
		definition.InitializeOverrides(_overrides.AsReadOnly());
		MethodBody body = CompleteBody(definition);
		definition.InitializeMethodBody(body);
		if (body != null)
		{
			definition.InitializeDebugInfo(CompletedDebugInformation(definition));
		}
		definition.InitializeReturnType(definition.MethodReturnType.ReturnType);
		definition.MethodReturnType.InitializeReturnType(definition.ReturnType);
		GenericParameterProviderPopulater.InitializeEmpty(definition);
		DefinitionInflater.PopulateMethodDefinitionInflatedProperties(_context.Context, typeFactory, definition);
		DefinitionPopulater.PopulateMethodDefinitionProperties(definition);
		ReferencePopulater.PopulateMethodRefProperties(definition);
		if (!typeUnderConstruction)
		{
			((ITypeDefinitionUpdater)declaringType).AddMethod(definition);
		}
		if (updateInflatedInstances)
		{
			UpdateInflatedInstances(definition.DeclaringType);
		}
		return definition;
	}

	private void UpdateInflatedInstances(TypeDefinition modifiedType)
	{
		foreach (TypeReference inflatedType in _context.Context.AllKnownNonDefinitionTypesUnordered())
		{
			if (inflatedType.Resolve() == modifiedType)
			{
				((ITypeReferenceUpdater)inflatedType).ClearMethodsCache();
			}
		}
	}

	private MethodBody CompleteBody(MethodDefinition method)
	{
		if (_bodyBuilder == null)
		{
			return null;
		}
		MethodBody methodBody = new MethodBody(method, CreateThisParameter(method), initLocals: false, 0, CompleteVariables(), _bodyBuilder.IlProcessorBuilder.Instructions, ReadOnlyCollectionCache<ExceptionHandler>.Empty);
		methodBody.OptimizeMacros();
		return methodBody;
	}

	private MethodDebugInfo CompletedDebugInformation(MethodDefinition method)
	{
		MethodDebugInfo methodDebugInfo = new MethodDebugInfo(method, ReadOnlyCollectionCache<ScopeDebugInfo>.Empty);
		methodDebugInfo.InitializeDebugInformation(ReadOnlyCollectionCache<SequencePoint>.Empty);
		return methodDebugInfo;
	}

	private ReadOnlyCollection<ParameterDefinition> CompleteParameters(TypeDefinition type)
	{
		if (_parameters.Count == 0)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		List<ParameterDefinition> parameters = new List<ParameterDefinition>(_parameters.Count);
		foreach (ParameterDefinitionBuilder builder in _parameters)
		{
			parameters.Add(builder.Complete(parameters.Count, type.Assembly.IssueNewParameterDefinitionToken()));
		}
		return parameters.AsReadOnly();
	}

	private ReadOnlyCollection<VariableDefinition> CompleteVariables()
	{
		if (_variables.Count == 0)
		{
			return ReadOnlyCollectionCache<VariableDefinition>.Empty;
		}
		List<VariableDefinition> variables = new List<VariableDefinition>(_variables.Count);
		foreach (VariableDefinitionBuilder builder in _variables)
		{
			variables.Add(builder.Complete(variables.Count));
		}
		return variables.AsReadOnly();
	}

	private ParameterDefinition CreateThisParameter(MethodDefinition method)
	{
		if (_hasThis)
		{
			return ParameterDefinition.MakeThisParameter(method.DeclaringType, method.DeclaringType.Assembly.IssueNewParameterDefinitionToken());
		}
		return null;
	}
}
