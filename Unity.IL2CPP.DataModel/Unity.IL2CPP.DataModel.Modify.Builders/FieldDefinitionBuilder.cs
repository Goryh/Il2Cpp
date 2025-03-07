using System;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class FieldDefinitionBuilder
{
	private readonly EditContext _context;

	private readonly string _fieldName;

	private readonly FieldAttributes _attributes;

	private readonly TypeReference _fieldType;

	private object _constant;

	private bool _hasConstant;

	internal FieldDefinitionBuilder(EditContext context, string fieldName, FieldAttributes attributes, TypeReference fieldType)
	{
		_context = context;
		_fieldName = fieldName;
		_attributes = attributes;
		_fieldType = fieldType;
	}

	public FieldDefinitionBuilder WithConstant(object constant)
	{
		_constant = constant;
		_hasConstant = true;
		return this;
	}

	public FieldDefinition Complete(TypeDefinition declaringType)
	{
		return Complete(declaringType, typeUnderConstruction: false, creatingFromBuildStage: false, updateInflatedInstances: true);
	}

	internal FieldDefinition CompleteBuildStage(TypeDefinition declaringType)
	{
		return Complete(declaringType, typeUnderConstruction: false, creatingFromBuildStage: true, updateInflatedInstances: false);
	}

	internal FieldDefinition Complete(TypeDefinition declaringType, bool typeUnderConstruction, bool creatingFromBuildStage, bool updateInflatedInstances)
	{
		FieldDefinition definition = new FieldDefinition(_fieldName, declaringType, _attributes, ReadOnlyCollectionCache<CustomAttribute>.Empty, null, 0, 0, Array.Empty<byte>(), declaringType.Assembly.IssueNewFieldToken(), isWindowsRuntimeProjection: false, _hasConstant, _constant);
		((IAssemblyDefinitionUpdater)declaringType.Assembly).AddGeneratedField(definition);
		definition.InitializeFieldType(_fieldType);
		if (!typeUnderConstruction && !creatingFromBuildStage)
		{
			((ITypeDefinitionUpdater)declaringType).AddField(definition);
			ReferencePopulater.PopulateFieldDefinitionProperties(definition);
			declaringType.InitializeTypeReferenceFieldTypes(declaringType.Fields.Select((FieldDefinition f) => new InflatedFieldType(f, f.FieldType)).ToArray().AsReadOnly());
		}
		if (updateInflatedInstances)
		{
			UpdateInflatedInstances(declaringType);
		}
		return definition;
	}

	private void UpdateInflatedInstances(TypeDefinition modifiedType)
	{
		foreach (TypeReference item in _context.Context.AllKnownNonDefinitionTypesUnordered())
		{
			if (item.Resolve() == modifiedType)
			{
				((ITypeReferenceUpdater)modifiedType).ClearFieldTypes();
			}
		}
	}
}
