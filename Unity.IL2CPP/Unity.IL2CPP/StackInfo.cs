using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public struct StackInfo
{
	public readonly string Expression;

	public readonly ResolvedTypeInfo Type;

	public readonly ResolvedTypeInfo BoxedType;

	public readonly ResolvedMethodInfo MethodExpressionIsPointingTo;

	public StackInfo(string expression, ResolvedTypeInfo type, ResolvedTypeInfo boxedType = null, ResolvedMethodInfo methodExpressionIsPointingTo = null)
	{
		Expression = expression;
		Type = type;
		BoxedType = boxedType;
		MethodExpressionIsPointingTo = methodExpressionIsPointingTo;
	}

	public StackInfo(StackInfo local)
	{
		Expression = local.Expression;
		Type = local.Type;
		BoxedType = local.BoxedType;
		MethodExpressionIsPointingTo = local.MethodExpressionIsPointingTo;
	}

	public override string ToString()
	{
		return Expression;
	}

	public string GetIdentifierExpression(ReadOnlyContext context)
	{
		return context.Global.Services.Naming.ForVariable(Type) + " " + Expression;
	}
}
