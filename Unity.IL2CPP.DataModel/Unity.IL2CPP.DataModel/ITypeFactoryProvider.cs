using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public interface ITypeFactoryProvider
{
	ITypeFactory TypeFactory { get; }
}
