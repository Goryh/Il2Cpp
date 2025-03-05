using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime;

internal class KeyValuePairCCWWriter : IProjectedComCallableWrapperMethodWriter
{
	private readonly TypeDefinition _keyValuePairTypeDef;

	public KeyValuePairCCWWriter(TypeDefinition keyValuePairTypeDef)
	{
		_keyValuePairTypeDef = keyValuePairTypeDef;
	}

	public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
	}

	public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference interfaceMethod)
	{
		GenericInstanceType iKeyValuePair = (GenericInstanceType)interfaceMethod.DeclaringType;
		GenericInstanceType keyValuePairInstance = context.Global.Services.TypeFactory.CreateGenericInstanceType(_keyValuePairTypeDef, _keyValuePairTypeDef.DeclaringType, iKeyValuePair.GenericArguments.ToArray());
		MethodDefinition keyValuePairMethod = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == interfaceMethod.Name);
		MethodReference resolvedKeyValuePairMethod = context.Global.Services.TypeFactory.ResolverFor(keyValuePairInstance).Resolve(keyValuePairMethod);
		return new ProjectedMethodBodyWriter(context, resolvedKeyValuePairMethod, interfaceMethod);
	}
}
