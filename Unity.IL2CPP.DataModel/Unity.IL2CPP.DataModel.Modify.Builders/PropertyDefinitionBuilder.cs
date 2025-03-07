using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class PropertyDefinitionBuilder
{
	private readonly EditContext _context;

	private string _name;

	private PropertyAttributes _attributes;

	private TypeReference _propertyType;

	private MethodDefinition _getMethod;

	private MethodDefinition _setMethod;

	internal PropertyDefinitionBuilder(EditContext context, string name, PropertyAttributes attributes, TypeReference propertyType)
	{
		_context = context;
		_name = name;
		_attributes = attributes;
		_propertyType = propertyType;
	}

	public PropertyDefinitionBuilder WithGetMethod(MethodDefinition method)
	{
		_getMethod = method;
		return this;
	}

	public PropertyDefinitionBuilder WithSetMethod(MethodDefinition method)
	{
		_setMethod = method;
		return this;
	}

	public PropertyDefinition Complete(TypeDefinition declaringType, bool typeUnderConstruction = false)
	{
		PropertyDefinition definition = new PropertyDefinition(declaringType, _name, _attributes, ReadOnlyCollectionCache<CustomAttribute>.Empty, BuildParameterDefinitions(), _getMethod, _setMethod, declaringType.Assembly.IssueNewPropertyToken());
		definition.InitializePropertyType(_propertyType);
		if (!typeUnderConstruction)
		{
			((IAssemblyDefinitionUpdater)declaringType.Assembly).AddGeneratedProperty(definition);
			((ITypeDefinitionUpdater)declaringType).AddProperty(definition);
		}
		return definition;
	}

	private ReadOnlyCollection<ParameterDefinition> BuildParameterDefinitions()
	{
		if (_getMethod != null)
		{
			return MirrorParameters(_getMethod, 0);
		}
		if (_setMethod != null)
		{
			return MirrorParameters(_setMethod, 1);
		}
		return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
	}

	private static ReadOnlyCollection<ParameterDefinition> MirrorParameters(MethodDefinition method, int bound)
	{
		if (!method.HasParameters)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		List<ParameterDefinition> parameters = new List<ParameterDefinition>();
		ReadOnlyCollection<ParameterDefinition> methodParameters = method.Parameters;
		int end = methodParameters.Count - bound;
		for (int i = 0; i < end; i++)
		{
			parameters.Add(methodParameters[i]);
		}
		return parameters.AsReadOnly();
	}
}
