using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.Contexts.Components;

public class TypeProviderComponent : ReusedServiceComponentBase<ITypeProviderService, TypeProviderComponent>, ITypeProviderService
{
	private class ResolvedTypeProvider : IResolvedTypeProviderService
	{
		public ResolvedTypeInfo SystemObject { get; }

		public ResolvedTypeInfo SystemString { get; }

		public ResolvedTypeInfo SystemArray { get; }

		public ResolvedTypeInfo SystemException { get; }

		public ResolvedTypeInfo SystemDelegate { get; }

		public ResolvedTypeInfo SystemMulticastDelegate { get; }

		public ResolvedTypeInfo SystemByte { get; }

		public ResolvedTypeInfo SystemUInt16 { get; }

		public ResolvedTypeInfo SystemIntPtr { get; }

		public ResolvedTypeInfo SystemUIntPtr { get; }

		public ResolvedTypeInfo SystemVoid { get; }

		public ResolvedTypeInfo SystemVoidPointer { get; }

		public ResolvedTypeInfo SystemNullable { get; }

		public ResolvedTypeInfo SystemType { get; }

		public ResolvedTypeInfo TypedReference { get; }

		public ResolvedTypeInfo Int32TypeReference { get; }

		public ResolvedTypeInfo Int16TypeReference { get; }

		public ResolvedTypeInfo UInt16TypeReference { get; }

		public ResolvedTypeInfo SByteTypeReference { get; }

		public ResolvedTypeInfo ByteTypeReference { get; }

		public ResolvedTypeInfo BoolTypeReference { get; }

		public ResolvedTypeInfo CharTypeReference { get; }

		public ResolvedTypeInfo IntPtrTypeReference { get; }

		public ResolvedTypeInfo UIntPtrTypeReference { get; }

		public ResolvedTypeInfo Int64TypeReference { get; }

		public ResolvedTypeInfo UInt32TypeReference { get; }

		public ResolvedTypeInfo UInt64TypeReference { get; }

		public ResolvedTypeInfo SingleTypeReference { get; }

		public ResolvedTypeInfo DoubleTypeReference { get; }

		public ResolvedTypeInfo ObjectTypeReference { get; }

		public ResolvedTypeInfo StringTypeReference { get; }

		public ResolvedTypeInfo RuntimeTypeHandleTypeReference { get; }

		public ResolvedTypeInfo RuntimeMethodHandleTypeReference { get; }

		public ResolvedTypeInfo RuntimeFieldHandleTypeReference { get; }

		public ResolvedTypeInfo RuntimeArgumentHandleTypeReference { get; }

		public ResolvedTypeProvider(TypeProviderComponent typeProvider)
		{
			SystemObject = MakeResolvedType(typeProvider.SystemObject);
			SystemString = MakeResolvedType(typeProvider.SystemString);
			SystemArray = MakeResolvedType(typeProvider.SystemArray);
			SystemException = MakeResolvedType(typeProvider.SystemException);
			SystemDelegate = MakeResolvedType(typeProvider.SystemDelegate);
			SystemMulticastDelegate = MakeResolvedType(typeProvider.SystemMulticastDelegate);
			SystemByte = MakeResolvedType(typeProvider.SystemByte);
			SystemUInt16 = MakeResolvedType(typeProvider.SystemUInt16);
			SystemIntPtr = MakeResolvedType(typeProvider.SystemIntPtr);
			SystemUIntPtr = MakeResolvedType(typeProvider.SystemUIntPtr);
			SystemVoid = MakeResolvedType(typeProvider.SystemVoid);
			SystemVoidPointer = MakeResolvedType(typeProvider.SystemVoidPointer);
			SystemNullable = MakeResolvedType(typeProvider.SystemNullable);
			SystemType = MakeResolvedType(typeProvider.SystemType);
			TypedReference = MakeResolvedType(typeProvider.TypedReference);
			Int32TypeReference = MakeResolvedType(typeProvider.Int32TypeReference);
			Int16TypeReference = MakeResolvedType(typeProvider.Int16TypeReference);
			UInt16TypeReference = MakeResolvedType(typeProvider.UInt16TypeReference);
			SByteTypeReference = MakeResolvedType(typeProvider.SByteTypeReference);
			ByteTypeReference = MakeResolvedType(typeProvider.ByteTypeReference);
			BoolTypeReference = MakeResolvedType(typeProvider.BoolTypeReference);
			CharTypeReference = MakeResolvedType(typeProvider.CharTypeReference);
			IntPtrTypeReference = MakeResolvedType(typeProvider.IntPtrTypeReference);
			UIntPtrTypeReference = MakeResolvedType(typeProvider.UIntPtrTypeReference);
			Int64TypeReference = MakeResolvedType(typeProvider.Int64TypeReference);
			UInt32TypeReference = MakeResolvedType(typeProvider.UInt32TypeReference);
			UInt64TypeReference = MakeResolvedType(typeProvider.UInt64TypeReference);
			SingleTypeReference = MakeResolvedType(typeProvider.SingleTypeReference);
			DoubleTypeReference = MakeResolvedType(typeProvider.DoubleTypeReference);
			ObjectTypeReference = MakeResolvedType(typeProvider.ObjectTypeReference);
			StringTypeReference = MakeResolvedType(typeProvider.StringTypeReference);
			RuntimeTypeHandleTypeReference = MakeResolvedType(typeProvider.RuntimeTypeHandleTypeReference);
			RuntimeMethodHandleTypeReference = MakeResolvedType(typeProvider.RuntimeMethodHandleTypeReference);
			RuntimeFieldHandleTypeReference = MakeResolvedType(typeProvider.RuntimeFieldHandleTypeReference);
			RuntimeArgumentHandleTypeReference = MakeResolvedType(typeProvider.RuntimeArgumentHandleTypeReference);
		}

