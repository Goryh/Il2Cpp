using Unity.IL2CPP.DataModel.BuildLogic;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

internal class ParameterDefinitionBuilder
{
	private readonly EditContext _context;

	private readonly string _name;

	private readonly ParameterAttributes _attributes;

	private readonly TypeReference _parameterType;

	internal ParameterDefinitionBuilder(EditContext context, string name, ParameterAttributes attributes, TypeReference parameterType)
	{
		_context = context;
		_name = name;
		_attributes = attributes;
		_parameterType = parameterType;
	}

	internal ParameterDefinition Complete(int index, MetadataToken metadataToken)
	{
		ParameterDefinition parameterDefinition = new ParameterDefinition(_name, _attributes, index, ReadOnlyCollectionCache<CustomAttribute>.Empty, null, hasConstant: false, null, metadataToken);
		parameterDefinition.InitializeParameterType(_parameterType);
		return parameterDefinition;
	}
}
