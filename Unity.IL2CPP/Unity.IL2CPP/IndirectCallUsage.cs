using System;

namespace Unity.IL2CPP;

[Flags]
public enum IndirectCallUsage
{
	Virtual = 1,
	Instance = 2,
	Static = 4
}
