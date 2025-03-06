using System;

namespace Unity.IL2CPP.Api;

[Flags]
public enum GenericsOptions
{
	None = 0,
	EnableSharing = 1,
	EnableEnumTypeSharing = 2,
	EnablePrimitiveValueTypeGenericSharing = 4,
	EnableFullSharing = 8,
	EnableLegacyGenericSharing = 0x10,
	EnableFullSharingForStaticConstructors = 0x20
}
