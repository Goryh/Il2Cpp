using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public static class RuntimeMetadataAccessExtensions
{
	public static string FieldInfo(this IRuntimeMetadataAccess runtimeMetadataAccess, FieldReference fieldReference)
	{
		return runtimeMetadataAccess.FieldInfo(fieldReference, fieldReference.DeclaringType);
	}
}
