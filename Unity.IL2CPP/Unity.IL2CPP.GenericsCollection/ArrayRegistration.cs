using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection;

internal static class ArrayRegistration
{
	public static bool ShouldForce2DArrayFor(ReadOnlyContext context, TypeDefinition type)
	{
		return type.MetadataType == MetadataType.Single;
	}
}
