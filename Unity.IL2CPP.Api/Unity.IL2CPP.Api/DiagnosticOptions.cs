using System;

namespace Unity.IL2CPP.Api;

[Flags]
public enum DiagnosticOptions : long
{
	None = 1L,
	EnableStats = 2L,
	NeverAttachDialog = 4L,
	EmitAttachDialog = 8L,
	EnableTinyDiagnostics = 0x10L,
	DebuggerOff = 0x20L,
	EmitReversePInvokeWrapperDebuggingHelpers = 0x40L,
	EnableDiagnostics = 0x80L,
	EnableTinyStaticConstructorExplanation = 0x100L
}
