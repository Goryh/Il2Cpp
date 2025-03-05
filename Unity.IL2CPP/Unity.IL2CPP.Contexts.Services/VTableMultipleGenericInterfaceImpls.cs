using System;

namespace Unity.IL2CPP.Contexts.Services;

[Flags]
public enum VTableMultipleGenericInterfaceImpls
{
	None = 0,
	HasDirectImplementation = 1,
	HasDefaultInterfaceImplementation = 2
}
