using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public class MethodReturnType : ICustomAttributeProvider, IMetadataTokenProvider, IMarshalInfoProvider, IMarshalInfoUpdater
{
	private TypeReference _returnType;

	private bool _marshalInfoUpdated;

	public virtual TypeReference ReturnType
	{
		get
		{
			if (_returnType == null)
			{
				throw new UninitializedDataAccessException("ReturnType");
			}
			return _returnType;
		}
	}

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public MetadataToken MetadataToken { get; }

	public MarshalInfo MarshalInfo { get; private set; }

	public bool HasMarshalInfo => MarshalInfo != null;

	bool IMarshalInfoUpdater.MarshalInfoHasBeenUpdated => _marshalInfoUpdated;

	internal MethodReturnType(ReadOnlyCollection<CustomAttribute> customAttributes, MarshalInfo marshalInfo, MetadataToken metadataToken)
	{
		CustomAttributes = customAttributes;
		MarshalInfo = marshalInfo;
		MetadataToken = metadataToken;
	}

	internal MethodReturnType(TypeReference typeReference)
	{
		_returnType = typeReference;
		CustomAttributes = ReadOnlyCollectionCache<CustomAttribute>.Empty;
		MarshalInfo = null;
		MetadataToken = MetadataToken.ParamZero;
	}

	internal void InitializeReturnType(TypeReference returnType)
	{
		_returnType = returnType;
	}

	void IMarshalInfoUpdater.UpdateMarshalInfo(MarshalInfo marshalInfo)
	{
		MarshalInfo = marshalInfo;
		_marshalInfoUpdated = true;
	}
}
