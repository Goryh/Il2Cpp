using System;

namespace Unity.IL2CPP.Api;

[Flags]
public enum TestingOptions
{
	None = 1,
	EnableErrorMessageTest = 2,
	EnableGoogleBenchmark = 4,
	AssertFullCacheHits = 8
}
