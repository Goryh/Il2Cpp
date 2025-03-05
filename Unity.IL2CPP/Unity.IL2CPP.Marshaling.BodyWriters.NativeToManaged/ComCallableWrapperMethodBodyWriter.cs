using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

public class ComCallableWrapperMethodBodyWriter : NativeToManagedInteropMethodBodyWriter
{
	protected virtual string ManagedObjectExpression => Emit.Call(_context, "GetManagedObjectInline");

	public ComCallableWrapperMethodBodyWriter(ReadOnlyContext context, MethodReference managedMethod, MethodReference interfaceMethod, MarshalType marshalType)
		: base(context, managedMethod, interfaceMethod, marshalType, useUnicodeCharset: true)
	{
	}

	protected sealed override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReturnType methodReturnType = GetMethodReturnType();
		bool preserveSig = InteropMethod.Resolve().IsPreserveSig;
		if (methodReturnType.ReturnType.MetadataType != MetadataType.Void && IsReturnValueMarshaled)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_typeResolver.Resolve(methodReturnType.ReturnType).CppNameForVariable} {writer.Context.Global.Services.Naming.ForInteropReturnValue()};");
		}
		writer.WriteLine("try");
		using (new BlockWriter(writer))
		{
			WriteInteropCallStatementWithinTryBlock(writer, localVariableNames, metadataAccess);
		}
		writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
		using (new BlockWriter(writer))
		{
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void && !preserveSig)
			{
				string returnValueName = writer.Context.Global.Services.Naming.ForComInterfaceReturnParameterName();
				writer.WriteStatement(Emit.Memset(writer.Context, returnValueName, 0, "sizeof(*" + returnValueName + ")"));
			}
			TypeResolver emptyTypeResolver = writer.Context.Global.Services.TypeFactory.EmptyResolver();
			writer.AddIncludeForTypeDefinition(writer.Context, writer.Context.Global.Services.TypeProvider.SystemString);
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.TypeProvider.SystemString.CppNameForVariable} exceptionStr = {"NULL"};");
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				string[] args = new string[1] { "ex.ex" };
				MethodDefinition toStringMethod = writer.Context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.Name == "ToString");
				MethodBodyWriter.WriteMethodCallExpression("exceptionStr", writer, _managedMethod, toStringMethod, emptyTypeResolver, MethodCallType.Virtual, metadataAccess.MethodMetadataFor(toStringMethod).OverrideHiddenMethodInfo(null), writer.Context.Global.Services.VTable, args, useArrayBoundsCheck: false);
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper&)");
			using (new BlockWriter(writer))
			{
				FieldDefinition stringEmptyField = writer.Context.Global.Services.TypeProvider.SystemString.Fields.Single((FieldDefinition f) => f.Name == "Empty");
				string staticFieldsAccess = MethodBodyWriter.TypeStaticsExpressionFor(writer.Context, stringEmptyField, emptyTypeResolver, metadataAccess);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"exceptionStr = {staticFieldsAccess}{stringEmptyField.CppName};");
			}
			writer.WriteLine("il2cpp_codegen_store_exception_info(ex.ex, exceptionStr);");
			if (!preserveSig)
			{
				WriteExceptionReturnStatement(writer);
			}
			else
			{
				WritePreserveSigExceptionReturnStatement(writer, methodReturnType);
			}
		}
	}

	protected virtual void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		string thisLocalVariableName = "__thisValue";
		if (_managedMethod.DeclaringType.IsValueType)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_managedMethod.DeclaringType.CppName}* {thisLocalVariableName} = ({_managedMethod.DeclaringType.CppName}*)UnBox({ManagedObjectExpression}, {metadataAccess.TypeInfoFor(_managedMethod.DeclaringType)});");
		}
		else
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_managedMethod.DeclaringType.CppNameForVariable} {thisLocalVariableName} = ({_managedMethod.DeclaringType.CppNameForVariable}){ManagedObjectExpression};");
		}
		if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
		{
			WriteMethodCallStatement(metadataAccess, thisLocalVariableName, localVariableNames, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
		}
		else
		{
			WriteMethodCallStatement(metadataAccess, thisLocalVariableName, localVariableNames, writer);
		}
	}

	protected virtual void WriteExceptionReturnStatement(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteStatement("return ex.ex->hresult");
	}

	private void WritePreserveSigExceptionReturnStatement(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType)
	{
		if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			string errorReturnValue = "0";
			switch (methodReturnType.ReturnType.MetadataType)
			{
			case MetadataType.Int32:
			case MetadataType.UInt32:
				errorReturnValue = "ex.ex->hresult";
				break;
			case MetadataType.Single:
				errorReturnValue = "std::numeric_limits<float>::quiet_NaN()";
				break;
			case MetadataType.Double:
				errorReturnValue = "std::numeric_limits<double>::quiet_NaN()";
				break;
			case MetadataType.ValueType:
				errorReturnValue = writer.Context.Global.Services.Naming.ForInteropReturnValue();
				writer.WriteStatement(writer.WriteMemset("&" + errorReturnValue, 0, "sizeof(" + errorReturnValue + ")"));
				break;
			}
			writer.WriteLine($"return static_cast<{MarshaledReturnType.DecoratedName}>({errorReturnValue});");
		}
	}

	protected override void WriteReturnStatementEpilogue(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName)
	{
		if (InteropMethod.Resolve().IsPreserveSig)
		{
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"return {(IsReturnValueMarshaled ? unmarshaledReturnValueVariableName : writer.Context.Global.Services.Naming.ForInteropReturnValue())};");
			}
			return;
		}
		if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void && IsReturnValueMarshaled)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"*{writer.Context.Global.Services.Naming.ForComInterfaceReturnParameterName()} = {unmarshaledReturnValueVariableName};");
		}
		writer.WriteLine("return IL2CPP_S_OK;");
	}

	protected void WriteRaiseManagedExceptionWithCustomHResult(IGeneratedMethodCodeWriter writer, MethodReference exceptionConstructor, int hresult, string hresultName, IRuntimeMetadataAccess metadataAccess, params string[] constructorArgs)
	{
		string exceptionVariableName = "exception";
		PropertyDefinition hresultProperty = writer.Context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
		TypeReference exceptionType = exceptionConstructor.DeclaringType;
		writer.AddIncludeForTypeDefinition(writer.Context, exceptionType);
		writer.AddIncludeForMethodDeclaration(exceptionConstructor);
		writer.AddIncludeForMethodDeclaration(hresultProperty.SetMethod);
		writer.WriteLine($"{exceptionType.CppNameForVariable} {exceptionVariableName} = {Emit.NewObj(writer.Context, exceptionType, metadataAccess)};");
		WriteMethodCallStatement(metadataAccess, exceptionVariableName, exceptionConstructor, MethodCallType.Normal, writer, constructorArgs);
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(hresultName);
		}
		WriteMethodCallStatement(metadataAccess, exceptionVariableName, hresultProperty.SetMethod, MethodCallType.Normal, writer, hresult.ToString());
		writer.WriteStatement(Emit.RaiseManagedException(exceptionVariableName));
	}
}
