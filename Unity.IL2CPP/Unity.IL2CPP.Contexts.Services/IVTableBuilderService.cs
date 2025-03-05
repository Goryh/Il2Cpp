using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Contexts.Services;

public interface IVTableBuilderService
{
	int IndexFor(ReadOnlyContext context, MethodDefinition method);

	VTable VTableFor(ReadOnlyContext context, TypeReference typeReference);

	MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method, out VTableMultipleGenericInterfaceImpls multipleGenericInterfaceImpls);
}
