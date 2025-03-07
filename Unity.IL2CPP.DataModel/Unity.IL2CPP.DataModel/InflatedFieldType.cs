namespace Unity.IL2CPP.DataModel;

public readonly struct InflatedFieldType
{
	public readonly FieldDefinition Field;

	public readonly TypeReference InflatedType;

	public InflatedFieldType(FieldDefinition field, TypeReference inflatedType)
	{
		Field = field;
		InflatedType = inflatedType;
	}
}
