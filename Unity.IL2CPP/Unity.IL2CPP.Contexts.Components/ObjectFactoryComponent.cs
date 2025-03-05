using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Diagnostics;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Contexts.Components;

public class ObjectFactoryComponent : ServiceComponentBase<IObjectFactory, ObjectFactoryComponent>, IObjectFactory, IDumpableState
{
	private class NotAvailable : IObjectFactory
	{
		public IChunkedMemoryStreamBufferProvider ChunkedMemoryStreamProvider
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage, WritingMethodFor writingMethodFor)
		{
			throw new NotSupportedException();
		}

		public DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
		{
			throw new NotSupportedException();
		}

		public Returnable<StringBuilder> CheckoutStringBuilder()
		{
			throw new NotSupportedException();
		}
	}

	private class BufferProvider : IChunkedMemoryStreamBufferProvider
	{
		public readonly List<byte[]> BufferCache = new List<byte[]>();

		public int BufferSize => 1048576;

		public byte[] Get()
		{
			if (BufferCache.Count == 0)
			{
				return new byte[BufferSize];
			}
			byte[] result = BufferCache[BufferCache.Count - 1];
			BufferCache.RemoveAt(BufferCache.Count - 1);
			return result;
		}

		public void Return(byte[] buffer)
		{
			BufferCache.Add(buffer);
		}
	}

	private const int DefaultStringBuilderSize = 8000;

	private readonly Stack<StringBuilder> _stringBuilderCache = new Stack<StringBuilder>();

	private readonly Action<StringBuilder> _returnStringBuilder;

	private readonly IChunkedMemoryStreamBufferProvider _bufferProvider;

	public int ForTesting_StringBuilderCacheCount => _stringBuilderCache.Count;

	public IChunkedMemoryStreamBufferProvider ChunkedMemoryStreamProvider => _bufferProvider;

	public ObjectFactoryComponent()
	{
		_returnStringBuilder = ReturnStringBuilder;
		_bufferProvider = new BufferProvider();
	}

	public IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage, WritingMethodFor writingMethodFor)
	{
		DefaultRuntimeMetadataAccess defaultMetadataAccess = new DefaultRuntimeMetadataAccess(context, method, methodMetadataUsage, methodUsage);
		if (method != null && method.IsSharedMethod(context))
		{
			return new SharedRuntimeMetadataAccess(context, method, defaultMetadataAccess, writingMethodFor);
		}
		return defaultMetadataAccess;
	}

	public DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
	{
		TypeDefinition resolvedType = type.Resolve();
		useUnicodeCharSet |= resolvedType != null && resolvedType.Attributes.HasFlag(TypeAttributes.UnicodeClass);
		bool isStringBuilder = MarshalingUtils.IsStringBuilder(type);
		if (type.MetadataType == MetadataType.String || isStringBuilder)
		{
			return new StringMarshalInfoWriter(type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling);
		}
		if (type.IsDelegate && (!(type is TypeSpecification) || type is GenericInstanceType))
		{
			if (marshalType == MarshalType.WindowsRuntime)
			{
				return new WindowsRuntimeDelegateMarshalInfoWriter(context, type);
			}
			return new DelegateMarshalInfoWriter(type, forFieldMarshaling);
		}
		NativeType? nativeType = marshalInfo?.NativeType;
		if (type == context.Global.Services.TypeProvider.GetSystemType(SystemType.HandleRef))
		{
			return new HandleRefMarshalInfoWriter(type, forByReferenceType);
		}
		if (type is ByReferenceType { ElementType: var elementType } byReferenceType)
		{
			if (MarshalingUtils.IsBlittable(context, elementType, nativeType, marshalType, useUnicodeCharSet) && elementType.IsValueType)
			{
				return new BlittableByReferenceMarshalInfoWriter(byReferenceType, marshalType, marshalInfo);
			}
			return new ByReferenceMarshalInfoWriter(byReferenceType, marshalType, marshalInfo, forNativeToManagedWrapper);
		}
		if (type.IsPrimitive || type.IsPointer || type.IsEnum || type.MetadataType == MetadataType.Void)
		{
			return new PrimitiveMarshalInfoWriter(context, type, marshalInfo, marshalType, useUnicodeCharSet);
		}
		if (type is ArrayType { ElementType: var elementType2 } arrayType)
		{
			NativeType? elementNativeType = ((marshalInfo is ArrayMarshalInfo arrayMarshalInfo) ? new NativeType?(arrayMarshalInfo.ElementType) : ((NativeType?)null));
			if (marshalType != MarshalType.WindowsRuntime)
			{
				if (!elementType2.IsStringBuilder && (elementType2.MetadataType == MetadataType.Object || elementType2.MetadataType == MetadataType.Array || (elementType2.MetadataType == MetadataType.Class && !MarshalingUtils.HasMarshalableLayout(elementType2)) || arrayType.Rank != 1))
				{
					return new UnmarshalableMarshalInfoWriter(context, type);
				}
				if (marshalInfo != null && marshalInfo.NativeType == NativeType.SafeArray)
				{
					return new ComSafeArrayMarshalInfoWriter(context, arrayType, marshalInfo);
				}
				if (marshalInfo != null && marshalInfo.NativeType == NativeType.FixedArray)
				{
					return new FixedArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo);
				}
			}
			if (!forByReferenceType && !forFieldMarshaling && MarshalingUtils.IsBlittable(context, elementType2, elementNativeType, marshalType, useUnicodeCharSet))
			{
				return new PinnedArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo, useUnicodeCharSet);
			}
			if (marshalInfo == null && forFieldMarshaling && ComSafeArrayMarshalInfoWriter.IsMarshalableAsSafeArray(context, elementType2.MetadataType))
			{
				return new ComSafeArrayMarshalInfoWriter(context, arrayType);
			}
			return new LPArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo);
		}
		TypeDefinition safeHandleTypeReference = context.Global.Services.TypeProvider.GetSystemType(SystemType.SafeHandle);
		if (type.DerivesFrom(context, safeHandleTypeReference, checkInterfaces: false))
		{
			return new SafeHandleMarshalInfoWriter(type, safeHandleTypeReference);
		}
		if (type.IsComOrWindowsRuntimeInterface(context))
		{
			return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
		}
		if (type.IsSystemObject)
		{
			if (marshalInfo != null)
			{
				switch (marshalInfo.NativeType)
				{
				case NativeType.IUnknown:
				case NativeType.IntF:
				case (NativeType)46:
					return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
				case NativeType.Struct:
					return new ComVariantMarshalInfoWriter(type);
				}
			}
			if (marshalType == MarshalType.WindowsRuntime)
			{
				return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
			}
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition.IsAbstract && typeDefinition.IsSealed)
		{
			return new UnmarshalableMarshalInfoWriter(context, type);
		}
		if (typeDefinition == context.Global.Services.TypeProvider.SystemException)
		{
			if (marshalType == MarshalType.WindowsRuntime)
			{
				return new ExceptionMarshalInfoWriter(context, typeDefinition);
			}
		}
		else
		{
			if (typeDefinition.MetadataType == MetadataType.Class && !(type is TypeSpecification) && typeDefinition.IsExposedToWindowsRuntime() && !typeDefinition.IsAttribute)
			{
				return new ComObjectMarshalInfoWriter(context, typeDefinition, marshalType, marshalInfo, forNativeToManagedWrapper);
			}
			if (marshalType == MarshalType.WindowsRuntime)
			{
				GenericInstanceType genericInstance = type as GenericInstanceType;
				if (genericInstance != null && context.Global.Services.TypeProvider.IReferenceType != null && type.IsNullableGenericInstance && genericInstance.GenericArguments[0].CanBoxToWindowsRuntime(context))
				{
					return new WindowsRuntimeNullableMarshalInfoWriter(context, type);
				}
				TypeReference windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
				if (windowsRuntimeType != type)
				{
					if (windowsRuntimeType.IsComOrWindowsRuntimeInterface(context))
					{
						if (typeDefinition.IsInterface)
						{
							return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
						}
						if (typeDefinition.IsValueType && genericInstance != null && typeDefinition == context.Global.Services.TypeProvider.GetSystemType(SystemType.KeyValuePair))
						{
							return new KeyValuePairMarshalInfoWriter(context, genericInstance);
						}
					}
					if (type.Namespace == "System" && type.Name == "Uri")
					{
						return new UriMarshalInfoWriter(context, typeDefinition);
					}
					if (type.Namespace == "System" && type.Name == "DateTimeOffset")
					{
						return new DateTimeOffsetMarshalInfoWriter(context, typeDefinition);
					}
				}
				if (typeDefinition == context.Global.Services.TypeProvider.SystemType)
				{
					return new WindowsRuntimeTypeMarshalInfoWriter(typeDefinition);
				}
			}
		}
		if (MarshalingUtils.IsBlittable(context, type, nativeType, marshalType, useUnicodeCharSet))
		{
			if (type.IsUserDefinedStruct())
			{
				if (!forByReferenceType && !forFieldMarshaling && marshalInfo != null && marshalInfo.NativeType == NativeType.LPStruct)
				{
					return new LPStructMarshalInfoWriter(context, type, marshalType);
				}
				return new BlittableStructMarshalInfoWriter(context, type, marshalType);
			}
			if (type.MetadataType == MetadataType.Class)
			{
				return new BlittableClassMarshalInfoWriter(context, typeDefinition, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper);
			}
		}
		if (MarshalDataCollector.HasCustomMarshalingMethods(context, type, nativeType, marshalType, useUnicodeCharSet, forFieldMarshaling))
		{
			if (typesForRecursiveFields == null)
			{
				typesForRecursiveFields = new HashSet<TypeReference>();
			}
			FieldDefinition unsupportedField = typeDefinition.GetTypeHierarchy().SelectMany((TypeDefinition t) => MarshalingUtils.NonStaticFieldsOf(t)).FirstOrDefault(delegate(FieldDefinition field)
			{
				typesForRecursiveFields.Add(type);
				try
				{
					if (typesForRecursiveFields.Contains(field.FieldType))
					{
						return true;
					}
					if (field.FieldType == context.Global.Services.TypeProvider.GetSystemType(SystemType.HandleRef))
					{
						return true;
					}
					if (field.FieldType.IsArray)
					{
						if (!MarshalingUtils.IsMarshalableArrayField(field))
						{
							return true;
						}
						TypeReference elementType3 = ((ArrayType)field.FieldType).ElementType;
						bool flag = MarshalingUtils.HasMarshalableLayout(elementType3) && MarshalingUtils.IsMarshalable(context, elementType3, marshalType, field.MarshalInfo, useUnicodeCharSet, forFieldMarshaling: true, typesForRecursiveFields);
						if (!elementType3.IsPrimitive && !elementType3.IsPointer && !elementType3.IsEnum && !flag)
						{
							return true;
						}
					}
					if (field.FieldType.IsDelegate)
					{
						MethodReference methodReference = field.FieldType.FindMethodByName(context, "Invoke");
						if (typesForRecursiveFields.Contains(methodReference.ReturnType.GetElementType()))
						{
							return true;
						}
						foreach (ParameterDefinition current in methodReference.Parameters)
						{
							if (typesForRecursiveFields.Contains(current.ParameterType.GetElementType()))
							{
								return true;
							}
						}
					}
					if (MarshalDataCollector.FieldIsArrayOfType(field, type))
					{
						return true;
					}
					return !MarshalingUtils.IsMarshalable(context, field.FieldType, marshalType, field.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(typeDefinition), forFieldMarshaling: true, typesForRecursiveFields);
				}
				finally
				{
					typesForRecursiveFields.Remove(type);
				}
			});
			if (unsupportedField != null)
			{
				return new TypeDefinitionWithUnsupportedFieldMarshalInfoWriter(context, typeDefinition, marshalType, unsupportedField);
			}
			if (type.IsGenericInstance)
			{
				return new TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter(context, typeDefinition, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper);
			}
			return new TypeDefinitionMarshalInfoWriter(context, typeDefinition, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper);
		}
		return new UnmarshalableMarshalInfoWriter(context, type);
	}

	public Returnable<StringBuilder> CheckoutStringBuilder()
	{
		if (_stringBuilderCache.Count > 0)
		{
			StringBuilder stringBuilder = _stringBuilderCache.Pop();
			stringBuilder.Clear();
			return new Returnable<StringBuilder>(stringBuilder, _returnStringBuilder);
		}
		return new Returnable<StringBuilder>(new StringBuilder(8000), _returnStringBuilder);
	}

	protected override IObjectFactory GetNotAvailableRead()
	{
		return new NotAvailable();
	}

	protected override ObjectFactoryComponent CreateEmptyInstance()
	{
		return new ObjectFactoryComponent();
	}

	protected override ObjectFactoryComponent CreatePooledInstance()
	{
		return new ObjectFactoryComponent();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
	}

	protected override void SyncPooledInstanceWithParent(ObjectFactoryComponent parent)
	{
	}

	protected override ObjectFactoryComponent ThisAsFull()
	{
		return this;
	}

	protected override IObjectFactory ThisAsRead()
	{
		return this;
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out object writer, out IObjectFactory reader, out ObjectFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out object writer, out IObjectFactory reader, out ObjectFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out object writer, out IObjectFactory reader, out ObjectFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out object writer, out IObjectFactory reader, out ObjectFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	private void ReturnStringBuilder(StringBuilder stringBuilder)
	{
		_stringBuilderCache.Push(stringBuilder);
	}

	void IDumpableState.DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendValue(builder, "_stringBuilderCache.Count", _stringBuilderCache.Count);
		CollectorStateDumper.AppendCollection(builder, "_stringBuilderCache.Capacity", _stringBuilderCache.Select((StringBuilder b) => b.Capacity));
	}
}
