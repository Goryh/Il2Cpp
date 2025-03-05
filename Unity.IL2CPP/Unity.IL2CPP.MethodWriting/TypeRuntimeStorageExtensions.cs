using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public static class TypeRuntimeStorageExtensions
{
	public static bool IsVariableSized(this RuntimeStorageKind runtimeStorageKind)
	{
		if (runtimeStorageKind != RuntimeStorageKind.VariableSizedAny)
		{
			return runtimeStorageKind == RuntimeStorageKind.VariableSizedValueType;
		}
		return true;
	}

	public static bool IsByValue(this RuntimeStorageKind runtimeStorageKind)
	{
		if (runtimeStorageKind != RuntimeStorageKind.ValueType)
		{
			return runtimeStorageKind == RuntimeStorageKind.VariableSizedValueType;
		}
		return true;
	}
}
