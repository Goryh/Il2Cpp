using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedFieldInfo
{
	public readonly FieldReference FieldReference;

	public readonly ResolvedTypeInfo FieldType;

	public readonly ResolvedTypeInfo DeclaringType;

	public string Name => FieldReference.Name;

	public bool IsLiteral => FieldReference.IsLiteral;

	public bool IsThreadStatic => FieldReference.IsThreadStatic;

	public bool IsNormalStatic => FieldReference.IsNormalStatic;

	public ResolvedFieldInfo(FieldReference fieldReference, ResolvedTypeInfo fieldType, ResolvedTypeInfo declaringType)
	{
		FieldReference = fieldReference;
		FieldType = fieldType;
		DeclaringType = declaringType;
	}

	public override string ToString()
	{
		return FieldReference.ToString();
	}
}
