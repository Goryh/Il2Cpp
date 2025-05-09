using System;

namespace Unity.IL2CPP.Api;

[Flags]
public enum CodeGenerationOptions
{
	None = 1,
	EnableNullChecks = 2,
	EnableStacktrace = 4,
	EnableArrayBoundsCheck = 8,
	EnableDivideByZeroCheck = 0x10,
	EnableLazyStaticConstructors = 0x20,
	EnableComments = 0x40,
	EnableSerial = 0x80,
	EnablePerAssemblyMetadata = 0x100,
	EnableInlining = 0x200,
	VirtualCallsViaInvokers = 0x400,
	SharedGenericCallsViaInvokers = 0x800,
	DelegateCallsViaInvokers = 0x1000
}
