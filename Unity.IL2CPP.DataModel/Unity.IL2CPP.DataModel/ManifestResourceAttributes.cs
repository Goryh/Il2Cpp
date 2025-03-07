using System;

namespace Unity.IL2CPP.DataModel;

[Flags]
public enum ManifestResourceAttributes : uint
{
	VisibilityMask = 7u,
	Public = 1u,
	Private = 2u
}
