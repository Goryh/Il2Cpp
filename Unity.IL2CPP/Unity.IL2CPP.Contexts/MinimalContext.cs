using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Contexts;

public class MinimalContext : ITypeFactoryProvider
{
	public readonly GlobalMinimalContext Global;

	ITypeFactory ITypeFactoryProvider.TypeFactory => Global.Services.TypeFactory;

	public MinimalContext(GlobalMinimalContext context)
	{
		Global = context;
	}

	public ReadOnlyContext AsReadonly()
	{
		return Global.GetReadOnlyContext();
	}

	public static implicit operator ReadOnlyContext(MinimalContext d)
	{
		return d.AsReadonly();
	}
}
