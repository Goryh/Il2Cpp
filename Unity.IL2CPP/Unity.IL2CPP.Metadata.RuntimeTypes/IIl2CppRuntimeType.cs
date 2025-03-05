using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

public interface IIl2CppRuntimeType
{
	TypeReference Type { get; }

	int Attrs { get; }
}
