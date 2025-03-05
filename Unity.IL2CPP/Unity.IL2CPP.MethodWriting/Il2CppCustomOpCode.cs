namespace Unity.IL2CPP.MethodWriting;

public enum Il2CppCustomOpCode
{
	Nop,
	EnumHasFlag,
	EnumGetHashCode,
	CopyStackValue,
	BoxBranchOptimization,
	NullableBoxBranchOptimization,
	VariableSizedBoxBranchOptimization,
	NullableIsNull,
	NullableIsNotNull,
	VariableSizedWouldBoxToNull,
	VariableSizedWouldBoxToNotNull,
	BranchRight,
	BranchLeft,
	PushTrue,
	PushFalse,
	LdsfldZero,
	BitConverterIsLittleEndian,
	Pop1
}
