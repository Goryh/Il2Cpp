using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal interface IHasDefinition
{
	IMemberDefinition GetDefinition();
}
