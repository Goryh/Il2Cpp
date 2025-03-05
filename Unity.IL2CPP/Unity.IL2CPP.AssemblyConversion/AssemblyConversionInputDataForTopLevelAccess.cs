using Unity.IL2CPP.Api;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionInputDataForTopLevelAccess
{
	public readonly ConversionMode ConversionMode;

	public AssemblyConversionInputDataForTopLevelAccess(ConversionMode conversionMode)
	{
		ConversionMode = conversionMode;
	}
}
