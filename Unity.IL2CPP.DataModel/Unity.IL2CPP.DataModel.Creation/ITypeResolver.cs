namespace Unity.IL2CPP.DataModel.Creation;

public interface ITypeResolver
{
	MethodReference Resolve(MethodReference method);

	TypeReference Resolve(TypeReference typeReference);

	FieldReference Resolve(FieldReference field);

	TypeReference ResolveReturnType(MethodReference method);

	TypeReference ResolveParameterType(MethodReference method, ParameterDefinition parameter);

	TypeReference ResolveVariableType(MethodReference method, VariableDefinition variable);

	TypeReference ResolveFieldType(FieldReference field);

	TypeReference Resolve(TypeReference typeReference, bool resolveGenericParameters);

	MethodReference Resolve(MethodReference method, bool resolveGenericParameters);
}
