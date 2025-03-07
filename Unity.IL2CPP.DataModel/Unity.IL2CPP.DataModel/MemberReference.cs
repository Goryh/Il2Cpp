using System.Diagnostics;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{DebuggerDisplayName}")]
public abstract class MemberReference : IMetadataTokenProvider
{
	private string _name;

	private string _fullName;

	public TypeReference DeclaringType { get; }

	public virtual string Name
	{
		get
		{
			if (_name == null)
			{
				ThrowDataNotInitialized("Name");
			}
			return _name;
		}
	}

	public virtual string FullName
	{
		get
		{
			if (_fullName == null)
			{
				ThrowDataNotInitialized("FullName");
			}
			return _fullName;
		}
	}

	public string DebuggerDisplayName
	{
		get
		{
			if (!IsFullNameBuilt)
			{
				return Name;
			}
			return FullName;
		}
	}

	public virtual ModuleDefinition Module => DeclaringType?.Module;

	public virtual MetadataToken MetadataToken { get; }

	public virtual bool ContainsGenericParameter
	{
		get
		{
			if (DeclaringType != null)
			{
				return DeclaringType.ContainsGenericParameter;
			}
			return false;
		}
	}

	public virtual bool IsWindowsRuntimeProjection => false;

	public virtual bool IsDefinition => false;

	protected abstract bool IsFullNameBuilt { get; }

	protected MemberReference(TypeReference declaringType, MetadataToken metadataToken)
	{
		DeclaringType = declaringType;
		MetadataToken = metadataToken;
	}

	public override string ToString()
	{
		return FullName;
	}

	internal void InitializeFullName(string value)
	{
		if (_fullName != null)
		{
			ThrowAlreadyInitializedDataException("_fullName");
		}
		_fullName = value;
	}

	internal void InitializeName(string value)
	{
		if (_name != null)
		{
			ThrowAlreadyInitializedDataException("_name");
		}
		_name = value;
	}

	protected void ThrowDataNotInitialized(string publicPropertyOrFieldName)
	{
		throw new UninitializedDataAccessException($"[{GetType()}] {this}.{publicPropertyOrFieldName} has not been initialized yet.");
	}

	protected void ThrowAlreadyInitializedDataException(string fieldName)
	{
		throw new AlreadyInitializedDataAccessException($"[{GetType()}] {this}.{fieldName} has already been initialized and cannot be set again");
	}
}
