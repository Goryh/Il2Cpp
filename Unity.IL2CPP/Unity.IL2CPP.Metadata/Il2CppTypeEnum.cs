namespace Unity.IL2CPP.Metadata;

public enum Il2CppTypeEnum : byte
{
	IL2CPP_TYPE_END = 0,
	IL2CPP_TYPE_VOID = 1,
	IL2CPP_TYPE_BOOLEAN = 2,
	IL2CPP_TYPE_CHAR = 3,
	IL2CPP_TYPE_I1 = 4,
	IL2CPP_TYPE_U1 = 5,
	IL2CPP_TYPE_I2 = 6,
	IL2CPP_TYPE_U2 = 7,
	IL2CPP_TYPE_I4 = 8,
	IL2CPP_TYPE_U4 = 9,
	IL2CPP_TYPE_I8 = 10,
	IL2CPP_TYPE_U8 = 11,
	IL2CPP_TYPE_R4 = 12,
	IL2CPP_TYPE_R8 = 13,
	IL2CPP_TYPE_STRING = 14,
	IL2CPP_TYPE_PTR = 15,
	IL2CPP_TYPE_BYREF = 16,
	IL2CPP_TYPE_VALUETYPE = 17,
	IL2CPP_TYPE_CLASS = 18,
	IL2CPP_TYPE_VAR = 19,
	IL2CPP_TYPE_ARRAY = 20,
	IL2CPP_TYPE_GENERICINST = 21,
	IL2CPP_TYPE_TYPEDBYREF = 22,
	IL2CPP_TYPE_I = 24,
	IL2CPP_TYPE_U = 25,
	IL2CPP_TYPE_FNPTR = 27,
	IL2CPP_TYPE_OBJECT = 28,
	IL2CPP_TYPE_SZARRAY = 29,
	IL2CPP_TYPE_MVAR = 30,
	IL2CPP_TYPE_CMOD_REQD = 31,
	IL2CPP_TYPE_CMOD_OPT = 32,
	IL2CPP_TYPE_INTERNAL = 33,
	IL2CPP_TYPE_MODIFIER = 64,
	IL2CPP_TYPE_SENTINEL = 65,
	IL2CPP_TYPE_PINNED = 69,
	IL2CPP_TYPE_ENUM = 85,
	IL2CPP_TYPE_IL2CPP_TYPE_INDEX = byte.MaxValue
}
