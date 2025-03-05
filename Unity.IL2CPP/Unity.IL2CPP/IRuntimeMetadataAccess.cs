using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public interface IRuntimeMetadataAccess
{
	public enum TypeInfoForReason
	{
		Any,
		Size,
		Field,
		IsValueType,
		Box,
		WouldBoxToNull
	}

	string StaticData(TypeReference type);

	string TypeInfoFor(TypeReference type);

	string TypeInfoFor(TypeReference type, TypeInfoForReason reason);

	string UnresolvedTypeInfoFor(TypeReference type);

	string ArrayInfo(ArrayType arrayType);

	string Newobj(MethodReference ctor);

	string Il2CppTypeFor(TypeReference type);

	string Method(MethodReference method);

	string MethodInfo(MethodReference method);

	string UnresolvedMethodInfo(MethodReference method);

	string HiddenMethodInfo(MethodReference method);

	string FieldInfo(FieldReference field, TypeReference declaringType);

	string FieldRvaData(FieldReference field, TypeReference declaringType);

	string StringLiteral(string literal);

	string StringLiteral(string literal, AssemblyDefinition assemblyDefinition);

	bool NeedsBoxingForValueTypeThis(MethodReference method);

	void AddInitializerStatement(string statement);

	bool MustDoVirtualCallFor(ResolvedTypeInfo type, MethodReference methodToCall);

	void StartInitMetadataInline();

	void EndInitMetadataInline();

	IMethodMetadataAccess MethodMetadataFor(MethodReference unresolvedMethodToCall);

	IMethodMetadataAccess ConstrainedMethodMetadataFor(ResolvedTypeInfo constrainedType, ResolvedMethodInfo methodToCall);

	GenericContextUsage GetMethodRgctxDataUsage()
	{
		return GenericContextUsage.None;
	}
}
