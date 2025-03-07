using System.Collections.Generic;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class EventDefinitionBuilder
{
	private readonly EditContext _context;

	private string _name;

	private EventAttributes _attributes;

	private TypeReference _eventType;

	private MethodDefinition _addMethod;

	private MethodDefinition _removeMethod;

	private MethodDefinition _invokeMethod;

	private List<MethodDefinition> _otherMethods = new List<MethodDefinition>();

	internal EventDefinitionBuilder(EditContext context, string name, EventAttributes attributes, TypeReference eventType)
	{
		_context = context;
		_name = name;
		_attributes = attributes;
		_eventType = eventType;
	}

	public EventDefinitionBuilder WithAddMethod(MethodDefinition method)
	{
		_addMethod = method;
		return this;
	}

	public EventDefinitionBuilder WithRemoveMethod(MethodDefinition method)
	{
		_removeMethod = method;
		return this;
	}

	public EventDefinitionBuilder WithInvokeMethod(MethodDefinition method)
	{
		_invokeMethod = method;
		return this;
	}

	public EventDefinitionBuilder WithOtherMethod(MethodDefinition method)
	{
		_otherMethods.Add(method);
		return this;
	}

	public EventDefinition Complete(TypeDefinition declaringType, bool typeUnderConstruction = false)
	{
		EventDefinition definition = new EventDefinition(declaringType, _name, _attributes, ReadOnlyCollectionCache<CustomAttribute>.Empty, _addMethod, _removeMethod, _invokeMethod, _otherMethods.AsReadOnly(), declaringType.Assembly.IssueNewEventToken());
		definition.InitializeEventType(_eventType);
		if (!typeUnderConstruction)
		{
			((IAssemblyDefinitionUpdater)declaringType.Assembly).AddGeneratedEvent(definition);
			((ITypeDefinitionUpdater)declaringType).AddEvent(definition);
		}
		return definition;
	}
}