		private ResolvedTypeInfo MakeResolvedType(TypeReference typeReference)
		{
			if (typeReference != null)
			{
				return ResolvedTypeInfo.FromResolvedType(typeReference);
			}
			return null;
		}
	}

	private TypeContext _typeContext;

	public IResolvedTypeProviderService Resolved { get; private set; }

	public ReadOnlyCollection<TypeDefinition> GraftedArrayInterfaceTypes => _typeContext.GraftedArrayInterfaceTypes;

	public ReadOnlyCollection<MethodDefinition> GraftedArrayInterfaceMethods => _typeContext.GraftedArrayInterfaceMethods;

	public AssemblyDefinition Corlib => _typeContext.SystemAssembly;

	public TypeDefinition SystemObject => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Object);

	public TypeDefinition SystemString => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.String);

	public TypeDefinition SystemArray => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Array);

	public TypeDefinition SystemException => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Exception);

	public TypeDefinition SystemDelegate => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Delegate);

	public TypeDefinition SystemMulticastDelegate => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.MulticastDelegate);

	public TypeDefinition SystemByte => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Byte);

	public TypeDefinition SystemUInt16 => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt16);

	public TypeDefinition SystemIntPtr => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IntPtr);

	public TypeDefinition SystemUIntPtr => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UIntPtr);

	public TypeDefinition SystemVoid => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Void);

	public PointerType SystemVoidPointer { get; private set; }

	public TypeDefinition SystemNullable => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Nullable);

	public TypeDefinition SystemType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Type);

	public TypeReference Int32TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int32);

	public TypeReference Int16TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int16);

	public TypeReference UInt16TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt16);

	public TypeReference SByteTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.SByte);

	public TypeReference ByteTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Byte);

	public TypeReference BoolTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Boolean);

	public TypeReference CharTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Char);

	public TypeReference IntPtrTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IntPtr);

	public TypeReference UIntPtrTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UIntPtr);

	public TypeReference Int64TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int64);

	public TypeReference UInt32TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt32);

	public TypeReference UInt64TypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt64);

	public TypeReference SingleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Single);

	public TypeReference DoubleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Double);

	public TypeReference ObjectTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Object);

	public TypeReference StringTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.String);

	public TypeReference RuntimeTypeHandleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.RuntimeTypeHandle);

	public TypeReference RuntimeMethodHandleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.RuntimeMethodHandle);

	public TypeReference RuntimeFieldHandleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.RuntimeFieldHandle);

	public TypeReference RuntimeArgumentHandleTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.RuntimeArgumentHandle);

	public TypeDefinition TypedReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.TypedReference);

	public TypeReference IActivationFactoryTypeReference => _typeContext.GetIl2CppCustomType(Il2CppCustomType.IActivationFactory);

	public TypeReference IPropertyValueType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IPropertyValue);

	public TypeReference IReferenceType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IReference);

	public TypeReference IReferenceArrayType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IReferenceArray);

	public TypeReference IIterableTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IIterable);

	public TypeReference IBindableIterableTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IBindableIterable);

	public TypeReference IBindableIteratorTypeReference => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IBindableIterator);

	public TypeReference Il2CppComObjectTypeReference => _typeContext.GetIl2CppCustomType(Il2CppCustomType.Il2CppComObject);

	public TypeReference Il2CppComDelegateTypeReference => _typeContext.GetIl2CppCustomType(Il2CppCustomType.Il2CppComDelegate);

	public TypeReference Il2CppFullySharedGenericTypeReference => _typeContext.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric);

	public TypeDefinition IStringableType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IStringable);

	public TypeDefinition ConstantSplittableMapType => _typeContext.GetSystemType(Unity.IL2CPP.DataModel.SystemType.ConstantSplittableMap_2);

	public void Initialize(AssemblyConversionContext context, TypeContext typeContext)
	{
		if (context.Results.Initialize.AllAssembliesOrderedByDependency.FirstOrDefault() == null)
		{
			throw new InvalidOperationException("One or more assemblies must be setup for conversion");
		}
		_typeContext = typeContext;
		if (IPropertyValueType == null != (IReferenceType == null) || IPropertyValueType == null != (IReferenceArrayType == null))
		{
			throw new InvalidProgramException("Windows.Foundation.IPropertyValue, Windows.Foundation.IReference`1<T> and Windows.Foundation.IReferenceArray`1<T> are a package deal. Either all or none must be available. Are stripper link.xml files configured correctly?");
		}
		SystemVoidPointer = context.StatefulServices.TypeFactory.CreatePointerType(SystemVoid);
		Resolved = new ResolvedTypeProvider(this);
	}

	public TypeDefinition GetSystemType(SystemType systemType)
	{
		return _typeContext.GetSystemType(systemType);
	}

	protected override TypeProviderComponent ThisAsFull()
	{
		return this;
	}

	protected override ITypeProviderService ThisAsRead()
	{
		return this;
	}
}
