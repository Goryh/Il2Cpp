using System;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public record LoadParameters
{
	public readonly bool EnableSerial;

	public readonly int JobCount;

	public readonly bool ApplyWindowsRuntimeProjections;

	public readonly bool AggregateWindowsMetadata;

	public readonly int MaximumRecursiveGenericDepth;

	public readonly bool DisableGenericSharing;

	public readonly bool DisableFullGenericSharing;

	public readonly bool FullGenericSharingOnly;

	public readonly bool FullGenericSharingStaticConstructors;

	public readonly bool CanShareEnumTypes;

	public readonly bool SupportWindowsRuntime;

	public readonly bool FreezeDefinitionsOnLoad;

	public readonly bool EnableDebugger;

	public LoadParameters(bool disableGenericSharing, bool disableFullGenericSharing, bool fullGenericSharingOnly, bool fullGenericSharingStaticConstructors, bool canShareEnumTypes, bool freezeDefinitionsOnLoad, bool enableDebugger, bool enableSerial = false, int jobCount = -1, bool applyWindowsRuntimeProjections = true, bool aggregateWindowsMetadata = true, int maximumRecursiveGenericDepth = 7, bool supportWindowsRuntime = false)
	{
		EnableSerial = enableSerial || jobCount == 1;
		JobCount = ((jobCount > 0) ? jobCount : Environment.ProcessorCount);
		ApplyWindowsRuntimeProjections = applyWindowsRuntimeProjections;
		AggregateWindowsMetadata = aggregateWindowsMetadata;
		MaximumRecursiveGenericDepth = maximumRecursiveGenericDepth;
		SupportWindowsRuntime = supportWindowsRuntime;
		DisableGenericSharing = disableGenericSharing;
		DisableFullGenericSharing = disableFullGenericSharing;
		FullGenericSharingOnly = fullGenericSharingOnly;
		FullGenericSharingStaticConstructors = fullGenericSharingStaticConstructors;
		CanShareEnumTypes = canShareEnumTypes;
		FreezeDefinitionsOnLoad = freezeDefinitionsOnLoad;
		EnableDebugger = enableDebugger;
	}
}
