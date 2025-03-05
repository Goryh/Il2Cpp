using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Contexts;

public class ReadOnlyContext : ITypeFactoryProvider
{
	public readonly GlobalReadOnlyContext Global;

	ITypeFactory ITypeFactoryProvider.TypeFactory => Global.Services.TypeFactory;

	public ReadOnlyContext(GlobalReadOnlyContext context)
	{
		Global = context;
	}
}
