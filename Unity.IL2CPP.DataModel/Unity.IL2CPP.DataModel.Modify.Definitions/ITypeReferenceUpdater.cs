namespace Unity.IL2CPP.DataModel.Modify.Definitions;

internal interface ITypeReferenceUpdater
{
	void ClearInterfaceTypesCache();

	void ClearMethodsCache();

	void ClearFieldTypes();
}
