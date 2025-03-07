using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class FullGenericSharedTypeReference : TypeDefinition
{
	public override bool IsValueType
	{
		get
		{
			throw new NotSupportedException("The fully shared type could be any type");
		}
	}

	public override MetadataType MetadataType
	{
		get
		{
			throw new NotSupportedException("The fully shard reference could be any type");
		}
	}

	internal FullGenericSharedTypeReference(TypeContext context, ModuleDefinition module, string @namespace, string name, TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttrs, MetadataToken token, TypeAttributes typeAttributes, MetadataType metadataType, bool isDataModelGenerated = true)
		: base(context, module, @namespace, name, declaringType, customAttrs, token, typeAttributes, metadataType, isDataModelGenerated)
	{
	}

	internal FullGenericSharedTypeReference(TypeContext context, ModuleDefinition module, string @namespace, string name, TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttrs, MetadataToken token, int classSize, short packingSize, TypeAttributes typeAttributes, MetadataType metadataType, bool isWindowsRuntimeProjection, bool isDataModelGenerated = true)
		: base(context, module, @namespace, name, declaringType, customAttrs, token, classSize, packingSize, typeAttributes, metadataType, isWindowsRuntimeProjection, isDataModelGenerated)
	{
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return RuntimeStorageKind.VariableSizedAny;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Variable;
	}

	public override RuntimeFieldLayoutKind GetStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Variable;
	}

	public override RuntimeFieldLayoutKind GetThreadStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Variable;
	}
}
