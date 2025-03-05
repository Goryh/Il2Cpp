using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime;

internal class DelegateCCWWriter : CCWWriterBase
{
	private readonly TypeReference[] _implementedInterfaces = new TypeReference[0];

	private readonly string[] _queryableInterfaces;

	protected override IEnumerable<TypeReference> AllImplementedInterfaces => _implementedInterfaces;

	protected override IEnumerable<string> AllQueryableInterfaceNames => _queryableInterfaces;

	public DelegateCCWWriter(SourceWritingContext context, TypeReference type)
		: base(context, type)
	{
		_queryableInterfaces = new string[1] { context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_type) };
	}

	public override void Write(IGeneratedMethodCodeWriter writer)
	{
		TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(_type);
		MethodReference invokeMethod = typeResolver.Resolve(_type.Resolve().Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
		string parameterList = MethodSignatureWriter.FormatComMethodParameterList(_context, invokeMethod, invokeMethod, typeResolver, MarshalType.WindowsRuntime, includeTypeNames: true, preserveSig: false);
		string comCallableWrapperInterfaceName = _context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_type);
		string comCallableWrapperClassName = _context.Global.Services.Naming.ForComCallableWrapperClass(_type);
		writer.AddInclude("vm/CachedCCWBase.h");
		AddIncludes(writer);
		writer.WriteLine();
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("COM Callable Wrapper class definition for " + _type.FullName);
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"struct {comCallableWrapperClassName} IL2CPP_FINAL : il2cpp::vm::CachedCCWBase<{comCallableWrapperClassName}>, {comCallableWrapperInterfaceName}");
		using (new BlockWriter(writer, semicolon: true))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"inline {comCallableWrapperClassName}({_context.Global.Services.TypeProvider.ObjectTypeReference.CppNameForVariable} obj) : ");
			writer.Indent();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp::vm::CachedCCWBase<{comCallableWrapperClassName}>(obj)");
			writer.Dedent();
			using (new BlockWriter(writer))
			{
			}
			WriteCommonInterfaceMethods(writer);
			writer.WriteLine();
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine("COM Callable invoker for " + _type.FullName);
			}
			string invokeSignature = "virtual il2cpp_hresult_t STDCALL Invoke(" + parameterList + ") override";
			writer.WriteMethodWithMetadataInitialization(invokeSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				new ComCallableWrapperMethodBodyWriter(writer.Context, invokeMethod, invokeMethod, MarshalType.WindowsRuntime).WriteMethodBody(bodyWriter, metadataAccess);
			}, invokeMethod.CppName + "_WindowsRuntimeManagedInvoker", invokeMethod);
		}
	}
}
