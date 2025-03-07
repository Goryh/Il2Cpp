using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{ToString()} ({ParameterType})")]
public class ParameterDefinition : ICustomAttributeProvider, IMetadataTokenProvider, IMarshalInfoProvider, IConstantProvider, IMarshalInfoUpdater
{
	public const int ThisParameterIndex = -1;

	internal readonly Mono.Cecil.ParameterDefinition Definition;

	private TypeReference _parameterType;

	private bool _marshalInfoUpdated;

	private string _cppName;

	public TypeReference ParameterType
	{
		get
		{
			if (_parameterType == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _parameterType;
		}
	}

	public string Name { get; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public ParameterAttributes Attributes { get; }

	public object Constant { get; }

	public int Index { get; }

	public bool IsIn => Attributes.HasFlag(ParameterAttributes.In);

	public bool IsLcid => Attributes.HasFlag(ParameterAttributes.Lcid);

	public bool IsOptional => Attributes.HasFlag(ParameterAttributes.Optional);

	public bool IsOut => Attributes.HasFlag(ParameterAttributes.Out);

	public bool IsReturnValue => Attributes.HasFlag(ParameterAttributes.Retval);

	public bool HasCustomAttributes => CustomAttributes.Count > 0;

	public bool HasConstant { get; }

	public bool HasDefault => Attributes.HasFlag(ParameterAttributes.HasDefault);

	public bool HasFieldMarshal => Attributes.HasFlag(ParameterAttributes.HasFieldMarshal);

	public MarshalInfo MarshalInfo { get; private set; }

	public bool HasMarshalInfo => MarshalInfo != null;

	public MetadataToken MetadataToken { get; }

	public string CppName
	{
		get
		{
			if (_cppName == null)
			{
				Interlocked.CompareExchange(ref _cppName, CppNamePopulator.GetParameterDefinitionCppName(this), null);
			}
			return _cppName;
		}
	}

	bool IMarshalInfoUpdater.MarshalInfoHasBeenUpdated => _marshalInfoUpdated;

	internal ParameterDefinition(Mono.Cecil.ParameterDefinition definition, ReadOnlyCollection<CustomAttribute> customAttributes, MarshalInfo marshalInfo)
		: this(definition.Name, (ParameterAttributes)definition.Attributes, definition.Index, customAttributes, marshalInfo, definition.HasConstant, definition.HasConstant ? definition.Constant : null, MetadataToken.FromCecil(definition))
	{
		Definition = definition;
	}

	internal ParameterDefinition(ParameterDefinition definition, MetadataToken metadataToken)
		: this(definition.Name, definition.Attributes, definition.Index, definition.CustomAttributes, definition.MarshalInfo, definition.HasConstant, definition.Constant, metadataToken)
	{
	}

	internal ParameterDefinition(string name, ParameterAttributes attributes, int index, ReadOnlyCollection<CustomAttribute> customAttributes, MarshalInfo marshalInfo, bool hasConstant, object constant, MetadataToken metadataToken)
	{
		Definition = null;
		CustomAttributes = customAttributes;
		MarshalInfo = marshalInfo;
		Name = name;
		Attributes = attributes;
		Index = index;
		MetadataToken = metadataToken;
		HasConstant = hasConstant;
		Constant = constant;
	}

	internal static ParameterDefinition MakeThisParameter(TypeReference thisType, MetadataToken metadataToken)
	{
		return new ParameterDefinition(thisType, -1, metadataToken);
	}

	private ParameterDefinition(TypeReference parameterType, int index, MetadataToken metadataToken)
	{
		Index = index;
		Definition = null;
		CustomAttributes = ReadOnlyCollectionCache<CustomAttribute>.Empty;
		MarshalInfo = null;
		Name = null;
		MetadataToken = metadataToken;
		HasConstant = false;
		Constant = null;
		InitializeParameterType(parameterType);
	}

	internal void InitializeParameterType(TypeReference parameterType)
	{
		_parameterType = parameterType;
	}

	void IMarshalInfoUpdater.UpdateMarshalInfo(MarshalInfo marshalInfo)
	{
		MarshalInfo = marshalInfo;
		_marshalInfoUpdated = true;
	}

	public override string ToString()
	{
		return Name;
	}
}
