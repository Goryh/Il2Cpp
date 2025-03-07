using System;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class GenericParameter : TypeReference, ICustomAttributeProvider, IMetadataTokenProvider
{
	public MethodReference DeclaringMethod => Owner as MethodReference;

	public override bool IsGraftedArrayInterfaceType => false;

	public override string FullName => Name;

	public IGenericParameterProvider Owner { get; }

	public int Position { get; }

	public GenericParameterAttributes Attributes { get; }

	public GenericParameterType Type => Owner.GenericParameterType;

	public bool HasConstraints => Constraints.Count > 0;

	public ReadOnlyCollection<GenericParameterConstraint> Constraints { get; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public bool HasReferenceTypeConstraint => Attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);

	public bool HasNotNullableValueTypeConstraint => Attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);

	public override bool ContainsGenericParameter => true;

	public override bool IsValueType => false;

	public override bool IsGenericParameter => true;

	public override bool ContainsDefaultInterfaceMethod => false;

	public override bool IsAbstract => false;

	public override bool IsByRefLike => false;

	public override FieldDuplication FieldDuplication => FieldDuplication.None;

	public override string CppNameForVariable
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override MetadataType MetadataType
	{
		get
		{
			if (Type != GenericParameterType.Method)
			{
				return MetadataType.Var;
			}
			return MetadataType.MVar;
		}
	}

	protected override bool IsFullNameBuilt => true;

	internal GenericParameter(Mono.Cecil.GenericParameter genericParameter, IGenericParameterProvider owner, ReadOnlyCollection<GenericParameterConstraint> constraints, ReadOnlyCollection<CustomAttribute> customAttrs, TypeContext context)
		: base(context, (owner as MemberReference)?.Module, owner as TypeReference, genericParameter.Namespace, MetadataToken.FromCecil(genericParameter))
	{
		InitializeName(genericParameter.Name);
		InitializeFullName(genericParameter.FullName);
		Owner = owner;
		Constraints = constraints;
		CustomAttributes = customAttrs;
		Position = genericParameter.Position;
		Attributes = (GenericParameterAttributes)genericParameter.Attributes;
	}

	internal GenericParameter(GenericParameter genericParameter, IGenericParameterProvider owner, TypeContext context)
		: base(context, (owner as MemberReference)?.Module, owner as TypeReference, genericParameter.Namespace, genericParameter.MetadataToken)
	{
		InitializeName(genericParameter.Name);
		InitializeFullName(genericParameter.FullName);
		Owner = owner;
		Constraints = ReadOnlyCollectionCache<GenericParameterConstraint>.Empty;
		CustomAttributes = ReadOnlyCollectionCache<CustomAttribute>.Empty;
		Attributes = GenericParameterAttributes.NonVariant;
		Position = genericParameter.Position;
		GenericParameterProviderPopulater.InitializeEmpty(this);
	}

	public override TypeReference GetBaseType(ITypeFactory typeFactory)
	{
		return null;
	}

	public override ReadOnlyCollection<MethodReference> GetMethods(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<MethodReference>.Empty;
	}

	public override ReadOnlyCollection<InflatedFieldType> GetInflatedFieldTypes(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<InflatedFieldType>.Empty;
	}

	public override ReadOnlyCollection<TypeReference> GetInterfaceTypes(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<TypeReference>.Empty;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		throw new InvalidOperationException();
	}
}
