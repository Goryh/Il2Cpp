using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class EnumeratorToBindableIteratorAdapterWriter : CCWWriterBase
{
	private sealed class GetCurrentMethodBodyWriter : ComCallableWrapperMethodBodyWriter
	{
		private readonly string _hresult;

		public GetCurrentMethodBodyWriter(ReadOnlyContext context, MethodReference managedMethod, MethodReference interfaceMethod)
			: base(context, managedMethod, interfaceMethod, MarshalType.WindowsRuntime)
		{
			_hresult = context.Global.Services.Naming.ForInteropHResultVariable();
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			base.WriteMethodPrologue(writer, metadataAccess);
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteStatement($"il2cpp_hresult_t {_hresult} = {writer.WriteCall("Initialize")}");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IL2CPP_HR_FAILED({_hresult}))");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				IGeneratedMethodCodeWriter generatedMethodCodeWriter2 = generatedMethodCodeWriter;
				CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 1, generatedMethodCodeWriter);
				left.AppendLiteral("*");
				left.AppendFormatted(writer.Context.Global.Services.Naming.ForComInterfaceReturnParameterName());
				generatedMethodCodeWriter2.WriteAssignStatement(ref left, "NULL");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteStatement($"return {_hresult}");
			}
		}

		protected override void WriteExceptionReturnStatement(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteAssignStatement(_hresult, "ex.ex->hresult");
			writer.WriteStatement($"return ({_hresult} == IL2CPP_COR_E_INVALIDOPERATION) ? IL2CPP_E_BOUNDS : {_hresult}");
		}
	}

	private const string InitializedFieldName = "_initialized";

	private const string HasCurrentFieldName = "_hasCurrent";

	private const string InitializeMethodName = "Initialize";

	private const string MoveNextMethodName = "MoveNext";

	private readonly TypeDefinition _iEnumeratorType;

	private readonly string _returnValue;

	private readonly string _hresult;

	private string _typeName;

	private TypeReference IBindableIteratorType => _type;

	protected override bool ImplementsAnyIInspectableInterfaces => true;

	protected override IEnumerable<TypeReference> AllImplementedInterfaces => new TypeReference[1] { _type };

	public static void WriteDefinitions(SourceWritingContext context, IGeneratedMethodCodeWriter writer)
	{
		new EnumeratorToBindableIteratorAdapterWriter(context).Write(writer);
	}

	private EnumeratorToBindableIteratorAdapterWriter(SourceWritingContext context)
		: base(context, context.Global.Services.TypeProvider.IBindableIteratorTypeReference)
	{
		_iEnumeratorType = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator);
		_returnValue = context.Global.Services.Naming.ForComInterfaceReturnParameterName();
		_hresult = context.Global.Services.Naming.ForInteropHResultVariable();
		if (_type == null)
		{
			throw new InvalidOperationException("It is not valid to use EnumeratorToBindableIteratorAdapterWriter without IBindableIterator type available.");
		}
		if (_iEnumeratorType == null)
		{
			throw new InvalidOperationException("It is not valid to use EnumeratorToBindableIteratorAdapterWriter without IEnumerator type available.");
		}
		_typeName = context.Global.Services.Naming.ForWindowsRuntimeAdapterClass(IBindableIteratorType);
	}

	public override void Write(IGeneratedMethodCodeWriter writer)
	{
		WriteTypeDefinition(writer);
		WriteMethodDefinitions(writer);
	}

	private void WriteTypeDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.AddInclude("vm/NonCachedCCWBase.h");
		AddIncludes(writer);
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(IBindableIteratorType.FullName + " adapter");
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"struct {_typeName} IL2CPP_FINAL : il2cpp::vm::NonCachedCCWBase<{_typeName}>, {IBindableIteratorType.CppName}");
		using (new BlockWriter(writer, semicolon: true))
		{
			WriteConstructor(writer);
			WriteCommonInterfaceMethods(writer);
			TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(IBindableIteratorType);
			foreach (MethodDefinition method in IBindableIteratorType.Resolve().Methods)
			{
				string signature = ComInterfaceWriter.GetSignature(_context, method, method, typeResolver);
				writer.WriteLine(signature + ";");
			}
			using (new DedentWriter(writer))
			{
				writer.WriteLine("private:");
			}
			writer.WriteLine("bool _initialized;");
			writer.WriteLine("bool _hasCurrent;");
			writer.WriteLine("il2cpp_hresult_t Initialize();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {"MoveNext"}(bool* {_returnValue});");
		}
	}

	private void WriteConstructor(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine($"inline {_typeName}(RuntimeObject* obj) : il2cpp::vm::NonCachedCCWBase<{_typeName}>(obj), {"_initialized"}(false), {"_hasCurrent"}(false) {{}}");
	}

	private void WriteMethodDefinitions(IGeneratedMethodCodeWriter writer)
	{
		TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(IBindableIteratorType);
		foreach (MethodDefinition method in IBindableIteratorType.Resolve().Methods)
		{
			string signature = ComInterfaceWriter.GetSignature(_context, method, method, typeResolver, _typeName);
			writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				switch (method.Name)
				{
				case "get_Current":
					WriteMethodGetCurrent(method, bodyWriter, metadataAccess);
					break;
				case "get_HasCurrent":
					WriteMethodGetHasCurrentCurrent(method, bodyWriter, metadataAccess);
					break;
				case "MoveNext":
					WriteMethodMoveNext(method, bodyWriter, metadataAccess);
					break;
				default:
					throw new NotSupportedException($"Interface '{IBindableIteratorType.FullName}' contains unsupported method '{method.Name}'.");
				}
			}, _typeName + "_" + method.CppName, method);
		}
		WriteMethodInitialize(writer);
		WriteMethodMoveNext(writer);
	}

	private void WriteMethodGetCurrent(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		MethodDefinition getCurrentMethod = _iEnumeratorType.Methods.First((MethodDefinition m) => m.Name == "get_Current");
		new GetCurrentMethodBodyWriter(writer.Context, getCurrentMethod, method).WriteMethodBody(writer, metadataAccess);
	}

	private void WriteMethodGetHasCurrentCurrent(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		WriteInitializationCode(writer);
		CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 1, writer);
		left.AppendLiteral("*");
		left.AppendFormatted(_returnValue);
		writer.WriteAssignStatement(ref left, "_hasCurrent");
		writer.WriteStatement("return IL2CPP_S_OK");
	}

	private void WriteMethodMoveNext(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		WriteInitializationCode(writer);
		writer.WriteStatement(Emit.Assign(_hresult, Emit.Call(writer.Context, "MoveNext", "&_hasCurrent")));
		CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 1, writer);
		left.AppendLiteral("*");
		left.AppendFormatted(_returnValue);
		writer.WriteAssignStatement(ref left, "_hasCurrent");
		writer.WriteStatement("return " + _hresult);
	}

	private void WriteInitializationCode(ICodeWriter writer)
	{
		writer.WriteStatement(Emit.Assign("il2cpp_hresult_t " + _hresult, Emit.Call(writer.Context, "Initialize")));
		ICodeWriter codeWriter = writer;
		codeWriter.WriteLine($"if (IL2CPP_HR_FAILED({_hresult}))");
		using (new BlockWriter(writer))
		{
			codeWriter = writer;
			ICodeWriter codeWriter2 = codeWriter;
			CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 1, codeWriter);
			left.AppendLiteral("*");
			left.AppendFormatted(_returnValue);
			codeWriter2.WriteAssignStatement(ref left, "false");
			writer.WriteStatement("return " + _hresult);
		}
	}

	private void WriteMethodInitialize(IGeneratedMethodCodeWriter writer)
	{
		string signature = $"il2cpp_hresult_t {_typeName}::{"Initialize"}()";
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadatAccess)
		{
			bodyWriter.WriteLine("if (_initialized)");
			using (new BlockWriter(bodyWriter))
			{
				bodyWriter.WriteStatement("return IL2CPP_S_OK");
			}
			bodyWriter.WriteStatement(Emit.Assign("const il2cpp_hresult_t " + _hresult, Emit.Call(bodyWriter.Context, "MoveNext", "&_hasCurrent")));
			bodyWriter.WriteStatement(Emit.Assign("_initialized", "IL2CPP_HR_SUCCEEDED(" + _hresult + ")"));
			bodyWriter.WriteStatement("return " + _hresult);
		}, _typeName + "_Initialize", null);
	}

	private void WriteMethodMoveNext(IGeneratedMethodCodeWriter writer)
	{
		string signature = $"il2cpp_hresult_t {_typeName}::{"MoveNext"}(bool* {_returnValue})";
		MethodDefinition moveNextManagedMethod = _iEnumeratorType.Methods.First((MethodDefinition m) => m.Name == "MoveNext");
		MethodDefinition moveNextNativeMethod = IBindableIteratorType.Resolve().Methods.First((MethodDefinition m) => m.Name == "MoveNext");
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			new ComCallableWrapperMethodBodyWriter(writer.Context, moveNextManagedMethod, moveNextNativeMethod, MarshalType.WindowsRuntime).WriteMethodBody(bodyWriter, metadataAccess);
		}, _typeName + "_MoveNext", moveNextManagedMethod);
	}
}
