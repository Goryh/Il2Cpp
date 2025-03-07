namespace Unity.IL2CPP.DataModel.Extensions;

public static class MetadataTypeExtensions
{
	public static bool IsPrimitive(this MetadataType self)
	{
		if (self - 2 <= MetadataType.UInt64 || self - 24 <= MetadataType.Void)
		{
			return true;
		}
		return false;
	}
}
