using System;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class FieldInst : FieldReference
{
	private TypeReference _resolvedFieldType;

	public override int FieldIndex => FieldDef.FieldIndex;

	public override FieldDefinition FieldDef { get; }

	public override bool IsThreadStatic => FieldDef.IsThreadStatic;

	public override bool IsNormalStatic => FieldDef.IsNormalStatic;

	public override FieldAttributes Attributes => FieldDef.Attributes;

	public override ReadOnlyCollection<CustomAttribute> CustomAttributes => FieldDef.CustomAttributes;

	public override TypeReference FieldType => FieldDef.FieldType;

	public override MetadataToken MetadataToken => FieldDef.MetadataToken;

	internal FieldInst(FieldDefinition fieldDef, TypeReference declaringType)
		: base(declaringType, MetadataToken.MemberRefZero)
	{
		if (fieldDef == null)
		{
			throw new ArgumentNullException("fieldDef");
		}
		FieldDef = fieldDef;
		InitializeName(fieldDef.Name);
	}

	public override TypeReference ResolvedFieldType(ITypeFactory typeFactory)
	{
		if (_resolvedFieldType == null)
		{
			Interlocked.CompareExchange(ref _resolvedFieldType, GenericParameterResolver.ResolveFieldTypeIfNeeded(typeFactory, this), null);
		}
		return _resolvedFieldType;
	}
}
