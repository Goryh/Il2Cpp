using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal abstract class ComMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
{
	protected readonly MethodReference _actualMethod;

	protected readonly MarshalType _marshalType;

	protected readonly TypeReference _interfaceType;

	public ComMethodBodyWriter(ReadOnlyContext context, MethodReference actualMethod, MethodReference interfaceMethod)
		: base(context, interfaceMethod, actualMethod, GetMarshalType(interfaceMethod), useUnicodeCharset: true)
	{
		_actualMethod = actualMethod;
		_marshalType = GetMarshalType(interfaceMethod);
		_interfaceType = interfaceMethod.DeclaringType;
	}

	private static MarshalType GetMarshalType(MethodReference interfaceMethod)
	{
		if (!interfaceMethod.DeclaringType.IsComInterface)
		{
			return MarshalType.WindowsRuntime;
		}
		return MarshalType.COM;
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string interfaceTypeName = _interfaceType.CppName;
		string localVariableName = writer.Context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType);
		if (_actualMethod.HasThis)
		{
			string thisVariable = "static_cast<Il2CppComObject*>(__this)";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{interfaceTypeName}* {localVariableName} = il2cpp_codegen_com_query_interface<{interfaceTypeName}>({thisVariable});");
		}
		else
		{
			string staticFieldsType = writer.Context.Global.Services.Naming.ForStaticFieldsStruct(writer.Context, _actualMethod.DeclaringType);
			string staticFieldsExpression = $"(({staticFieldsType}*)il2cpp_codegen_static_fields_for({metadataAccess.TypeInfoFor(_actualMethod.DeclaringType)}))";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{interfaceTypeName}* {localVariableName} = {staticFieldsExpression}->{writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(_interfaceType)}();");
		}
		writer.AddIncludeForTypeDefinition(writer.Context, _interfaceType);
		writer.WriteLine();
	}

	protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReturnType methodReturnType = GetMethodReturnType();
		if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			MarshalInfoWriterFor(writer.Context, methodReturnType).WriteNativeVariableDeclarationOfType(writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
		}
		writer.WriteStatement(GetMethodCallExpression(writer.Context, localVariableNames));
		if (!InteropMethod.Resolve().IsPreserveSig)
		{
			writer.WriteLine();
			writer.WriteStatement(Emit.Call(writer.Context, "il2cpp_codegen_com_raise_exception_if_failed", writer.Context.Global.Services.Naming.ForInteropHResultVariable(), (_marshalType == MarshalType.COM) ? "true" : "false"));
		}
	}

	private string GetMethodCallExpression(ReadOnlyContext context, string[] localVariableNames)
	{
		bool preserveSig = InteropMethod.Resolve().IsPreserveSig;
		MethodReturnType methodReturnType = GetMethodReturnType();
		string parametersExpression = GetFunctionCallParametersExpression(context, localVariableNames, !preserveSig);
		StringBuilder builder = new StringBuilder();
		if (!preserveSig)
		{
			builder.Append("const il2cpp_hresult_t ");
			builder.Append(context.Global.Services.Naming.ForInteropHResultVariable());
			builder.Append(" = ");
		}
		else if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			builder.Append(context.Global.Services.Naming.ForInteropReturnValue());
			builder.Append(" = ");
		}
		builder.Append(context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType));
		builder.Append("->");
		builder.Append(GetMethodNameInGeneratedCode(context));
		builder.Append("(");
		builder.Append(parametersExpression);
		builder.Append(")");
		return builder.ToString();
	}
}
