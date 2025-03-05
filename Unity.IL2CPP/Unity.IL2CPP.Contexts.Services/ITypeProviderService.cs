using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Services;

public interface ITypeProviderService
{
	AssemblyDefinition Corlib { get; }

	TypeDefinition SystemObject { get; }

	TypeDefinition SystemString { get; }

	TypeDefinition SystemArray { get; }

	TypeDefinition SystemException { get; }

	TypeDefinition SystemDelegate { get; }

	TypeDefinition SystemMulticastDelegate { get; }

	TypeDefinition SystemByte { get; }

	TypeDefinition SystemUInt16 { get; }

	TypeDefinition SystemIntPtr { get; }

	TypeDefinition SystemUIntPtr { get; }

	TypeDefinition SystemVoid { get; }

	PointerType SystemVoidPointer { get; }

	TypeDefinition SystemNullable { get; }

	TypeDefinition SystemType { get; }

	TypeDefinition TypedReference { get; }

	TypeReference Int32TypeReference { get; }

	TypeReference Int16TypeReference { get; }

	TypeReference UInt16TypeReference { get; }

	TypeReference SByteTypeReference { get; }

	TypeReference ByteTypeReference { get; }

	TypeReference BoolTypeReference { get; }

	TypeReference CharTypeReference { get; }

	TypeReference IntPtrTypeReference { get; }

	TypeReference UIntPtrTypeReference { get; }

	TypeReference Int64TypeReference { get; }

	TypeReference UInt32TypeReference { get; }

	TypeReference UInt64TypeReference { get; }

	TypeReference SingleTypeReference { get; }

	TypeReference DoubleTypeReference { get; }

	TypeReference ObjectTypeReference { get; }

	TypeReference StringTypeReference { get; }

	TypeReference RuntimeTypeHandleTypeReference { get; }

	TypeReference RuntimeMethodHandleTypeReference { get; }

	TypeReference RuntimeFieldHandleTypeReference { get; }

	TypeReference RuntimeArgumentHandleTypeReference { get; }

	TypeReference IActivationFactoryTypeReference { get; }

	TypeReference IIterableTypeReference { get; }

	TypeReference IBindableIterableTypeReference { get; }

	TypeReference IBindableIteratorTypeReference { get; }

	TypeReference IPropertyValueType { get; }

	TypeReference IReferenceType { get; }

	TypeReference IReferenceArrayType { get; }

	TypeDefinition IStringableType { get; }

	TypeReference Il2CppComObjectTypeReference { get; }

	TypeReference Il2CppComDelegateTypeReference { get; }

	TypeReference Il2CppFullySharedGenericTypeReference { get; }

	TypeDefinition ConstantSplittableMapType { get; }

	IResolvedTypeProviderService Resolved { get; }

	ReadOnlyCollection<TypeDefinition> GraftedArrayInterfaceTypes { get; }

	ReadOnlyCollection<MethodDefinition> GraftedArrayInterfaceMethods { get; }

	TypeDefinition GetSystemType(SystemType systemType);
}
