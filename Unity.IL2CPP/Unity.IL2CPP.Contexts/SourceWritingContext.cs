using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Contexts;

public class SourceWritingContext : ITypeFactoryProvider
{
	public readonly GlobalWriteContext Global;

	ITypeFactory ITypeFactoryProvider.TypeFactory => Global.Services.TypeFactory;

	public SourceWritingContext(GlobalWriteContext context)
	{
		Global = context;
	}

	public MinimalContext AsMinimal()
	{
		return Global.CreateMinimalContext();
	}

	public ReadOnlyContext AsReadonly()
	{
		return Global.GetReadOnlyContext();
	}

	public static implicit operator ReadOnlyContext(SourceWritingContext c)
	{
		return c.AsReadonly();
	}

	public static implicit operator MinimalContext(SourceWritingContext c)
	{
		return c.AsMinimal();
	}

	public AssemblyWriteContext CreateAssemblyWritingContext(AssemblyDefinition assembly)
	{
		return new AssemblyWriteContext(this, assembly);
	}

	public AssemblyWriteContext CreateAssemblyWritingContext(MethodReference method)
	{
		return CreateAssemblyWritingContext(method.Resolve().Module.Assembly);
	}

	public MethodWriteContext CreateMethodWritingContext(MethodReference method)
	{
		return new MethodWriteContext(CreateAssemblyWritingContext(method.Resolve().Module.Assembly), method);
	}
}
