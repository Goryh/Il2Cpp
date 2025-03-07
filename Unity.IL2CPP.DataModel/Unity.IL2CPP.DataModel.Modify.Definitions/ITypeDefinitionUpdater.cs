using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Modify.Definitions;

internal interface ITypeDefinitionUpdater
{
	bool BaseTypeHasBeenUpdated { get; }

	void UpdateBaseType(TypeReference newBaseType);

	void AddMethod(MethodDefinition method);

	void AddEvent(EventDefinition @event);

	void AddProperty(PropertyDefinition property);

	void AddField(FieldDefinition field);

	void AddInterfaceImplementations(IEnumerable<InterfaceImplementation> interfaceImplementations);

	void AddNestedType(TypeDefinition type);
}
