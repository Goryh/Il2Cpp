namespace Unity.IL2CPP.DataModel.Modify.Definitions;

internal interface IAssemblyDefinitionUpdater
{
	void AddGeneratedMethod(MethodDefinition method);

	void AddGeneratedEvent(EventDefinition @event);

	void AddGeneratedProperty(PropertyDefinition property);

	void AddGeneratedField(FieldDefinition field);

	void AddGeneratedType(TypeDefinition type);
}
