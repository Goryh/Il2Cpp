namespace Unity.IL2CPP.DataModel.BuildLogic.Utils;

internal struct LazyInitBool
{
	private byte _val;

	public bool IsInitialized => _val > 0;

	public bool Value
	{
		get
		{
			if (!IsInitialized)
			{
				throw new UninitializedDataAccessException("LazyInitBool");
			}
			return (_val & 1) == 1;
		}
	}

	public void Initialize(bool value)
	{
		_val = (byte)((value ? 1u : 0u) | 2u);
	}
}
