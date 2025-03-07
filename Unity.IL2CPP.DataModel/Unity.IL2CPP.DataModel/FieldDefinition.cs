using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public class FieldDefinition : FieldReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, IMarshalInfoProvider, IConstantProvider, IMarshalInfoUpdater
{
	internal readonly Mono.Cecil.FieldDefinition Definition;

	private bool _marshalInfoUpdated;

	private TypeReference _fieldType;

	private bool _propertiesInitialized;

	private bool _isThreadStatic;

	private int _fieldIndex;

	private bool _isNormalStatic;

	public override TypeReference FieldType => _fieldType;

	internal bool IsDataModelGenerated { get; }

	public override FieldAttributes Attributes { get; }

	public override ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public override bool IsDefinition => true;

	public object Constant { get; }

	public int Offset { get; }

	public int RVA { get; }

	public bool HasConstant { get; }

	public bool HasLayoutInfo => Offset >= 0;

	public MarshalInfo MarshalInfo { get; private set; }

	public bool HasMarshalInfo => MarshalInfo != null;

	public byte[] InitialValue { get; }

	public override bool IsWindowsRuntimeProjection { get; }

	public new TypeDefinition DeclaringType => (TypeDefinition)base.DeclaringType;

	public override FieldDefinition FieldDef => this;

	public override bool IsThreadStatic
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsThreadStatic");
			}
			return _isThreadStatic;
		}
	}

	public override bool IsNormalStatic
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsNormalStatic");
			}
			return _isNormalStatic;
		}
	}

	public override int FieldIndex
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("FieldIndex");
			}
			return _fieldIndex;
		}
	}

	bool IMarshalInfoUpdater.MarshalInfoHasBeenUpdated => _marshalInfoUpdated;

	internal FieldDefinition(TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttributes, MarshalInfo marshalInfo, Mono.Cecil.FieldDefinition definition)
		: this(definition.Name, declaringType, (FieldAttributes)definition.Attributes, customAttributes, marshalInfo, definition.Offset, definition.RVA, definition.InitialValue, MetadataToken.FromCecil(definition), definition.IsWindowsRuntimeProjection, definition.HasConstant, definition.HasConstant ? definition.Constant : null, isDataModelGenerated: false)
	{
		Definition = definition;
	}

	internal FieldDefinition(string name, TypeDefinition declaringType, FieldAttributes attributes, ReadOnlyCollection<CustomAttribute> customAttributes, MarshalInfo marshalInfo, int offset, int rva, byte[] initialValue, MetadataToken metadataToken, bool isWindowsRuntimeProjection, bool hasConstant, object constant, bool isDataModelGenerated = true)
		: base(declaringType, metadataToken)
	{
		InitializeName(name);
		CustomAttributes = customAttributes;
		MarshalInfo = marshalInfo;
		Attributes = attributes;
		Offset = offset;
		RVA = rva;
		InitialValue = initialValue;
		IsWindowsRuntimeProjection = isWindowsRuntimeProjection;
		HasConstant = hasConstant;
		Constant = constant;
		IsDataModelGenerated = isDataModelGenerated;
	}

	public override TypeReference ResolvedFieldType(ITypeFactory typeFactory)
	{
		return _fieldType;
	}

	internal void InitializeFieldDefinitionProperties(bool isThreadStatic, bool isNormalStatic, int fieldIndex)
	{
		_propertiesInitialized = true;
		_isThreadStatic = isThreadStatic;
		_isNormalStatic = isNormalStatic;
		_fieldIndex = fieldIndex;
	}

	void IMarshalInfoUpdater.UpdateMarshalInfo(MarshalInfo marshalInfo)
	{
		MarshalInfo = marshalInfo;
		_marshalInfoUpdated = true;
	}

	internal void InitializeFieldType(TypeReference fieldType)
	{
		if (_fieldType != null)
		{
			ThrowAlreadyInitializedDataException("FieldType");
		}
		_fieldType = fieldType;
	}
}
