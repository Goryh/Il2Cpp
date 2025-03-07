namespace Unity.IL2CPP.DataModel.Comparers;

internal static class ParameterDefinitionComparer
{
	public static bool AreEqual(ParameterDefinition x, ParameterDefinition y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		return x.ParameterType == y.ParameterType;
	}

	public static int GetHashCodeFor(ParameterDefinition obj)
	{
		return ((obj.ParameterType.GetHashCode() * 17) ^ obj.Name?.GetHashCode()).GetValueOrDefault();
	}
}
