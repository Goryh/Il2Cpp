using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Contexts;

public class MethodWriteContext : ITypeFactoryProvider
{
	public readonly GlobalWriteContext Global;

	public readonly AssemblyWriteContext Assembly;

	public readonly MethodReference MethodReference;

	public readonly MethodDefinition MethodDefinition;

	public readonly TypeResolver TypeResolver;

	public readonly TypeReference ResolvedReturnType;

	public SourceWritingContext SourceWritingContext => Assembly.SourceWritingContext;

	ITypeFactory ITypeFactoryProvider.TypeFactory => Global.Services.TypeFactory;

	public MethodWriteContext(AssemblyWriteContext assembly, MethodReference method)
	{
		Global = assembly.Global;
		Assembly = assembly;
		MethodReference = method;
		MethodDefinition = method.Resolve();
		TypeResolver = assembly.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
		ResolvedReturnType = TypeResolver.ResolveReturnType(MethodReference);
	}

	public MinimalContext AsMinimal()
	{
		return Global.CreateMinimalContext();
	}

	public ReadOnlyContext AsReadonly()
	{
		return Global.GetReadOnlyContext();
	}

	public static implicit operator ReadOnlyContext(MethodWriteContext c)
	{
		return c.AsReadonly();
	}

	public static implicit operator MinimalContext(MethodWriteContext c)
	{
		return c.AsMinimal();
	}
}
