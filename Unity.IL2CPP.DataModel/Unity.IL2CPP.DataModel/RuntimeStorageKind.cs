namespace Unity.IL2CPP.DataModel;

public enum RuntimeStorageKind
{
	Pointer = 1,
	ReferenceType,
	ValueType,
	VariableSizedValueType,
	VariableSizedAny
}
