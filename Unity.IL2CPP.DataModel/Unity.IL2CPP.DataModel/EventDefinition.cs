using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public sealed class EventDefinition : MemberReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider
{
	internal readonly Mono.Cecil.EventDefinition Definition;

	private TypeReference _eventType;

	internal bool IsDataModelGenerated { get; }

	public EventAttributes Attributes { get; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public MethodDefinition AddMethod { get; }

	public MethodDefinition RemoveMethod { get; }

	public MethodDefinition InvokeMethod { get; }

	public ReadOnlyCollection<MethodDefinition> OtherMethods { get; }

	public bool HasOtherMethods => OtherMethods.Count > 0;

	public TypeReference EventType
	{
		get
		{
			if (_eventType == null)
			{
				ThrowDataNotInitialized("EventType");
			}
			return _eventType;
		}
	}

	public override string FullName
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsDefinition => true;

	public bool IsSpecialName => Attributes.HasFlag(EventAttributes.SpecialName);

	public bool IsRuntimeSpecialName => Attributes.HasFlag(EventAttributes.RTSpecialName);

	public new TypeDefinition DeclaringType => (TypeDefinition)base.DeclaringType;

	protected override bool IsFullNameBuilt => false;

	internal EventDefinition(TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttributes, MethodDefinition addMethod, MethodDefinition removeMethod, MethodDefinition invokeMethod, ReadOnlyCollection<MethodDefinition> otherMethods, Mono.Cecil.EventDefinition definition)
		: this(declaringType, definition.Name, (EventAttributes)definition.Attributes, customAttributes, addMethod, removeMethod, invokeMethod, otherMethods, MetadataToken.FromCecil(definition), isDataModelGenerated: false)
	{
		Definition = definition;
	}

	internal EventDefinition(TypeDefinition declaringType, string name, EventAttributes attributes, ReadOnlyCollection<CustomAttribute> customAttributes, MethodDefinition addMethod, MethodDefinition removeMethod, MethodDefinition invokeMethod, ReadOnlyCollection<MethodDefinition> otherMethods, MetadataToken metadataToken, bool isDataModelGenerated = true)
		: base(declaringType, metadataToken)
	{
		InitializeName(name);
		CustomAttributes = customAttributes;
		AddMethod = addMethod;
		RemoveMethod = removeMethod;
		InvokeMethod = invokeMethod;
		OtherMethods = otherMethods;
		Attributes = attributes;
		IsDataModelGenerated = isDataModelGenerated;
	}

	public override string ToString()
	{
		return Name;
	}

	internal void InitializeEventType(TypeReference eventType)
	{
		_eventType = eventType;
	}
}
