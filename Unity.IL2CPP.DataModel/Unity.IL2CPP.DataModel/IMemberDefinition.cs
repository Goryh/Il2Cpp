namespace Unity.IL2CPP.DataModel;

public interface IMemberDefinition : ICustomAttributeProvider, IMetadataTokenProvider
{
	string Name { get; }

	string FullName { get; }

	bool IsSpecialName { get; }

	bool IsRuntimeSpecialName { get; }

	TypeDefinition DeclaringType { get; }
}
