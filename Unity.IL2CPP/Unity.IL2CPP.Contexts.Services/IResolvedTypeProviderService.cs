using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.Contexts.Services;

public interface IResolvedTypeProviderService
{
	ResolvedTypeInfo SystemObject { get; }

	ResolvedTypeInfo SystemString { get; }

	ResolvedTypeInfo SystemArray { get; }

	ResolvedTypeInfo SystemException { get; }

	ResolvedTypeInfo SystemDelegate { get; }

	ResolvedTypeInfo SystemMulticastDelegate { get; }

	ResolvedTypeInfo SystemByte { get; }

	ResolvedTypeInfo SystemUInt16 { get; }

	ResolvedTypeInfo SystemIntPtr { get; }

	ResolvedTypeInfo SystemUIntPtr { get; }

	ResolvedTypeInfo SystemVoid { get; }

	ResolvedTypeInfo SystemVoidPointer { get; }

	ResolvedTypeInfo SystemNullable { get; }

	ResolvedTypeInfo SystemType { get; }

	ResolvedTypeInfo TypedReference { get; }

	ResolvedTypeInfo Int32TypeReference { get; }

	ResolvedTypeInfo Int16TypeReference { get; }

	ResolvedTypeInfo UInt16TypeReference { get; }

	ResolvedTypeInfo SByteTypeReference { get; }

	ResolvedTypeInfo ByteTypeReference { get; }

	ResolvedTypeInfo BoolTypeReference { get; }

	ResolvedTypeInfo CharTypeReference { get; }

	ResolvedTypeInfo IntPtrTypeReference { get; }

	ResolvedTypeInfo UIntPtrTypeReference { get; }

	ResolvedTypeInfo Int64TypeReference { get; }

	ResolvedTypeInfo UInt32TypeReference { get; }

	ResolvedTypeInfo UInt64TypeReference { get; }

	ResolvedTypeInfo SingleTypeReference { get; }

	ResolvedTypeInfo DoubleTypeReference { get; }

	ResolvedTypeInfo ObjectTypeReference { get; }

	ResolvedTypeInfo StringTypeReference { get; }

	ResolvedTypeInfo RuntimeTypeHandleTypeReference { get; }

	ResolvedTypeInfo RuntimeMethodHandleTypeReference { get; }

	ResolvedTypeInfo RuntimeFieldHandleTypeReference { get; }

	ResolvedTypeInfo RuntimeArgumentHandleTypeReference { get; }
}
