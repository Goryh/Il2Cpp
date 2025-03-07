namespace Unity.IL2CPP.DataModel;

public abstract class TypeSpecification : TypeReference
{
	public TypeReference ElementType { get; }

	public override bool IsGraftedArrayInterfaceType => ElementType.IsGraftedArrayInterfaceType;

	public override bool ContainsGenericParameter => ElementType.ContainsGenericParameter;

	public override bool IsEnum => ElementType.IsEnum;

	public override bool IsInterface => ElementType.IsInterface;

	public override bool IsDelegate => ElementType.IsDelegate;

	public override bool IsComInterface => ElementType.IsComInterface;

	public override bool IsAttribute => ElementType.IsAttribute;

	public override bool HasStaticConstructor => ElementType.HasStaticConstructor;

	public override bool IsAbstract => ElementType.IsAbstract;

	public override bool IsByRefLike => ElementType.IsByRefLike;

	public override FieldDuplication FieldDuplication => ElementType.FieldDuplication;

	public override bool ContainsDefaultInterfaceMethod => ElementType.ContainsDefaultInterfaceMethod;

	public override bool ContainsFullySharedGenericTypes => ElementType?.ContainsFullySharedGenericTypes ?? false;

	internal TypeSpecification(TypeReference elementType, TypeContext context)
		: this(null, elementType, context)
	{
	}

	internal TypeSpecification(ModuleDefinition moduleDefinition, TypeContext context)
		: base(context, moduleDefinition, null, "", MetadataToken.TypeSpecZero)
	{
	}

	internal TypeSpecification(TypeReference declaringType, TypeReference elementType, TypeContext context)
		: base(context, elementType.Module, declaringType, elementType.Namespace, MetadataToken.TypeSpecZero)
	{
		ElementType = elementType;
	}

	public override AssemblyNameReference GetAssemblyNameReference()
	{
		return ElementType.GetAssemblyNameReference();
	}

	public override TypeReference GetElementType()
	{
		return ElementType?.GetElementType();
	}

	public override TypeDefinition Resolve()
	{
		return ElementType?.Resolve();
	}

	public override TypeReference GetUnderlyingEnumType()
	{
		return ElementType?.GetUnderlyingEnumType();
	}
}
