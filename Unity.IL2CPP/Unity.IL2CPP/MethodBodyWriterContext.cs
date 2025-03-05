using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal class MethodBodyWriterContext
{
	private readonly MethodWriteContext _context;

	public ReadOnlyContext Context => _context.AsReadonly();

	public MethodReference MethodReference => _context.MethodReference;

	public MethodDefinition MethodDefinition => _context.MethodDefinition;

	public IGeneratedCodeWriter Writer { get; }

	public IRuntimeMetadataAccess RuntimeMetadataAccess { get; }

	public Stack<StackInfo> ValueStack { get; }

	public MethodBodyWriterContext(MethodWriteContext context, IGeneratedCodeWriter writer, IRuntimeMetadataAccess runtimeMetadataAccess, Stack<StackInfo> valueStack)
	{
		_context = context;
		Writer = writer;
		RuntimeMetadataAccess = runtimeMetadataAccess;
		ValueStack = valueStack;
	}
}
