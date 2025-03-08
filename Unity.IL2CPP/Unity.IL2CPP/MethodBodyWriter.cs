using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Awesome.CFG;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StackAnalysis;

namespace Unity.IL2CPP;

[DebuggerDisplay("{_methodDefinition}")]
public class MethodBodyWriter
{
	private enum FunctionPointerCallType
	{
		Native,
		Managed,
		Invoker
	}

	private enum Signedness
	{
		Signed,
		Unsigned
	}

	private int _tempIndex;

	private readonly SharingType _sharingType;

	private readonly Labeler _labeler;

	private readonly IGeneratedMethodCodeWriter _writer;

	private readonly ControlFlowGraph _cfg;

	private readonly TypeResolver _typeResolver;

	private readonly ResolvedTypeFactory _resolvedTypeFactory;

	private readonly MethodReference _methodReference;

	private readonly MethodDefinition _methodDefinition;

	private readonly MethodBodyWriterDebugOptions _options;

	private readonly Unity.IL2CPP.StackAnalysis.StackAnalysis _stackAnalysis;

	private readonly IRuntimeMetadataAccess _runtimeMetadataAccess;

	private readonly ArrayBoundsCheckSupport _arrayBoundsCheckSupport;

	private readonly DivideByZeroCheckSupport _divideByZeroCheckSupport;

	private readonly IVTableBuilderService _vTableBuilder;

	private readonly Stack<StackInfo> _valueStack = new Stack<StackInfo>();

	private readonly HashSet<Labeler.LabelId> _emittedLabels = new HashSet<Labeler.LabelId>();

	private readonly HashSet<TypeReference> _classesAlreadyInitializedInBlock = new HashSet<TypeReference>();

	private readonly MethodBodyWriterContext _methodBodyWriterContext;

	private bool _thisInstructionIsVolatile;

	private ExceptionSupport _exceptionSupport;

	private NullChecksSupport _nullCheckSupport;

	private ResolvedTypeInfo _constrainedCallThisType;

	private readonly ISourceAnnotationWriter _sourceAnnotationWriter;

	private ISequencePointProvider _sequencePointProvider;

	private readonly MethodWriteContext _context;

	private bool _shouldEmitDebugInformation;

	private readonly ResolvedMethodContext _resolvedMethodContext;

	private readonly VariableSizedTypeSupport _variableSizedTypeSupport;

	private ResolvedTypeInfo Int16TypeReference => _context.Global.Services.TypeProvider.Resolved.Int16TypeReference;

	private ResolvedTypeInfo UInt16TypeReference => _context.Global.Services.TypeProvider.Resolved.UInt16TypeReference;

	private ResolvedTypeInfo Int32TypeReference => _context.Global.Services.TypeProvider.Resolved.Int32TypeReference;

	private ResolvedTypeInfo SByteTypeReference => _context.Global.Services.TypeProvider.Resolved.SByteTypeReference;

	private ResolvedTypeInfo ByteTypeReference => _context.Global.Services.TypeProvider.Resolved.ByteTypeReference;

	private ResolvedTypeInfo IntPtrTypeReference => _context.Global.Services.TypeProvider.Resolved.IntPtrTypeReference;

	private ResolvedTypeInfo UIntPtrTypeReference => _context.Global.Services.TypeProvider.Resolved.UIntPtrTypeReference;

	private ResolvedTypeInfo Int64TypeReference => _context.Global.Services.TypeProvider.Resolved.Int64TypeReference;

	private ResolvedTypeInfo UInt32TypeReference => _context.Global.Services.TypeProvider.Resolved.UInt32TypeReference;

	private ResolvedTypeInfo UInt64TypeReference => _context.Global.Services.TypeProvider.Resolved.UInt64TypeReference;

	private ResolvedTypeInfo SingleTypeReference => _context.Global.Services.TypeProvider.Resolved.SingleTypeReference;

	private ResolvedTypeInfo DoubleTypeReference => _context.Global.Services.TypeProvider.Resolved.DoubleTypeReference;

	private ResolvedTypeInfo ObjectTypeReference => _context.Global.Services.TypeProvider.Resolved.ObjectTypeReference;

	private ResolvedTypeInfo StringTypeReference => _context.Global.Services.TypeProvider.Resolved.StringTypeReference;

	private ResolvedTypeInfo SystemIntPtr => _context.Global.Services.TypeProvider.Resolved.SystemIntPtr;

	private ResolvedTypeInfo BoolTypeReference => _context.Global.Services.TypeProvider.Resolved.BoolTypeReference;

	private ResolvedTypeInfo SystemUIntPtr => _context.Global.Services.TypeProvider.Resolved.SystemUIntPtr;

	private ResolvedTypeInfo SystemVoidPointer => _context.Global.Services.TypeProvider.Resolved.SystemVoidPointer;

	private ResolvedTypeInfo RuntimeTypeHandleTypeReference => _context.Global.Services.TypeProvider.Resolved.RuntimeTypeHandleTypeReference;

	private ResolvedTypeInfo RuntimeMethodHandleTypeReference => _context.Global.Services.TypeProvider.Resolved.RuntimeMethodHandleTypeReference;

	private ResolvedTypeInfo RuntimeFieldHandleTypeReference => _context.Global.Services.TypeProvider.Resolved.RuntimeFieldHandleTypeReference;

	private TypeReference SystemExceptionTypeReference => _context.Global.Services.TypeProvider.SystemException;

	public MethodBodyWriter(MethodWriteContext context, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		_context = context;
		_methodReference = context.MethodReference;
		_methodDefinition = context.MethodDefinition;
		_methodBodyWriterContext = new MethodBodyWriterContext(context, writer, metadataAccess, _valueStack);
		_nullCheckSupport = new NullChecksSupport(writer, _methodDefinition);
		_arrayBoundsCheckSupport = new ArrayBoundsCheckSupport(_methodDefinition, context.Global.Parameters.EnableArrayBoundsCheck);
		_divideByZeroCheckSupport = new DivideByZeroCheckSupport(writer, _methodDefinition, context.Global.Parameters.EnableDivideByZeroCheck);
		_sequencePointProvider = context.Assembly.SequencePoints;
		_writer = writer;
		_typeResolver = context.TypeResolver;
		_vTableBuilder = context.Global.Services.VTable;
		_options = new MethodBodyWriterDebugOptions();
		_sourceAnnotationWriter = context.Global.Services.SourceAnnotationWriter;
		_resolvedTypeFactory = ResolvedTypeFactory.Create(context, context.TypeResolver);
		ReadOnlyCollection<InstructionBlock> basicBlocks = ControlFlowGraph.CreateBasicBlocks(_methodDefinition);
		_resolvedMethodContext = ResolvedMethodContext.Create(_resolvedTypeFactory, _methodDefinition, _methodReference, basicBlocks);
		_stackAnalysis = Unity.IL2CPP.StackAnalysis.StackAnalysis.Analyze(_context, _resolvedTypeFactory, _resolvedMethodContext);
		_cfg = ControlFlowGraph.Create(_methodDefinition, basicBlocks);
		_labeler = new Labeler(_methodDefinition);
		_runtimeMetadataAccess = metadataAccess;
		_sharingType = (_methodReference.IsSharedMethod(context) ? SharingType.Shared : SharingType.NonShared);
		if (_context.Global.Parameters.EnableDebugger)
		{
			_shouldEmitDebugInformation = DebugWriter.ShouldEmitDebugInformation(context.Global.InputData, _methodDefinition.Module.Assembly);
		}
		_variableSizedTypeSupport = new VariableSizedTypeSupport(_runtimeMetadataAccess);
	}

	public void Generate()
	{
		if (!_methodDefinition.HasBody)
		{
			return;
		}
		if (_methodDefinition.IsUnmanagedCallersOnly)
		{
			Instruction? instruction = _methodDefinition.Body.Instructions.FirstOrDefault();
			if (instruction == null || instruction.OpCode.Code != Code.Ret)
			{
				_writer.WriteLine("il2cpp::vm::ScopedThreadAttacher _vmThreadHelper;");
			}
			if (!_methodDefinition.UnmanagedCallersOnlyInfo.IsValid)
			{
				UnmanagedCallersOnlyUtils.WriteCallToRaiseInvalidCallingConvs(_writer, _runtimeMetadataAccess, _methodReference);
			}
		}
		if (GenericsUtilities.CheckForMaximumRecursion(_context, _methodReference) && _methodReference != _methodDefinition.FullySharedMethod)
		{
			if (_context.Global.Parameters.DisableFullGenericSharing)
			{
				_writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_maximum_nested_generics_exception()"));
				return;
			}
			List<string> parameterList = new List<string>(_methodDefinition.Parameters.Count + 2);
			if (!_methodDefinition.IsStatic)
			{
				parameterList.Add("__this");
			}
			foreach (ParameterDefinition parameter in _methodReference.Parameters)
			{
				parameterList.Add(parameter.CppName);
			}
			WriteCallExpressionFor(_methodReference, _methodDefinition, MethodCallType.Normal, parameterList, _runtimeMetadataAccess.MethodMetadataFor(_methodDefinition).OverrideHiddenMethodInfo("method"), emitNullCheckForInvocation: false);
			if (_valueStack.Count == 1)
			{
				_writer.WriteReturnStatement(_valueStack.Pop().Expression);
			}
			return;
		}
		WriteLocalVariables();
		if (_context.Global.Parameters.EnableDebugger)
		{
			WriteDebuggerSupport();
			if (_sequencePointProvider.TryGetSequencePointAt(_methodDefinition, -1, SequencePointKind.Normal, out var sequencePoint))
			{
				WriteCheckSequencePoint(sequencePoint);
			}
			if (_sequencePointProvider.TryGetSequencePointAt(_methodDefinition, 16777215, SequencePointKind.Normal, out sequencePoint))
			{
				WriteCheckMethodExitSequencePoint(sequencePoint);
			}
			WriteCheckPausePoint(-1);
		}
		_exceptionSupport = new ExceptionSupport(_context, _methodDefinition, _cfg.FlowTree, _writer);
		_exceptionSupport.Prepare();
		foreach (ExceptionHandler exceptionHandler in _methodDefinition.Body.ExceptionHandlers)
		{
			if (exceptionHandler.CatchType != null)
			{
				_writer.AddIncludeForTypeDefinition(_context, _typeResolver.Resolve(exceptionHandler.CatchType));
			}
		}
		ReadOnlyDictionary<InstructionBlock, ResolvedInstructionBlock> blockToResolvedBlock = _resolvedMethodContext.Blocks.ToDictionary((ResolvedInstructionBlock b) => b.Block, (ResolvedInstructionBlock b) => b).AsReadOnly();
		foreach (GlobalVariable globalVariable in _stackAnalysis.Globals)
		{
			WriteVariable(globalVariable.Type, globalVariable.VariableName);
		}
		foreach (Node node in _exceptionSupport.FlowTree.Children)
		{
			if (node.Type != NodeType.Finally && node.Type != NodeType.Fault)
			{
				GenerateCodeRecursive(node, blockToResolvedBlock);
			}
		}
		if (_methodReference.ReturnType.IsNotVoid)
		{
			Instruction lastInstruction = _methodDefinition.Body.Instructions.LastOrDefault();
			if (lastInstruction != null && lastInstruction.OpCode != OpCodes.Ret && lastInstruction.OpCode != OpCodes.Throw && lastInstruction.OpCode != OpCodes.Rethrow && !(lastInstruction.Operand is Instruction))
			{
				_writer.WriteLine("il2cpp_codegen_no_return();");
			}
		}
		_variableSizedTypeSupport.GenerateInitializerStatements(_context);
	}

	private void WriteVariable(ResolvedTypeInfo type, string name, bool allocateVariableSized = true)
	{
		if (type.GetRuntimeStorage(_context).IsVariableSized())
		{
			if (allocateVariableSized)
			{
				string typeName = _context.Global.Services.Naming.ForVariable(type);
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{typeName} {name} = alloca({SizeOf(type)});");
			}
			_writer.WriteStatement(_writer.WriteMemset(name, 0, SizeOf(type)));
		}
		else
		{
			_writer.AddIncludesForTypeReference(_context, type.ResolvedType);
			_writer.WriteVariable(_context, type.ResolvedType, name);
		}
	}

	private void WriteDebuggerSupport()
	{
		bool hasSequencePoints = _sequencePointProvider.MethodHasSequencePoints(_methodDefinition);
		string thisVariable = "NULL";
		if (hasSequencePoints && _methodDefinition.HasThis)
		{
			_runtimeMetadataAccess.Il2CppTypeFor(_methodDefinition.DeclaringType);
			thisVariable = _context.Global.Services.Naming.ForMethodExecutionContextThisVariable();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteStatement($"DECLARE_METHOD_THIS({_context.Global.Services.Naming.ForMethodExecutionContextThisVariable()}, {Emit.AddressOf("__this")})");
		}
		string parametersVariable = "NULL";
		if (hasSequencePoints && _methodDefinition.HasParameters)
		{
			List<string> parameters = new List<string>(_methodDefinition.Parameters.Count);
			foreach (ResolvedParameter parameter in _resolvedMethodContext.Parameters.Skip(_methodDefinition.HasThis ? 1 : 0))
			{
				if (parameter.ParameterType.GetRuntimeStorage(_context).IsVariableSized())
				{
					parameters.Add(VariableSizedAnyForArgLoad(parameter));
				}
				else
				{
					parameters.Add(Emit.AddressOf(parameter.CppName));
				}
			}
			parametersVariable = _context.Global.Services.Naming.ForMethodExecutionContextParametersVariable();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteStatement($"DECLARE_METHOD_PARAMS({_context.Global.Services.Naming.ForMethodExecutionContextParametersVariable()}, {parameters.AggregateWithComma(_context)})");
		}
		string localsVariable = "NULL";
		if (hasSequencePoints && _methodDefinition.Body.HasVariables)
		{
			List<string> variables = new List<string>(localsVariable.Length);
			foreach (ResolvedVariable localVariable in _resolvedMethodContext.LocalVariables)
			{
				ResolvedTypeInfo nonPinnedAndNonByReferenceType = localVariable.VariableType.GetNonPinnedAndNonByReferenceType();
				_runtimeMetadataAccess.Il2CppTypeFor(nonPinnedAndNonByReferenceType);
				if (_methodDefinition.DebugInformation != null && _methodDefinition.DebugInformation.TryGetName(localVariable.VariableReference, out var _))
				{
					if (localVariable.VariableType.GetRuntimeStorage(_context).IsVariableSized())
					{
						variables.Add(_context.Global.Services.Naming.ForVariableName(localVariable));
					}
					else
					{
						variables.Add(Emit.AddressOf(_context.Global.Services.Naming.ForVariableName(localVariable)));
					}
				}
			}
			if (variables.Count > 0)
			{
				localsVariable = _context.Global.Services.Naming.ForMethodExecutionContextLocalsVariable();
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteStatement($"DECLARE_METHOD_LOCALS({_context.Global.Services.Naming.ForMethodExecutionContextLocalsVariable()}, {variables.AggregateWithComma(_context)})");
			}
		}
		if (_shouldEmitDebugInformation)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteStatement($"DECLARE_METHOD_EXEC_CTX({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {_runtimeMetadataAccess.MethodInfo(_methodDefinition)}, {thisVariable}, {parametersVariable}, {localsVariable})");
		}
	}

	private void WriteLocalVariables()
	{
		foreach (ResolvedVariable variable in _resolvedMethodContext.LocalVariables)
		{
			_writer.AddIncludeForTypeDefinition(variable.VariableType);
			string variableName = _context.Global.Services.Naming.ForVariableName(variable);
			WriteVariable(variable.VariableType, variableName);
		}
	}

	private void GenerateCodeRecursive(Node node, ReadOnlyDictionary<InstructionBlock, ResolvedInstructionBlock> instructionBlocks)
	{
		using (RuntimeMetadataAccessContext.Create(_runtimeMetadataAccess, node))
		{
			InstructionBlock block = node.Block;
			if (block != null)
			{
				if (node.Children.Count > 0)
				{
					throw new NotSupportedException("Node with explicit Block should have no children!");
				}
				if (block.IsDead)
				{
					if (_writer.Context.Global.Parameters.EmitComments)
					{
						WriteComment("Dead block : " + block.First.ToString());
					}
					if (IsReturnNeededForDeadBlock(block))
					{
						_writer.WriteLine("IL2CPP_UNREACHABLE;");
						_writer.WriteDefaultReturn(_context, _methodReference.GetResolvedReturnType(_context));
					}
					return;
				}
				_valueStack.Clear();
				_variableSizedTypeSupport.EnterBlock();
				if (_options.EmitBlockInfo && _writer.Context.Global.Parameters.EmitComments)
				{
					WriteComment($"BLOCK: {block.Index}");
				}
				if (_options.EmitInputAndOutputs)
				{
					DumpInsFor(block);
				}
				ResolvedInstruction ins = instructionBlocks[block].Instructions.First();
				EnterNode(node, instructionBlocks);
				GlobalVariable[] variables = _stackAnalysis.InputVariablesFor(block);
				for (int index = variables.Length - 1; index >= 0; index--)
				{
					GlobalVariable globalVariable = variables[index];
					_valueStack.Push(new StackInfo(globalVariable.VariableName, globalVariable.Type));
				}
				StackInfo? exceptionInfo = _exceptionSupport.GetActiveExceptionIfNeeded(node, _typeResolver, _context.Global.Services.TypeProvider.SystemException);
				if (exceptionInfo.HasValue)
				{
					StackInfo tempStorage = NewTemp(exceptionInfo.Value.Type);
					IGeneratedMethodCodeWriter writer = _writer;
					writer.WriteStatement($"{tempStorage.GetIdentifierExpression(_context)} = {exceptionInfo.Value.Expression};");
					_valueStack.Push(tempStorage);
				}
				_classesAlreadyInitializedInBlock.Clear();
				while (true)
				{
					SequencePoint sequencePoint = GetSequencePoint(ins.Instruction);
					if (sequencePoint != null)
					{
						if (sequencePoint.StartLine != 16707566 && _options.EmitLineNumbers)
						{
							IGeneratedMethodCodeWriter writer = _writer;
							writer.WriteUnindented($"#line {sequencePoint.StartLine} \"{sequencePoint.Document.Url.ToString().Replace("\\", "\\\\")}\"");
						}
						if (ins.OpCode != OpCodes.Nop)
						{
							_sourceAnnotationWriter.EmitAnnotation(_writer, sequencePoint);
						}
						WriteCheckSequencePoint(sequencePoint);
					}
					WriteCheckPausePoint(ins.Offset);
					if (_options.EmitIlCode && _writer.Context.Global.Parameters.EmitComments)
					{
						_writer.WriteComment(ins.ToString());
					}
					ProcessInstruction(node, block, ins);
					ProcessInstructionOperand(ins);
					if (ins.Next == null || ins.Instruction == block.Last)
					{
						break;
					}
					ins = ins.Next;
				}
				if ((ins.OpCode.Code < Code.Br_S || ins.OpCode.Code > Code.Blt_Un) && block.Successors.Any() && ins.OpCode.Code != Code.Switch)
				{
					SetupFallthroughVariables(block);
				}
				if (_options.EmitInputAndOutputs)
				{
					DumpOutsFor(block);
				}
				if (_options.EmitBlockInfo && _writer.Context.Global.Parameters.EmitComments)
				{
					if (block.Successors.Any())
					{
						WriteComment($"END BLOCK {block.Index} (succ: {block.Successors.Select((InstructionBlock b) => b.Index.ToString()).AggregateWithComma(_context)})");
					}
					else
					{
						WriteComment($"END BLOCK {block.Index} (succ: none)");
					}
					_writer.WriteLine();
					_writer.WriteLine();
				}
				ExitNode(node);
				_variableSizedTypeSupport.LeaveBlock();
				return;
			}
			if (node.Children.Count == 0)
			{
				throw new NotSupportedException("Unexpected empty node!");
			}
			EnterNode(node, instructionBlocks);
			foreach (Node child in node.Children)
			{
				if (child.Type != NodeType.Finally && child.Type != NodeType.Fault)
				{
					GenerateCodeRecursive(child, instructionBlocks);
				}
			}
			ExitNode(node);
		}
	}

	private bool IsReturnNeededForDeadBlock(InstructionBlock block)
	{
		if (_methodReference.ReturnType.IsNotVoid && !_methodReference.ReturnValueIsByRef(_context))
		{
			if (block.Last.Next == null)
			{
				Instruction first = block.First;
				if (first == null)
				{
					return true;
				}
				return first.Previous.OpCode.Code != Code.Ret;
			}
			return false;
		}
		return false;
	}

	private void ProcessInstructionOperand(ResolvedInstruction ins)
	{
		if (ins is MethodInfoResolvedInstruction && ins.OpCode != OpCodes.Ldtoken)
		{
			ProcessMethodReferenceOperand(ins.MethodInfo);
		}
		else if (ins is FieldInfoResolvedInstruction)
		{
			ProcessFieldReferenceOperand(ins.FieldInfo);
		}
		else if (ins is CallSiteInfoResolvedInstruction)
		{
			ProcessCallSiteOperand(ins.CallSiteInfo);
		}
	}

	private void ProcessCallSiteOperand(ResolvedCallSiteInfo method)
	{
		_writer.AddIncludeForTypeDefinition(method.ReturnType);
		foreach (ResolvedParameter p in method.Parameters)
		{
			_writer.AddIncludeForTypeDefinition(p.ParameterType);
		}
	}

	private void ProcessMethodReferenceOperand(ResolvedMethodInfo method)
	{
		_writer.AddIncludeForTypeDefinition(method.ReturnType);
		if (method.HasThis)
		{
			_writer.AddIncludeForTypeDefinition(method.DeclaringType);
		}
		foreach (ResolvedParameter p in method.Parameters)
		{
			_writer.AddIncludeForTypeDefinition(p.ParameterType);
		}
	}

	private void ProcessFieldReferenceOperand(ResolvedFieldInfo field)
	{
		_writer.AddIncludeForTypeDefinition(field.DeclaringType);
		_writer.AddIncludeForTypeDefinition(field.FieldType);
	}

	private void SetupFallthroughVariables(InstructionBlock block)
	{
		GlobalVariable[] outputVariables = _stackAnalysis.InputVariablesFor(block.Successors.Single());
		WriteAssignGlobalVariables(outputVariables);
		_valueStack.Clear();
		for (int index = outputVariables.Length - 1; index >= 0; index--)
		{
			GlobalVariable globalVariable = outputVariables[index];
			_valueStack.Push(new StackInfo(globalVariable.VariableName, globalVariable.Type));
		}
	}

	private void EnterNode(Node node, ReadOnlyDictionary<InstructionBlock, ResolvedInstructionBlock> instructionBlocks)
	{
		switch (node.Type)
		{
		case NodeType.Block:
			WriteLabelForBranchTarget(node.Start, node);
			_writer.BeginBlock();
			break;
		case NodeType.Try:
			EnterTry(node, instructionBlocks);
			break;
		case NodeType.Catch:
			WriteLabelForBranchTarget(node.Start, node);
			EnterCatch(node);
			break;
		case NodeType.Filter:
			WriteLabelForBranchTarget(node.Start, node);
			EnterFilter(node);
			break;
		case NodeType.Finally:
			WriteLabelForBranchTarget(node.Start, node);
			EnterFinally(node);
			break;
		case NodeType.Fault:
			WriteLabelForBranchTarget(node.Start, node);
			EnterFault(node);
			break;
		default:
			throw new NotImplementedException("Unexpected node type " + node.Type);
		}
	}

	private void EnterTry(Node node, ReadOnlyDictionary<InstructionBlock, ResolvedInstructionBlock> instructionBlocks)
	{
		bool isFault = false;
		Node finallyNode = node.FinallyNode;
		if (finallyNode == null)
		{
			finallyNode = node.FaultNode;
			isFault = true;
		}
		WriteLabelForBranchTarget(node.Start, node.Parent);
		if (finallyNode != null)
		{
			_writer.BeginBlock();
			string handlerType = (isFault ? "Fault" : "Finally");
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"auto {"__finallyBlock"} = il2cpp::utils::{handlerType}([&]");
			_writer.WriteLine("{");
			_writer.Indent();
			GenerateCodeRecursive(finallyNode, instructionBlocks);
			_writer.Dedent();
			_writer.WriteLine("});");
		}
		EmitTry();
		_writer.BeginBlock($"begin try (depth: {node.Depth})");
		WriteStoreTryId(node);
		WriteLabelForBranchTarget(node.Start, node);
	}

	private void EmitTry()
	{
		_writer.WriteLine("try");
	}

	private void EnterFinally(Node node)
	{
		_writer.BeginBlock($"begin finally (depth: {node.Depth})");
		WriteStoreTryId(node.ParentTryNode);
	}

	private void EnterFault(Node node)
	{
		_writer.BeginBlock($"begin fault (depth: {node.Depth})");
	}

	private void EnterCatch(Node node)
	{
		_writer.BeginBlock("begin catch(" + ((node.Handler.HandlerType == ExceptionHandlerType.Catch) ? node.Handler.CatchType.FullName : "filter") + ")");
		WriteStoreTryId(node.ParentTryNode);
	}

	private void EnterFilter(Node node)
	{
		_writer.BeginBlock($"begin filter(depth: {node.Depth})");
		_writer.WriteLine("bool __filter_local = false;");
		EmitTry();
		_writer.BeginBlock("begin implicit try block");
	}

	private void ExitNode(Node node)
	{
		switch (node.Type)
		{
		case NodeType.Block:
			_writer.EndBlock();
			break;
		case NodeType.Try:
			ExitTry(node);
			break;
		case NodeType.Catch:
			ExitCatch(node);
			break;
		case NodeType.Filter:
			ExitFilter(node);
			break;
		case NodeType.Finally:
			ExitFinally(node);
			break;
		case NodeType.Fault:
			ExitFault(node);
			break;
		default:
			throw new NotImplementedException("Unexpected node type " + node.Type);
		}
	}

	private void ExitTry(Node node)
	{
		_writer.EndBlock($"end try (depth: {node.Depth})");
		Node[] catchNodes = node.CatchNodes;
		Node finallyNode = node.FinallyNode;
		Node[] filterNodes = node.FilterNodes;
		Node faultNode = node.FaultNode;
		_writer.WriteLine("catch(Il2CppExceptionWrapper& e)");
		_writer.BeginBlock();
		if (catchNodes.Length != 0)
		{
			using (RuntimeMetadataAccessContext.CreateForCatchHandlers(_runtimeMetadataAccess))
			{
				_runtimeMetadataAccess.StartInitMetadataInline();
				foreach (ExceptionHandler handler in catchNodes.Select((Node n) => n.Handler))
				{
					IGeneratedMethodCodeWriter writer = _writer;
					writer.WriteLine($"if(il2cpp_codegen_class_is_assignable_from ({_runtimeMetadataAccess.TypeInfoFor(handler.CatchType)}, il2cpp_codegen_object_class(e.ex)))");
					using (new BlockWriter(_writer))
					{
						writer = _writer;
						writer.WriteLine($"{_exceptionSupport.EmitPushActiveException("e.ex")};");
						_writer.WriteLine(_labeler.ForJump(handler.HandlerStart, NodeForLeaveTargetInstruction(node, handler.HandlerStart)));
					}
				}
				_runtimeMetadataAccess.EndInitMetadataInline();
				if (finallyNode != null || faultNode != null)
				{
					_writer.WriteLine("__finallyBlock.StoreException(e.ex);");
					_writer.WriteLine(_labeler.ForJump(faultNode.Handler.HandlerStart, NodeForLeaveTargetInstruction(node, faultNode.Handler.HandlerStart)));
				}
				else
				{
					_writer.WriteLine("throw e;");
				}
			}
		}
		else if (filterNodes.Length != 0)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{_exceptionSupport.EmitPushActiveException("e.ex")};");
		}
		else
		{
			if (finallyNode == null && faultNode == null)
			{
				throw new NotSupportedException("Try block ends without any catch, finally, nor fault handler");
			}
			_writer.WriteLine("__finallyBlock.StoreException(e.ex);");
		}
		_writer.EndBlock();
		if (finallyNode != null || faultNode != null)
		{
			_writer.EndBlock();
		}
	}

	private void ExitCatch(Node node)
	{
		_writer.EndBlock($"end catch (depth: {node.Depth})");
	}

	private void ExitFilter(Node node)
	{
		_writer.EndBlock("end implicit try block");
		_writer.WriteLine("catch(Il2CppExceptionWrapper&)");
		_writer.BeginBlock("begin implicit catch block");
		_writer.WriteLine("__filter_local = false;");
		_writer.EndBlock("end implicit catch block");
		_writer.WriteLine("if (__filter_local)");
		_writer.BeginBlock();
		_writer.WriteLine(_labeler.ForJump(node.End.Next, node));
		_writer.EndBlock();
		_writer.WriteLine("else");
		_writer.BeginBlock();
		_writer.WriteStatement(Emit.RaiseManagedException(_exceptionSupport.EmitPopActiveException(SystemExceptionTypeReference), _runtimeMetadataAccess.MethodInfo(_methodReference)));
		_writer.EndBlock();
		_writer.EndBlock($"end filter (depth: {node.Depth})");
	}

	private void ExitFinally(Node node)
	{
		_writer.EndBlock($"end finally (depth: {node.Depth})");
	}

	private void ExitFault(Node node)
	{
		_writer.EndBlock("end fault");
	}

	private void DumpInsFor(InstructionBlock block)
	{
		if (!_writer.Context.Global.Parameters.EmitComments)
		{
			return;
		}
		StackState ins = _stackAnalysis.InputStackStateFor(block);
		if (ins.IsEmpty)
		{
			WriteComment("[in: -] empty");
			return;
		}
		List<Entry> entries = new List<Entry>(ins.Entries);
		for (int index = 0; index < entries.Count; index++)
		{
			Entry entry = entries[index];
			WriteComment($"[in: {index.ToString()}] {entry.Types.Select((ResolvedTypeInfo t) => t.FullName).AggregateWithComma(_context)} (null: {entry.NullValue.ToString()})");
		}
	}

	private void DumpOutsFor(InstructionBlock block)
	{
		StackState outs = _stackAnalysis.OutputStackStateFor(block);
		if (outs.IsEmpty)
		{
			WriteComment("[out: -] empty");
			return;
		}
		List<Entry> entries = new List<Entry>(outs.Entries);
		for (int index = 0; index < entries.Count; index++)
		{
			Entry entry = entries[index];
			WriteComment($"[out: {index}] {entry.Types.Select((ResolvedTypeInfo t) => t.FullName).AggregateWithComma(_context)} (null: {entry.NullValue})");
		}
	}

	private void WriteComment(string message)
	{
		_writer.WriteCommentedLine(message);
	}

	private void WriteAssignment(string leftName, ResolvedTypeInfo leftType, StackInfo right)
	{
		_writer.WriteStatement(GetAssignment(_context, leftName, leftType, right, _variableSizedTypeSupport, _sharingType));
	}

	private void WriteDeclarationAndAssignment(StackInfo left, ResolvedTypeInfo rightType, string rightExpression)
	{
		_writer.WriteStatement(GetDeclarationAndAssignment(_context, left, rightType, rightExpression, _variableSizedTypeSupport, _sharingType));
	}

	public static string GetAssignment(ReadOnlyContext context, string leftName, ResolvedTypeInfo leftType, StackInfo right, VariableSizedTypeSupport variableSizedTypeSupport, SharingType sharingType)
	{
		if (IsFullySharedTypeAssignment(context, leftType, right.Type))
		{
			return $"il2cpp_codegen_memcpy({leftName}, {right.Expression}, {variableSizedTypeSupport.RuntimeSizeFor(context, leftType)})";
		}
		return Emit.Assign(leftName, WriteExpressionAndCastIfNeeded(context, leftType, right.Type, right.Expression, sharingType));
	}

	public static string GetDeclarationAndAssignment(ReadOnlyContext context, StackInfo left, ResolvedTypeInfo rightType, string rightExpression, VariableSizedTypeSupport variableSizedTypeSupport, SharingType sharingType)
	{
		if (IsFullySharedTypeAssignment(context, left.Type, rightType))
		{
			return $"il2cpp_codegen_memcpy({left.Expression}, {rightExpression}, {variableSizedTypeSupport.RuntimeSizeFor(context, left.Type)})";
		}
		return Emit.Assign(left.GetIdentifierExpression(context), WriteExpressionAndCastIfNeeded(context, left.Type, rightType, rightExpression, sharingType));
	}

	private static bool IsFullySharedTypeAssignment(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType)
	{
		if (leftType.GetRuntimeStorage(context).IsVariableSized())
		{
			if (rightType.GetRuntimeStorage(context).IsVariableSized())
			{
				if (!leftType.IsSameType(rightType) && !rightType.IsIntegralPointerType())
				{
					throw new NotImplementedException("No support for assigning to/from variable size types of different types " + leftType.FullName + ", " + rightType.FullName);
				}
			}
			else if (!rightType.IsPointer && !rightType.IsIntegralPointerType() && !rightType.IsByReference)
			{
				throw new NotImplementedException("No support for assigning to/from variable size types of different types " + leftType.FullName + ", " + rightType.FullName);
			}
			return true;
		}
		if (rightType.GetRuntimeStorage(context).IsVariableSized())
		{
			throw new NotImplementedException("No support for assigning to/from variable size types and concrete types " + leftType.FullName + ", " + rightType.FullName);
		}
		return false;
	}

	private string WriteExpressionAndCastIfNeeded(ResolvedTypeInfo leftType, StackInfo right)
	{
		return WriteExpressionAndCastIfNeeded(_context, leftType, right.Type, right.Expression, _sharingType);
	}

	private string WriteExpressionAndCastIfNeeded(TypeReference leftType, StackInfo right)
	{
		return WriteExpressionAndCastIfNeeded(_context, leftType, right.Type, right.Expression, _sharingType);
	}

	private static string WriteExpressionAndCastIfNeeded(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType, string rightExpression, SharingType sharingType)
	{
		return WriteExpressionAndCastIfNeeded(context, leftType.ResolvedType, rightType, rightExpression, sharingType);
	}

	private static string WriteExpressionAndCastIfNeeded(ReadOnlyContext context, TypeReference leftType, ResolvedTypeInfo rightType, string rightExpression, SharingType sharingType)
	{
		if (rightType.IsSameType(leftType))
		{
			return rightExpression;
		}
		if (leftType.MetadataType == MetadataType.Boolean && rightType.IsIntegralType())
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (leftType.IsPointer || leftType.IsFunctionPointer || leftType.IsByReference)
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (leftType.IsIntegralPointerType && (rightType.IsByReference || rightType.IsPointer))
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (leftType.ContainsGenericParameter)
		{
			return rightExpression;
		}
		if (rightType.MetadataType == MetadataType.Object)
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (rightType.IsArray && leftType.IsArray)
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (rightType.MetadataType == MetadataType.IntPtr && leftType.MetadataType == MetadataType.Int32)
		{
			return $"({leftType.CppNameForVariable})({"intptr_t"}){rightExpression}";
		}
		if (rightType.IsIntegralPointerType() && leftType.MetadataType == MetadataType.IntPtr)
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if ((rightType.IsPointer || rightType.IsFunctionPointer) && leftType.IsIntegralPointerType)
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (leftType is ByReferenceType leftTypeByRef && (leftTypeByRef.ElementType.IsIntegralPointerType || leftTypeByRef.ElementType.MetadataType.IsPrimitiveType()) && rightType.IsSameType(context.Global.Services.TypeProvider.SystemIntPtr))
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (leftType.IsIntegralType && rightType.IsIntegralType() && GetMetadataTypeOrderFor(leftType) < GetMetadataTypeOrderFor(context, rightType))
		{
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (sharingType == SharingType.Shared)
		{
			if (leftType.IsUserDefinedStruct())
			{
				return $"il2cpp_codegen_cast_struct<{leftType.CppNameForVariable}, {context.Global.Services.Naming.ForVariable(rightType)}>({Emit.AddressOf(rightExpression)})";
			}
			return EmitCastRightCastToLeftType(context, leftType, rightExpression);
		}
		if (!VarianceSupport.IsNeededForConversion(leftType, rightType))
		{
			return rightExpression;
		}
		return VarianceSupport.Apply(context, leftType, rightType) + rightExpression;
	}

	private static string EmitCastRightCastToLeftType(ReadOnlyContext context, TypeReference leftType, string rightExpression)
	{
		return "(" + leftType.CppNameForVariable + ")" + rightExpression;
	}

	private SequencePoint GetSequencePoint(Instruction ins)
	{
		return _methodDefinition.DebugInformation?.GetSequencePoint(ins);
	}

	private void ProcessCustomOpCode(Node node, InstructionBlock block, ResolvedInstruction ins)
	{
		Il2CppCustomOpCode customOpCode = ins.Optimization.CustomOpCode.Value;
		switch (customOpCode)
		{
		case Il2CppCustomOpCode.Pop1:
			_valueStack.Pop();
			break;
		case Il2CppCustomOpCode.EnumHasFlag:
			WriteCustomEnumHasFlag(ins);
			break;
		case Il2CppCustomOpCode.EnumGetHashCode:
			WriteCustomEnumGetHashCode();
			break;
		case Il2CppCustomOpCode.CopyStackValue:
		{
			StackInfo value = _valueStack.Pop();
			StackInfo newCopy = NewTemp(value.Type);
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{newCopy.GetIdentifierExpression(_context)} = {value.Expression};");
			_valueStack.Push(newCopy);
			break;
		}
		case Il2CppCustomOpCode.BoxBranchOptimization:
			_valueStack.Pop();
			_valueStack.Push(new StackInfo("true", _context.Global.Services.TypeProvider.Resolved.BoolTypeReference));
			break;
		case Il2CppCustomOpCode.NullableBoxBranchOptimization:
		case Il2CppCustomOpCode.NullableIsNull:
		case Il2CppCustomOpCode.NullableIsNotNull:
		{
			StackInfo nullableExpression = _valueStack.Pop();
			InflatedFieldType hasValueField = nullableExpression.Type.ResolvedType.GetInflatedFieldTypes(_context).Single((InflatedFieldType f) => f.Field.Name == "hasValue");
			_valueStack.Push(new StackInfo($"{((customOpCode == Il2CppCustomOpCode.NullableIsNull) ? "!" : "")}{nullableExpression}.{hasValueField.Field.CppName}", (customOpCode == Il2CppCustomOpCode.NullableBoxBranchOptimization) ? ObjectTypeReference : BoolTypeReference));
			break;
		}
		case Il2CppCustomOpCode.VariableSizedBoxBranchOptimization:
		case Il2CppCustomOpCode.VariableSizedWouldBoxToNull:
		case Il2CppCustomOpCode.VariableSizedWouldBoxToNotNull:
		{
			StackInfo valueExpr = _valueStack.Pop();
			string nullCheckBox = NewTempName();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"bool {nullCheckBox} = {((customOpCode == Il2CppCustomOpCode.VariableSizedWouldBoxToNull) ? "!" : "")}il2cpp_codegen_would_box_to_non_null({_runtimeMetadataAccess.TypeInfoFor(ins.TypeInfo, IRuntimeMetadataAccess.TypeInfoForReason.WouldBoxToNull)}, {valueExpr.Expression});");
			_valueStack.Push(new StackInfo(nullCheckBox, (customOpCode == Il2CppCustomOpCode.VariableSizedBoxBranchOptimization) ? ObjectTypeReference : BoolTypeReference));
			break;
		}
		case Il2CppCustomOpCode.BranchRight:
			_valueStack.Pop();
			GenerateRightBranch(block, (Instruction)ins.Operand, node);
			break;
		case Il2CppCustomOpCode.BranchLeft:
			_valueStack.Pop();
			GenerateLeftBranch(block, (Instruction)ins.Operand);
			break;
		case Il2CppCustomOpCode.PushTrue:
			_valueStack.Push(new StackInfo("true", _context.Global.Services.TypeProvider.Resolved.BoolTypeReference));
			break;
		case Il2CppCustomOpCode.PushFalse:
			_valueStack.Push(new StackInfo("false", _context.Global.Services.TypeProvider.Resolved.BoolTypeReference));
			break;
		case Il2CppCustomOpCode.LdsfldZero:
			_valueStack.Push(new StackInfo("0", ins.FieldInfo.FieldType));
			break;
		case Il2CppCustomOpCode.BitConverterIsLittleEndian:
			_valueStack.Push(new StackInfo("il2cpp_codegen_is_little_endian()", BoolTypeReference));
			break;
		case Il2CppCustomOpCode.Nop:
			break;
		}
	}

	private void WriteCustomEnumHasFlag(ResolvedInstruction ins)
	{
		List<StackInfo> args = PopItemsFromStack(2, _valueStack);
		args[0] = GetOptimizedEnumArgument(args[0]);
		args[1] = GetOptimizedEnumArgument(args[1]);
		string thisArg = args[0].Expression;
		string flagArg = args[1].Expression;
		if (!args[0].Type.IsSameType(args[1].Type))
		{
			ResolvedTypeInfo typeForCall = StackAnalysisUtils.GetWidestValueType(_context, new ResolvedTypeInfo[2]
			{
				args[0].Type,
				args[1].Type
			});
			thisArg = Emit.Cast(_context, typeForCall, thisArg);
			flagArg = Emit.Cast(_context, typeForCall, flagArg);
		}
		StackInfo returnValue = NewTemp(ins.MethodInfo.ReturnType);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{returnValue.GetIdentifierExpression(_context)} = il2cpp_codegen_enum_has_flag({thisArg}, {flagArg});");
		_valueStack.Push(returnValue);
	}

	private void WriteCustomEnumGetHashCode()
	{
		List<StackInfo> args = PopItemsFromStack(1, _valueStack);
		args[0] = GetOptimizedEnumByRefArgument(args[0]);
		MethodReference baseMethod = args[0].Type.ResolvedType.GetMethods(_context).Single((MethodReference m) => m.Name == "GetHashCode");
		_writer.AddIncludeForTypeDefinition(_context, baseMethod.DeclaringType);
		WriteCallExpressionFor(baseMethod, MethodCallType.Normal, args, _runtimeMetadataAccess.MethodMetadataFor(baseMethod));
		_constrainedCallThisType = null;
	}

	private StackInfo GetOptimizedEnumArgument(StackInfo arg)
	{
		string argExpression = arg.Expression;
		ResolvedTypeInfo argType = arg.Type;
		if (arg.BoxedType != null)
		{
			argExpression = Emit.Dereference(Unbox(arg.BoxedType, arg));
			argType = arg.BoxedType;
		}
		return new StackInfo(argExpression, argType.IsEnum() ? argType.GetUnderlyingEnumType() : argType);
	}

	private StackInfo GetOptimizedEnumByRefArgument(StackInfo arg)
	{
		string argExpression = arg.Expression;
		ResolvedTypeInfo argType = arg.Type;
		if (arg.BoxedType != null)
		{
			argExpression = Unbox(arg.BoxedType, arg);
			argType = arg.BoxedType;
		}
		return new StackInfo(argExpression, argType.IsEnum() ? argType.GetUnderlyingEnumType().MakeByReferenceType(_context) : argType);
	}

	private void ProcessInstruction(Node node, InstructionBlock block, ResolvedInstruction ins)
	{
		_context.Global.Services.ErrorInformation.CurrentInstruction = ins.Instruction;
		Code code = ins.OpCode.Code;
		if (ins.Optimization.CustomOpCode.HasValue)
		{
			ProcessCustomOpCode(node, block, ins);
			return;
		}
		switch (code)
		{
		case Code.Ldarg_0:
		case Code.Ldarg_1:
		case Code.Ldarg_2:
		case Code.Ldarg_3:
			WriteLdarg(ins.ParameterInfo, block, ins);
			break;
		case Code.Ldloc_0:
		case Code.Ldloc_1:
		case Code.Ldloc_2:
		case Code.Ldloc_3:
			WriteLdloc(ins.VariableInfo, block, ins);
			break;
		case Code.Stloc_0:
		case Code.Stloc_1:
		case Code.Stloc_2:
		case Code.Stloc_3:
			WriteStloc(ins.VariableInfo);
			break;
		case Code.Ldarg_S:
			WriteLdarg(ins.ParameterInfo, block, ins);
			break;
		case Code.Ldarga_S:
			LoadArgumentAddress(ins.ParameterInfo);
			break;
		case Code.Starg_S:
			StoreArg(ins);
			break;
		case Code.Ldloc_S:
		case Code.Ldloc:
			WriteLdloc(ins.VariableInfo, block, ins);
			break;
		case Code.Ldloca_S:
			LoadLocalAddress(ins.VariableInfo);
			break;
		case Code.Stloc_S:
		case Code.Stloc:
			WriteStloc(ins.VariableInfo);
			break;
		case Code.Ldnull:
			LoadNull();
			break;
		case Code.Ldc_I4_M1:
			LoadInt32Constant(-1);
			break;
		case Code.Ldc_I4_0:
			LoadInt32Constant(0);
			break;
		case Code.Ldc_I4_1:
			LoadInt32Constant(1);
			break;
		case Code.Ldc_I4_2:
			LoadInt32Constant(2);
			break;
		case Code.Ldc_I4_3:
			LoadInt32Constant(3);
			break;
		case Code.Ldc_I4_4:
			LoadInt32Constant(4);
			break;
		case Code.Ldc_I4_5:
			LoadInt32Constant(5);
			break;
		case Code.Ldc_I4_6:
			LoadInt32Constant(6);
			break;
		case Code.Ldc_I4_7:
			LoadInt32Constant(7);
			break;
		case Code.Ldc_I4_8:
			LoadInt32Constant(8);
			break;
		case Code.Ldc_I4_S:
			LoadPrimitiveTypeSByte(ins, Int32TypeReference);
			break;
		case Code.Ldc_I4:
			LoadPrimitiveTypeInt32(ins, Int32TypeReference);
			break;
		case Code.Ldc_I8:
			LoadLong(ins, Int64TypeReference);
			break;
		case Code.Ldc_R4:
			LoadConstant(SingleTypeReference, Formatter.StringRepresentationFor((float)ins.Operand));
			break;
		case Code.Ldc_R8:
			LoadConstant(DoubleTypeReference, Formatter.StringRepresentationFor((double)ins.Operand));
			break;
		case Code.Dup:
			WriteDup();
			break;
		case Code.Pop:
			_valueStack.Pop();
			break;
		case Code.Jmp:
			throw new NotImplementedException("The jmp opcode is not implemented");
		case Code.Callvirt:
		{
			ResolvedMethodInfo methodToCall3 = ins.MethodInfo;
			bool benchmarkMethod = BenchmarkSupport.BeginBenchmark(methodToCall3.UnresovledMethodReference, _writer);
			WriteStoreStepOutSequencePoint(ins);
			if (methodToCall3.IsStatic())
			{
				throw new InvalidOperationException($"In method '{_methodReference.FullName}', an attempt to call the static method '{methodToCall3.FullName}' with the callvirt opcode is not valid IL. Use the call opcode instead.");
			}
			List<StackInfo> poppedValues = PopItemsFromStack(methodToCall3.Parameters.Count + 1, _valueStack);
			StackInfo thisValue = poppedValues[0];
			ResolvedTypeInfo constrainedCallThisType = _constrainedCallThisType;
			if (constrainedCallThisType != null && constrainedCallThisType.GetRuntimeStorage(_context).IsVariableSized())
			{
				WriteVariableSizedConstrainedCallExpressionFor(methodToCall3, poppedValues);
			}
			else if (_constrainedCallThisType != null || (thisValue.BoxedType != null && !thisValue.BoxedType.GetRuntimeStorage(_context).IsVariableSized()))
			{
				WriteConstrainedCallExpressionFor(ins, methodToCall3, poppedValues, out var copyBackBoxedExpr);
				if (copyBackBoxedExpr != null)
				{
					_writer.WriteStatement(copyBackBoxedExpr);
				}
			}
			else
			{
				WriteCallExpressionFor(methodToCall3, MethodCallType.Virtual, poppedValues, _runtimeMetadataAccess.MethodMetadataFor(methodToCall3), !benchmarkMethod);
			}
			_constrainedCallThisType = null;
			WriteCheckStepOutSequencePoint(ins);
			BenchmarkSupport.EndBenchmark(benchmarkMethod, _writer, _runtimeMetadataAccess, methodToCall3.ResolvedMethodReference, thisValue.Expression);
			break;
		}
		case Code.Call:
		{
			WriteStoreStepOutSequencePoint(ins);
			if (_constrainedCallThisType != null)
			{
				throw new InvalidOperationException($"Constrained opcode was followed a Call rather than a Callvirt in method '{_methodReference.FullName}' at instruction '{ins}'");
			}
			ResolvedMethodInfo methodToCall2 = ins.MethodInfo;
			List<StackInfo> args = PopItemsFromStack(methodToCall2.Parameters.Count + (methodToCall2.HasThis ? 1 : 0), _valueStack);
			bool emitNullCheck = methodToCall2.HasThis && ValueCouldBeNull(args[0]) && !methodToCall2.UnresovledMethodReference.IsConstructor;
			WriteCallExpressionFor(methodToCall2, MethodCallType.Normal, args, _runtimeMetadataAccess.MethodMetadataFor(methodToCall2), emitNullCheck);
			WriteCheckStepOutSequencePoint(ins);
			break;
		}
		case Code.Calli:
		{
			FunctionPointerCallType callType = FunctionPointerCallType.Native;
			ResolvedCallSiteInfo site = ((CallSiteInfoResolvedInstruction)ins).CallSiteInfo;
			if (site.HasThis)
			{
				_writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_not_supported_exception(\"Calli is not supported on instance methods\")"));
			}
			string callConvStr = "";
			switch (site.CallingConvention)
			{
			case MethodCallingConvention.C:
				callConvStr = "CDECL";
				break;
			case MethodCallingConvention.FastCall:
				callConvStr = "FASTCALL";
				break;
			case MethodCallingConvention.StdCall:
				callConvStr = "STDCALL";
				break;
			case MethodCallingConvention.ThisCall:
				callConvStr = "THISCALL";
				break;
			default:
				callType = FunctionPointerCallType.Managed;
				break;
			}
			string funcPointerExp = _valueStack.Pop().Expression;
			string methodInfoExp = null;
			if (callType != 0)
			{
				methodInfoExp = "(const RuntimeMethod*)" + funcPointerExp;
				funcPointerExp = "il2cpp_codegen_get_direct_method_pointer(" + methodInfoExp + ")";
			}
			WriteCallViaMethodPointer(ins, funcPointerExp, methodInfoExp, PopItemsFromStack(site.Parameters.Count, _valueStack), null, callType, site, callConvStr);
			break;
		}
		case Code.Ret:
			WriteReturnStatement();
			break;
		case Code.Br_S:
		case Code.Br:
			WriteUnconditionalJumpTo(block, (Instruction)ins.Operand, node);
			break;
		case Code.Brfalse_S:
		case Code.Brfalse:
			GenerateConditionalJump(block, ins, node, isTrue: false);
			break;
		case Code.Brtrue_S:
		case Code.Brtrue:
			GenerateConditionalJump(block, ins, node, isTrue: true);
			break;
		case Code.Beq_S:
		case Code.Beq:
			GenerateConditionalJump(block, ins, node, "==", Signedness.Signed);
			break;
		case Code.Bge_S:
		case Code.Bge:
			GenerateConditionalJump(block, ins, node, ">=", Signedness.Signed);
			break;
		case Code.Bgt_S:
		case Code.Bgt:
			GenerateConditionalJump(block, ins, node, ">", Signedness.Signed);
			break;
		case Code.Ble_S:
		case Code.Ble:
			GenerateConditionalJump(block, ins, node, "<=", Signedness.Signed);
			break;
		case Code.Blt_S:
		case Code.Blt:
			GenerateConditionalJump(block, ins, node, "<", Signedness.Signed);
			break;
		case Code.Bne_Un_S:
		case Code.Bne_Un:
			GenerateConditionalJump(block, ins, node, "==", Signedness.Unsigned, negate: true);
			break;
		case Code.Bge_Un_S:
		case Code.Bge_Un:
			GenerateConditionalJump(block, ins, node, "<", Signedness.Unsigned, negate: true);
			break;
		case Code.Bgt_Un_S:
		case Code.Bgt_Un:
			GenerateConditionalJump(block, ins, node, "<=", Signedness.Unsigned, negate: true);
			break;
		case Code.Ble_Un_S:
		case Code.Ble_Un:
			GenerateConditionalJump(block, ins, node, ">", Signedness.Unsigned, negate: true);
			break;
		case Code.Blt_Un_S:
		case Code.Blt_Un:
			GenerateConditionalJump(block, ins, node, ">=", Signedness.Unsigned, negate: true);
			break;
		case Code.Switch:
		{
			StackInfo value = _valueStack.Pop();
			Instruction[] targetInstructions = (Instruction[])ins.Operand;
			int i = 0;
			List<InstructionBlock> successors = new List<InstructionBlock>(block.Successors);
			InstructionBlock defaultSuccessor = successors.SingleOrDefault((InstructionBlock b) => !targetInstructions.Select((Instruction t) => t.Offset).Contains(b.First.Offset));
			if (defaultSuccessor != null)
			{
				successors.Remove(defaultSuccessor);
				WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(defaultSuccessor));
			}
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"switch ({value})");
			using (NewBlock())
			{
				Instruction[] array = targetInstructions;
				foreach (Instruction targetInstruction in array)
				{
					writer = _writer;
					writer.WriteLine($"case {i++}:");
					using (NewBlock())
					{
						InstructionBlock successor = successors.First((InstructionBlock b) => b.First.Offset == targetInstruction.Offset);
						WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(successor));
						WriteJump(targetInstruction, node);
					}
				}
				break;
			}
		}
		case Code.Ldind_I1:
			LoadIndirect(SByteTypeReference, Int32TypeReference);
			break;
		case Code.Ldind_U1:
			LoadIndirect(ByteTypeReference, Int32TypeReference);
			break;
		case Code.Ldind_I2:
			LoadIndirect(Int16TypeReference, Int32TypeReference);
			break;
		case Code.Ldind_U2:
			LoadIndirect(UInt16TypeReference, Int32TypeReference);
			break;
		case Code.Ldind_I4:
			LoadIndirect(Int32TypeReference, Int32TypeReference);
			break;
		case Code.Ldind_U4:
			LoadIndirect(UInt32TypeReference, Int32TypeReference);
			break;
		case Code.Ldind_I8:
			LoadIndirect(Int64TypeReference, Int64TypeReference);
			break;
		case Code.Ldind_R4:
			LoadIndirect(SingleTypeReference, SingleTypeReference);
			break;
		case Code.Ldind_R8:
			LoadIndirect(DoubleTypeReference, DoubleTypeReference);
			break;
		case Code.Ldind_I:
			LoadIndirect(SystemIntPtr, SystemIntPtr);
			break;
		case Code.Ldind_Ref:
			LoadIndirectReference();
			break;
		case Code.Stind_Ref:
			StoreIndirect(ObjectTypeReference);
			break;
		case Code.Stind_I1:
			StoreIndirect(SByteTypeReference);
			break;
		case Code.Stind_I2:
			StoreIndirect(Int16TypeReference);
			break;
		case Code.Stind_I4:
			StoreIndirect(Int32TypeReference);
			break;
		case Code.Stind_I8:
			StoreIndirect(Int64TypeReference);
			break;
		case Code.Stind_R4:
			StoreIndirect(SingleTypeReference);
			break;
		case Code.Stind_R8:
			StoreIndirect(DoubleTypeReference);
			break;
		case Code.Add:
			ArithmeticOpCodes.Add(_context, _valueStack);
			break;
		case Code.Sub:
			ArithmeticOpCodes.Sub(_context, _valueStack);
			break;
		case Code.Mul:
			ArithmeticOpCodes.Mul(_context, _valueStack);
			break;
		case Code.Div:
			_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
			WriteBinaryOperationUsingLargestOperandTypeAsResultType("/");
			break;
		case Code.Div_Un:
			_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
			WriteUnsignedArithmeticOperation("/");
			break;
		case Code.Rem:
			_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
			WriteRemainderOperation();
			break;
		case Code.Rem_Un:
			_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
			WriteUnsignedArithmeticOperation("%");
			break;
		case Code.And:
			WriteBinaryOperationUsingLeftOperandTypeAsResultType("&");
			break;
		case Code.Or:
			WriteBinaryOperationUsingLargestOperandTypeAsResultType("|");
			break;
		case Code.Xor:
			WriteBinaryOperationUsingLargestOperandTypeAsResultType("^");
			break;
		case Code.Shl:
			WriteBinaryOperationUsingLeftOperandTypeAsResultType("<<");
			break;
		case Code.Shr:
			WriteBinaryOperationUsingLeftOperandTypeAsResultType(">>");
			break;
		case Code.Shr_Un:
			WriteShrUn();
			break;
		case Code.Neg:
			WriteNegateOperation();
			break;
		case Code.Not:
			WriteNotOperation();
			break;
		case Code.Conv_I1:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, SByteTypeReference);
			break;
		case Code.Conv_I2:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, Int16TypeReference);
			break;
		case Code.Conv_I4:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, Int32TypeReference);
			break;
		case Code.Conv_I8:
			ConversionOpCodes.WriteNumericConversionI8(_methodBodyWriterContext);
			break;
		case Code.Conv_R4:
			ConversionOpCodes.WriteNumericConversionFloat(_methodBodyWriterContext, SingleTypeReference);
			break;
		case Code.Conv_R8:
			ConversionOpCodes.WriteNumericConversionFloat(_methodBodyWriterContext, DoubleTypeReference);
			break;
		case Code.Conv_U4:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, UInt32TypeReference, Int32TypeReference);
			break;
		case Code.Conv_U8:
			ConversionOpCodes.WriteNumericConversionU8(_methodBodyWriterContext);
			break;
		case Code.Cpobj:
		{
			StackInfo src2 = _valueStack.Pop();
			StackInfo dest = _valueStack.Pop();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_memcpy({dest}, {src2}, {SizeOf(ins.TypeInfo)});");
			break;
		}
		case Code.Ldobj:
			WriteLoadObject(ins, block);
			break;
		case Code.Ldstr:
		{
			string literalString = (string)ins.Operand;
			_writer.AddIncludeForTypeDefinition(StringTypeReference);
			_valueStack.Push(new StackInfo(_runtimeMetadataAccess.StringLiteral(literalString, _methodDefinition.Module.Assembly), StringTypeReference));
			break;
		}
		case Code.Newobj:
		{
			WriteStoreStepOutSequencePoint(ins);
			ResolvedMethodInfo method = ins.MethodInfo;
			ResolvedTypeInfo declaringType = method.DeclaringType;
			_writer.AddIncludeForTypeDefinition(method.DeclaringType);
			List<ResolvedTypeInfo> parameterTypes = method.Parameters.Select((ResolvedParameter p) => p.ParameterType).ToList();
			List<StackInfo> arguments = PopItemsFromStack(parameterTypes.Count, _valueStack);
			if (declaringType.IsArray)
			{
				List<string> argsFor = FormatArgumentsForMethodCall(method, parameterTypes, arguments, callingViaInvoker: false);
				StackInfo variable2 = NewTemp(declaringType);
				ArrayType arrayType2 = (ArrayType)declaringType.ResolvedType;
				if (arrayType2.Rank < 2)
				{
					throw new NotImplementedException("Attempting to create a multidimensional array of rank lesser than 2");
				}
				string arrayLengths = NewTempName();
				string genArrayNewInvocation = Emit.Call(_context, "GenArrayNew", _runtimeMetadataAccess.TypeInfoFor(method.DeclaringType), arrayLengths);
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{ArrayNaming.ForArrayIndexType()} {arrayLengths}[] = {{ {Emit.CastEach(ArrayNaming.ForArrayIndexType(), argsFor).AggregateWithComma(_context)} }};");
				_writer.WriteAssignStatement(variable2.GetIdentifierExpression(_context), Emit.Cast(_context, arrayType2, genArrayNewInvocation));
				_valueStack.Push(new StackInfo(variable2));
			}
			else if (declaringType.GetRuntimeStorage(_context).IsByValue())
			{
				List<string> argsFor2 = FormatArgumentsForMethodCall(method, parameterTypes, arguments, callingViaInvoker: false);
				StackInfo variable3 = NewTemp(declaringType);
				string thisArg = ((declaringType.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType) ? Emit.AddressOf(variable3.Expression) : Emit.CastToPointer(_context, variable3.Type, variable3.Expression));
				argsFor2.Insert(0, thisArg);
				if (MethodSignatureWriter.NeedsHiddenMethodInfo(_context, method.ResolvedMethodReference, MethodCallType.Normal, _methodReference.ContainsFullySharedGenericTypes))
				{
					argsFor2.Add((_context.Global.Parameters.EmitComments ? "/*hidden argument*/" : "") + _runtimeMetadataAccess.HiddenMethodInfo(method));
				}
				WriteVariable(declaringType, variable3.Expression, allocateVariableSized: false);
				if (IntrinsicRemap.ShouldRemap(_context, method.ResolvedMethodReference, _methodReference.ContainsFullySharedGenericTypes))
				{
					IntrinsicRemap.IntrinsicCall intrinsicCall = IntrinsicRemap.GetMappedCallFor(_writer, method.ResolvedMethodReference, _methodReference, _runtimeMetadataAccess, argsFor2);
					WriteCallAndAssignReturnValue(_writer, string.Empty, Emit.Call(_context, intrinsicCall.FunctionName, intrinsicCall.Arguments));
				}
				else
				{
					_writer.WriteStatement(Emit.Call(_context, _runtimeMetadataAccess.Method(method.ResolvedMethodReference), argsFor2));
				}
				_valueStack.Push(new StackInfo(variable3));
			}
			else if (method.Name == ".ctor" && declaringType.MetadataType == MetadataType.String)
			{
				MethodReference methodToCall = GetCreateStringMethod(method);
				List<string> argsFor3 = FormatArgumentsForMethodCall(method, parameterTypes, arguments, callingViaInvoker: false);
				argsFor3.Insert(0, "NULL");
				WriteCallExpressionFor(_methodReference, methodToCall, MethodCallType.Normal, argsFor3, _runtimeMetadataAccess.MethodMetadataFor(methodToCall), emitNullCheckForInvocation: false);
			}
			else
			{
				StackInfo variable4 = NewTemp(declaringType);
				if (CanOptimizeAwayDelegateAllocation(ins, method))
				{
					StackInfo stackVariable = NewTemp(declaringType);
					IGeneratedMethodCodeWriter writer = _writer;
					writer.WriteStatement($"{_context.Global.Services.Naming.ForTypeNameOnly(declaringType)} {stackVariable.Expression}");
					_writer.WriteStatement(_writer.WriteMemset(Emit.AddressOf(stackVariable.Expression), 0, "sizeof(" + stackVariable.Expression + ")"));
					_writer.WriteAssignStatement(variable4.GetIdentifierExpression(_context), Emit.AddressOf(stackVariable.Expression));
				}
				else
				{
					_writer.WriteAssignStatement(variable4.GetIdentifierExpression(_context), Emit.Cast(_context, declaringType, Emit.Call(_context, "il2cpp_codegen_object_new", _runtimeMetadataAccess.Newobj(method))));
				}
				arguments.Insert(0, variable4);
				WriteCallExpressionFor(method, MethodCallType.Normal, arguments, _runtimeMetadataAccess.MethodMetadataFor(method), emitNullCheckForInvocation: false);
				_valueStack.Push(new StackInfo(variable4));
			}
			WriteCheckStepOutSequencePoint(ins);
			break;
		}
		case Code.Castclass:
			WriteCastclassOrIsInst(ins.TypeInfo, _valueStack.Pop(), "Castclass");
			break;
		case Code.Isinst:
			WriteCastclassOrIsInst(ins.TypeInfo, _valueStack.Pop(), "IsInst");
			break;
		case Code.Conv_R_Un:
			ConversionOpCodes.WriteNumericConversionToFloatFromUnsigned(_methodBodyWriterContext);
			break;
		case Code.Unbox:
			Unbox(ins);
			break;
		case Code.Throw:
		{
			if (node.IsInCatchBlock)
			{
				_writer.WriteStatement(_exceptionSupport.EmitPopActiveException(SystemExceptionTypeReference));
			}
			StackInfo exc = _valueStack.Pop();
			_writer.WriteStatement(Emit.RaiseManagedException(exc.ToString(), _methodReference.IsGenericHiddenMethodNeverUsed ? "NULL" : _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		}
		case Code.Ldfld:
			LoadField(ins);
			break;
		case Code.Ldflda:
			LoadField(ins, loadAddress: true);
			break;
		case Code.Stfld:
			StoreField(ins);
			break;
		case Code.Ldsfld:
		case Code.Ldsflda:
		case Code.Stsfld:
			StaticFieldAccess(ins);
			break;
		case Code.Stobj:
			WriteStoreObject(ins.TypeInfo);
			break;
		case Code.Conv_Ovf_I1_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, ByteTypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(sbyte.MaxValue));
			break;
		case Code.Conv_Ovf_I2_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int16TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(short.MaxValue));
			break;
		case Code.Conv_Ovf_I4_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int32TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(int.MaxValue));
			break;
		case Code.Conv_Ovf_I8_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int64TypeReference, treatInputAsUnsigned: true, long.MaxValue + "ULL");
			break;
		case Code.Conv_Ovf_U1_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, ByteTypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(byte.MaxValue));
			break;
		case Code.Conv_Ovf_U2_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt16TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(ushort.MaxValue));
			break;
		case Code.Conv_Ovf_U4_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt32TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(uint.MaxValue));
			break;
		case Code.Conv_Ovf_U8_Un:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt64TypeReference, treatInputAsUnsigned: true, ulong.MaxValue + "ULL");
			break;
		case Code.Conv_Ovf_I_Un:
			ConversionOpCodes.ConvertToNaturalIntWithOverflow(_methodBodyWriterContext, SystemIntPtr, treatInputAsUnsigned: true, "INTPTR_MAX");
			break;
		case Code.Conv_Ovf_U_Un:
			ConversionOpCodes.ConvertToNaturalIntWithOverflow(_methodBodyWriterContext, SystemUIntPtr, treatInputAsUnsigned: true, "UINTPTR_MAX");
			break;
		case Code.Box:
		{
			ResolvedTypeInfo type3 = ins.TypeInfo;
			ResolvedTypeInfo boxedType = type3;
			_writer.AddIncludeForTypeDefinition(type3);
			if (type3.GetRuntimeStorage(_context).IsVariableSized())
			{
				StackInfo valueExpr2 = _valueStack.Pop();
				StoreLocalAndPush(ObjectTypeReference, $"Box({_runtimeMetadataAccess.TypeInfoFor(boxedType, IRuntimeMetadataAccess.TypeInfoForReason.Box)}, {valueExpr2.Expression})", boxedType);
				break;
			}
			StackInfo valueExpr3 = _valueStack.Pop();
			bool num = type3.MetadataType == MetadataType.IntPtr || type3.MetadataType == MetadataType.UIntPtr;
			bool rightIsNativeInt = valueExpr3.Type.IsSameType(SystemIntPtr) || valueExpr3.Type.IsSameType(SystemUIntPtr);
			if (num && rightIsNativeInt)
			{
				type3 = valueExpr3.Type;
			}
			StackInfo variable = NewTemp(type3);
			if (type3.IsPrimitive)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {CastTypeIfNeeded(valueExpr3, variable.Type)};");
			}
			else
			{
				WriteAssignment(variable.GetIdentifierExpression(_context), variable.Type, valueExpr3);
			}
			StoreLocalAndPush(ObjectTypeReference, $"Box({_runtimeMetadataAccess.TypeInfoFor(boxedType, IRuntimeMetadataAccess.TypeInfoForReason.Box)}, &{variable.Expression})", boxedType);
			break;
		}
		case Code.Newarr:
		{
			StackInfo elementCount = _valueStack.Pop();
			ResolvedTypeInfo arrayType = ins.TypeInfo.MakeArrayType(_context);
			_writer.AddIncludeForTypeDefinition(arrayType);
			string elementCountExpression = "(uint32_t)" + elementCount.Expression;
			StoreLocalAndPush(arrayType, Emit.Cast(_context, arrayType, Emit.NewSZArray(_context, (ArrayType)arrayType.ResolvedType, (ArrayType)arrayType.UnresolvedType, elementCountExpression, _runtimeMetadataAccess)));
			break;
		}
		case Code.Ldlen:
		{
			StackInfo stackInfo = _valueStack.Pop();
			_nullCheckSupport.WriteNullCheckIfNeeded(_context, stackInfo);
			PushExpression(UInt32TypeReference, $"(({"RuntimeArray"}*){stackInfo})->max_length");
			break;
		}
		case Code.Ldelema:
		{
			StackInfo index4 = _valueStack.Pop();
			StackInfo array5 = _valueStack.Pop();
			ResolvedTypeInfo byReferenceType = ins.TypeInfo.MakeByReferenceType(_context);
			_nullCheckSupport.WriteNullCheckIfNeeded(_context, array5);
			PushExpression(byReferenceType, EmitArrayLoadElementAddress(array5, index4.Expression));
			break;
		}
		case Code.Ldelem_I1:
			LoadElemAndPop(SByteTypeReference);
			break;
		case Code.Ldelem_U1:
			LoadElemAndPop(ByteTypeReference);
			break;
		case Code.Ldelem_I2:
			LoadElemAndPop(Int16TypeReference);
			break;
		case Code.Ldelem_U2:
			LoadElemAndPop(UInt16TypeReference);
			break;
		case Code.Ldelem_I4:
			LoadElemAndPop(Int32TypeReference);
			break;
		case Code.Ldelem_U4:
			LoadElemAndPop(UInt32TypeReference);
			break;
		case Code.Ldelem_I8:
			LoadElemAndPop(Int64TypeReference);
			break;
		case Code.Ldelem_I:
			LoadElemAndPop(IntPtrTypeReference);
			break;
		case Code.Ldelem_R4:
			LoadElemAndPop(SingleTypeReference);
			break;
		case Code.Ldelem_R8:
			LoadElemAndPop(DoubleTypeReference);
			break;
		case Code.Ldelem_Ref:
		{
			StackInfo index3 = _valueStack.Pop();
			StackInfo array4 = _valueStack.Pop();
			LoadElemRef(array4, index3);
			break;
		}
		case Code.Stelem_I:
		case Code.Stelem_I1:
		case Code.Stelem_I2:
		case Code.Stelem_I4:
		case Code.Stelem_I8:
		case Code.Stelem_R4:
		case Code.Stelem_R8:
		case Code.Stelem_Any:
		{
			StackInfo value3 = _valueStack.Pop();
			StackInfo index2 = _valueStack.Pop();
			StackInfo array3 = _valueStack.Pop();
			StoreElement(array3, index2, value3, emitElementTypeCheck: false);
			break;
		}
		case Code.Ldelem_Any:
			LoadElemAndPop(ins.TypeInfo);
			break;
		case Code.Stelem_Ref:
		{
			StackInfo value2 = _valueStack.Pop();
			StackInfo index = _valueStack.Pop();
			StackInfo array2 = _valueStack.Pop();
			ResolvedTypeInfo elementType = array2.Type.GetElementType();
			bool emitElementTypeCheck = !elementType.IsSealed || !elementType.IsSameType(value2.Type);
			StoreElement(array2, index, value2, emitElementTypeCheck);
			break;
		}
		case Code.Unbox_Any:
		{
			StackInfo boxedValue = _valueStack.Pop();
			ResolvedTypeInfo type2 = ins.TypeInfo;
			_writer.AddIncludeForTypeDefinition(type2);
			if (type2.GetRuntimeStorage(_context).IsVariableSized())
			{
				PushExpression(type2, Emit.InParentheses(Emit.Cast(_context, type2, Unbox(type2, boxedValue))));
			}
			else if (type2.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType)
			{
				PushExpression(type2, Emit.InParentheses(Emit.Dereference(Unbox(type2, boxedValue))));
			}
			else if (boxedValue.Type.IsSameType(type2))
			{
				_valueStack.Push(boxedValue);
			}
			else
			{
				WriteCastclassOrIsInst(type2, boxedValue, "Castclass");
			}
			break;
		}
		case Code.Conv_Ovf_I1:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, SByteTypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(sbyte.MaxValue));
			break;
		case Code.Conv_Ovf_U1:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, ByteTypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(byte.MaxValue));
			break;
		case Code.Conv_Ovf_I2:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int16TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(short.MaxValue));
			break;
		case Code.Conv_Ovf_U2:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt16TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(ushort.MaxValue));
			break;
		case Code.Conv_Ovf_I4:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int32TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(int.MaxValue));
			break;
		case Code.Conv_Ovf_U4:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt32TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(uint.MaxValue));
			break;
		case Code.Conv_Ovf_I8:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, Int64TypeReference, treatInputAsUnsigned: false, "(std::numeric_limits<int64_t>::max)()");
			break;
		case Code.Conv_Ovf_U8:
			ConversionOpCodes.WriteNumericConversionWithOverflow(_methodBodyWriterContext, UInt64TypeReference, treatInputAsUnsigned: true, "(std::numeric_limits<uint64_t>::max)()");
			break;
		case Code.Ckfinite:
			throw new NotImplementedException("The chkfinite opcode is not implemented");
		case Code.Mkrefany:
		{
			StackInfo pointer = _valueStack.Pop();
			ResolvedTypeInfo refType = ins.TypeInfo;
			TypeDefinition typedReference3 = _context.Global.Services.TypeProvider.TypedReference;
			_writer.AddIncludeForTypeDefinition(_writer.Context, typedReference3);
			StackInfo local = NewTemp(_context.Global.Services.TypeProvider.Resolved.TypedReference);
			_writer.WriteAssignStatement(local.GetIdentifierExpression(_context), "{ 0 }");
			FieldDefinition typeField3 = typedReference3.GetRuntimeRequiredField("type", RuntimeTypeHandleTypeReference);
			_writer.WriteFieldSetter(typeField3, local.Expression + "." + typeField3.CppName, "{ reinterpret_cast<intptr_t>(" + _runtimeMetadataAccess.Il2CppTypeFor(refType.UnresolvedType) + ") }");
			FieldDefinition valueField2 = typedReference3.GetRuntimeRequiredField("Value", IntPtrTypeReference);
			_writer.WriteFieldSetter(valueField2, local.Expression + "." + valueField2.CppName, "reinterpret_cast<intptr_t>(" + pointer.Expression + ")");
			FieldDefinition classField = typedReference3.GetRuntimeRequiredField("Type", IntPtrTypeReference);
			_writer.WriteFieldSetter(classField, local.Expression + "." + classField.CppName, "reinterpret_cast<intptr_t>(" + _runtimeMetadataAccess.TypeInfoFor(refType.UnresolvedType) + ")");
			PushExpression(local.Type, local.Expression);
			break;
		}
		case Code.Ldtoken:
			EmitLoadToken(ins);
			break;
		case Code.Conv_U2:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, UInt16TypeReference, Int32TypeReference);
			break;
		case Code.Conv_U1:
			ConversionOpCodes.WriteNumericConversion(_methodBodyWriterContext, ByteTypeReference, Int32TypeReference);
			break;
		case Code.Conv_I:
			ConversionOpCodes.ConvertToNaturalInt(_methodBodyWriterContext, SystemIntPtr);
			break;
		case Code.Conv_Ovf_I:
			ConversionOpCodes.ConvertToNaturalIntWithOverflow(_methodBodyWriterContext, SystemIntPtr, treatInputAsUnsigned: false, "INTPTR_MAX");
			break;
		case Code.Conv_Ovf_U:
			ConversionOpCodes.ConvertToNaturalIntWithOverflow(_methodBodyWriterContext, SystemUIntPtr, treatInputAsUnsigned: false, "UINTPTR_MAX");
			break;
		case Code.Add_Ovf:
			ArithmeticOpCodes.Add(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Add_Ovf_Un:
			ArithmeticOpCodes.Add(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Mul_Ovf:
			ArithmeticOpCodes.Mul(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Mul_Ovf_Un:
			ArithmeticOpCodes.Mul(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Sub_Ovf:
			ArithmeticOpCodes.Sub(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Sub_Ovf_Un:
			ArithmeticOpCodes.Sub(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
			break;
		case Code.Endfinally:
			_writer.WriteLine("return;");
			break;
		case Code.Leave:
		case Code.Leave_S:
			if (ShouldStripLeaveInstruction(block, ins))
			{
				_writer.Write(";");
				if (_writer.Context.Global.Parameters.EmitComments)
				{
					_writer.WriteComment(ins.ToString());
				}
				_writer.WriteLine();
				break;
			}
			switch (node.Type)
			{
			case NodeType.Try:
				EmitCodeForLeaveFromTry(node, ins);
				break;
			case NodeType.Catch:
				EmitCodeForLeaveFromCatch(node, ins);
				break;
			case NodeType.Finally:
			case NodeType.Fault:
				EmitCodeForLeaveFromFinallyOrFault();
				break;
			case NodeType.Block:
			case NodeType.Root:
				EmitCodeForLeaveFromBlock(node, ins);
				break;
			}
			_valueStack.Clear();
			break;
		case Code.Stind_I:
			StoreIndirect(SystemIntPtr);
			break;
		case Code.Conv_U:
			ConversionOpCodes.ConvertToNaturalInt(_methodBodyWriterContext, SystemUIntPtr);
			break;
		case Code.Arglist:
			_writer.WriteLine("#pragma message(FIXME \"arglist is not supported\")");
			_writer.WriteLine("IL2CPP_ASSERT(false && \"arglist is not supported\");");
			StoreLocalAndPush(_context.Global.Services.TypeProvider.Resolved.RuntimeArgumentHandleTypeReference, "{ 0 }");
			break;
		case Code.Ceq:
			GenerateConditional("==", Signedness.Signed);
			break;
		case Code.Cgt:
			GenerateConditional(">", Signedness.Signed);
			break;
		case Code.Cgt_Un:
			GenerateConditional("<=", Signedness.Unsigned, negate: true);
			break;
		case Code.Clt:
			GenerateConditional("<", Signedness.Signed);
			break;
		case Code.Clt_Un:
			GenerateConditional(">=", Signedness.Unsigned, negate: true);
			break;
		case Code.Ldftn:
			PushCallToLoadFunction(ins.MethodInfo);
			break;
		case Code.Ldvirtftn:
			LoadVirtualFunction(ins);
			break;
		case Code.Ldarg:
			WriteLdarg(ins.ParameterInfo, block, ins);
			break;
		case Code.Ldarga:
			LoadArgumentAddress(ins.ParameterInfo);
			break;
		case Code.Starg:
			StoreArg(ins);
			break;
		case Code.Ldloca:
			LoadLocalAddress(ins.VariableInfo);
			break;
		case Code.Localloc:
		{
			StackInfo size = _valueStack.Pop();
			ResolvedTypeInfo charPtr = SByteTypeReference.MakePointerType(_context);
			StackInfo allocSize = NewTemp(size.Type);
			string allocatedMem = NewTempName();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteStatement($"{allocSize.GetIdentifierExpression(_context)} = {size.Expression}");
			writer = _writer;
			writer.WriteStatement($"{_context.Global.Services.Naming.ForVariable(charPtr)} {allocatedMem} = ({_context.Global.Services.Naming.ForVariable(charPtr)}) ({allocSize.Expression} ? alloca({allocSize.Expression}) : {"NULL"})");
			if (_methodDefinition.Body.InitLocals)
			{
				_writer.WriteStatement(_writer.WriteMemset(allocatedMem, 0, allocSize.Expression));
			}
			PushExpression(charPtr, allocatedMem);
			break;
		}
		case Code.Endfilter:
		{
			StackInfo val3 = _valueStack.Pop();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{"__filter_local"} = ({val3}) ? true : false;");
			break;
		}
		case Code.Volatile:
			AddVolatileStackEntry();
			break;
		case Code.Tail:
			_context.Global.Collectors.Stats.RecordTailCall(_methodDefinition);
			break;
		case Code.Initobj:
		{
			StackInfo val2 = _valueStack.Pop();
			ResolvedTypeInfo type = ins.TypeInfo;
			_writer.AddIncludeForTypeDefinition(type);
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_initobj({val2.Expression}, {SizeOf(type)});");
			break;
		}
		case Code.Constrained:
			_constrainedCallThisType = ins.TypeInfo;
			_writer.AddIncludeForTypeDefinition(ins.TypeInfo);
			break;
		case Code.Cpblk:
		{
			StackInfo bytes2 = _valueStack.Pop();
			StackInfo src = _valueStack.Pop();
			StackInfo dst = _valueStack.Pop();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_memcpy({dst}, {src}, {bytes2});");
			break;
		}
		case Code.Initblk:
		{
			StackInfo bytes = _valueStack.Pop();
			StackInfo val = _valueStack.Pop();
			StackInfo ptr = _valueStack.Pop();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_memset({ptr}, {val}, {bytes});");
			break;
		}
		case Code.No:
			throw new NotImplementedException("The 'no' opcode is not implemented");
		case Code.Rethrow:
		{
			string activeException = _exceptionSupport.EmitPopActiveException(SystemExceptionTypeReference);
			if (node.Type == NodeType.Finally)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{"__finallyBlock"}.StoreException({activeException});");
				WriteJump(node.Handler.HandlerStart, node);
			}
			else
			{
				_writer.WriteStatement(Emit.RethrowManagedException(activeException));
			}
			break;
		}
		case Code.Sizeof:
		{
			ResolvedTypeInfo typeReference = ins.TypeInfo;
			_writer.AddIncludeForTypeDefinition(typeReference);
			StoreLocalAndPush(UInt32TypeReference, SizeOf(typeReference));
			break;
		}
		case Code.Refanytype:
		{
			StackInfo typeToken2 = _valueStack.Pop();
			TypeDefinition typedReference2 = _context.Global.Services.TypeProvider.TypedReference;
			_writer.AddIncludeForTypeDefinition(_writer.Context, typedReference2);
			if (!typeToken2.Type.IsSameType(typedReference2))
			{
				throw new InvalidOperationException($"Refanytype (__reftype) can only be used with a System.TypedReference, but found {typeToken2.Type}");
			}
			FieldDefinition typeField2 = typedReference2.GetRuntimeRequiredField("type", RuntimeTypeHandleTypeReference);
			string typeExpr2 = typeToken2.Expression + "." + typeField2.CppName;
			_valueStack.Push(new StackInfo(typeExpr2, RuntimeTypeHandleTypeReference));
			break;
		}
		case Code.Refanyval:
		{
			StackInfo typeToken = _valueStack.Pop();
			TypeDefinition typedReference = _context.Global.Services.TypeProvider.TypedReference;
			_writer.AddIncludeForTypeDefinition(_writer.Context, typedReference);
			if (!typeToken.Type.IsSameType(typedReference))
			{
				throw new InvalidOperationException($"Refanyval (__refval) can only be used with a System.TypedReference, but found {typeToken.Type}");
			}
			FieldDefinition typeField = typedReference.GetRuntimeRequiredField("Type", IntPtrTypeReference);
			string typeExpr = typeToken.Expression + "." + typeField.CppName;
			using (new IndentWriter($"if ({typeExpr} != reinterpret_cast<intptr_t>({_runtimeMetadataAccess.TypeInfoFor(ins.TypeInfo)}))", _writer))
			{
				_writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_invalid_cast_exception(\"\")"));
			}
			FieldDefinition valueField = typedReference.GetRuntimeRequiredField("Value", IntPtrTypeReference);
			string valueExpr = typeToken.Expression + "." + valueField.CppName;
			_valueStack.Push(new StackInfo(valueExpr, ins.TypeInfo.MakeByReferenceType(_context)));
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case Code.Nop:
		case Code.Break:
		case Code.Unaligned:
		case Code.Readonly:
			break;
		}
	}

	private void WriteCallViaMethodPointer(ResolvedInstruction ins, string funcPointerExp, string methodInfoExp, List<StackInfo> parameters, ResolvedTypeInfo thisType, FunctionPointerCallType callType, IResolvedMethodSignature methodSig, string callConvStr)
	{
		if (methodSig.HasThis && thisType == null)
		{
			throw new InvalidOperationException("Unsupported call via a method pointer without a thisType supplied");
		}
		if (callType != 0 && string.IsNullOrEmpty(methodInfoExp))
		{
			throw new ArgumentException("A managed call must have a method info supplied");
		}
		ResolvedTypeInfo returnType = methodSig.ReturnType;
		List<ResolvedTypeInfo> parameterTypes = methodSig.Parameters.Select((ResolvedParameter p) => p.ParameterType).ToList();
		TypeReference[] resolvedParameterTypes = parameterTypes.Select((ResolvedTypeInfo p) => p.ResolvedType).ToArray();
		if (returnType.GetRuntimeStorage(_context).IsVariableSized() || resolvedParameterTypes.Any((TypeReference p) => p.GetRuntimeStorage(_context).IsVariableSized()))
		{
			if (callType == FunctionPointerCallType.Native)
			{
				_writer.WriteStatement("il2cpp_codegen_raise_exception(il2cpp_codegen_get_not_supported_exception(\"Cannot call native function pointer with generic arguments from fully shared generic code\"))");
				if (returnType.IsNotVoid())
				{
					_valueStack.Push(NewTemp(returnType));
				}
				return;
			}
			callType = FunctionPointerCallType.Invoker;
		}
		if (callType == FunctionPointerCallType.Managed)
		{
			_context.Global.Collectors.IndirectCalls.Add(_context.SourceWritingContext, returnType.ResolvedType, resolvedParameterTypes, methodSig.HasThis ? IndirectCallUsage.Instance : IndirectCallUsage.Static);
		}
		if (thisType != null)
		{
			parameterTypes.Insert(0, thisType.GetRuntimeStorage(_context).IsByValue() ? thisType.MakeByReferenceType(_context) : thisType);
			_writer.AddIncludeForTypeDefinition(thisType);
		}
		_writer.AddIncludeForTypeDefinition(_context, returnType.ResolvedType);
		using Returnable<StringBuilder> callExpBuilderBuilder = _context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder callExpBuilder = callExpBuilderBuilder.Value;
		bool callExpHasArguments = false;
		if (callType == FunctionPointerCallType.Invoker)
		{
			funcPointerExp = _writer.VirtualCallInvokeMethod(methodSig.UnresolvedMethodSignature, _typeResolver, VirtualMethodCallType.InvokerCall, doCallViaInvoker: false, resolvedParameterTypes);
			callExpBuilder.Append(funcPointerExp);
			callExpBuilder.Append("(il2cpp_codegen_get_direct_method_pointer(");
			callExpBuilder.Append(methodInfoExp);
			callExpBuilder.Append("),");
			callExpBuilder.Append(methodInfoExp);
			callExpBuilder.Append(",NULL");
			callExpHasArguments = true;
		}
		else
		{
			string funcName = EmitFunctionPointerTypeDef(returnType, parameterTypes, callConvStr, callType == FunctionPointerCallType.Managed);
			StringBuilder stringBuilder = callExpBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder);
			handler.AppendLiteral("((");
			handler.AppendFormatted(funcName);
			handler.AppendLiteral(")");
			handler.AppendFormatted(funcPointerExp);
			handler.AppendLiteral(")(");
			stringBuilder.Append(ref handler);
		}
		List<string> formattedArguments = FormatArgumentsForMethodCall(null, parameterTypes, parameters, callType == FunctionPointerCallType.Invoker);
		if (formattedArguments.Any())
		{
			if (callExpHasArguments)
			{
				callExpBuilder.Append(',');
			}
			callExpBuilder.AppendAggregateWithComma((IList<string>)formattedArguments);
			callExpHasArguments = true;
		}
		if (returnType.IsReturnedByRef(_context))
		{
			if (callExpHasArguments)
			{
				callExpBuilder.Append(',');
			}
			callExpHasArguments = true;
			StackInfo variableName = NewTemp(returnType);
			_valueStack.Push(new StackInfo(variableName.Expression, returnType));
			callExpBuilder.Append(Emit.CastToPointer(_context, returnType, variableName.Expression));
		}
		if (callType == FunctionPointerCallType.Managed)
		{
			if (callExpHasArguments)
			{
				callExpBuilder.Append(',');
			}
			callExpHasArguments = true;
			callExpBuilder.Append(methodInfoExp);
		}
		callExpBuilder.Append(')');
		if (returnType.IsVoid() || returnType.IsReturnedByRef(_context))
		{
			_writer.WriteStatement(callExpBuilder.ToString());
		}
		else if (ins.Next != null && ins.Next.OpCode.Code == Code.Pop)
		{
			_writer.WriteStatement(callExpBuilder.ToString());
			_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
		}
		else
		{
			StackInfo variableName2 = NewTemp(returnType);
			_valueStack.Push(new StackInfo(variableName2.Expression, returnType));
			WriteDeclarationAndAssignment(variableName2, returnType, callExpBuilder.ToString());
		}
	}

	private string EmitFunctionPointerTypeDef(ResolvedTypeInfo returnType, List<ResolvedTypeInfo> parameterTypes, string callConvStr, bool isMangedCallingConvention)
	{
		_writer.Write("typedef ");
		_writer.Write(_context.Global.Services.Naming.ForVariable(returnType));
		string funcName = "func_" + NewTempName();
		IGeneratedMethodCodeWriter writer = _writer;
		writer.Write($" ({callConvStr} *{funcName})(");
		for (int i = 0; i < parameterTypes.Count; i++)
		{
			_writer.Write(_context.Global.Services.Naming.ForVariable(parameterTypes[i]));
			if (i < parameterTypes.Count - 1)
			{
				_writer.Write(",");
			}
		}
		if (isMangedCallingConvention)
		{
			if (parameterTypes.Count > 0)
			{
				_writer.Write(",");
			}
			_writer.Write("const RuntimeMethod*");
		}
		_writer.WriteLine(");");
		return funcName;
	}

	private static bool CanOptimizeAwayDelegateAllocation(ResolvedInstruction ins, ResolvedMethodInfo method)
	{
		if (method.DeclaringType.IsDelegate())
		{
			ResolvedInstruction next = ins.Next;
			if (next != null && (next.OpCode == OpCodes.Call || next.OpCode == OpCodes.Callvirt) && next.Operand is GenericInstanceMethod nextMethod && nextMethod.DeclaringType.FullName == "Unity.Entities.EntityQueryBuilder" && nextMethod.Name == "ForEach")
			{
				return true;
			}
		}
		return false;
	}

	private void WriteCheckSequencePoint(SequencePoint sequencePoint)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
		{
			SequencePointInfo sequencePointInfo = _sequencePointProvider.GetSequencePointAt(_methodDefinition, sequencePoint.Offset, SequencePointKind.Normal);
			WriteCheckSequencePoint(sequencePointInfo);
		}
	}

	private string GetSequencePoint(SequencePointInfo sequencePointInfo)
	{
		int index = _sequencePointProvider.GetSeqPointIndex(sequencePointInfo);
		string sequencePoints = DebugWriter.GetSequencePointName(_context, _methodDefinition.Module.Assembly);
		_writer.AddForwardDeclaration("IL2CPP_EXTERN_C Il2CppSequencePoint " + sequencePoints + "[]");
		return $"({sequencePoints} + {index})";
	}

	private void WriteCheckSequencePoint(SequencePointInfo sequencePointInfo)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
		{
			if (sequencePointInfo.IlOffset == -1)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"CHECK_METHOD_ENTRY_SEQ_POINT({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {GetSequencePoint(sequencePointInfo)});");
			}
			else
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"CHECK_SEQ_POINT({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {GetSequencePoint(sequencePointInfo)});");
			}
		}
	}

	private void WriteCheckMethodExitSequencePoint(SequencePointInfo sequencePointInfo)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"CHECK_METHOD_EXIT_SEQ_POINT({_context.Global.Services.Naming.ForMethodExitSequencePointChecker()}, {_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {GetSequencePoint(sequencePointInfo)});");
		}
	}

	private void WriteCheckStepOutSequencePoint(ResolvedInstruction ins)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.TryGetSequencePointAt(_methodDefinition, ins.Offset, SequencePointKind.StepOut, out var sequencePoint))
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"CHECK_SEQ_POINT({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {GetSequencePoint(sequencePoint)});");
		}
	}

	private void WriteStoreStepOutSequencePoint(ResolvedInstruction ins)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.TryGetSequencePointAt(_methodDefinition, ins.Offset, SequencePointKind.StepOut, out var sequencePoint))
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"STORE_SEQ_POINT({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {GetSequencePoint(sequencePoint)});");
		}
	}

	private void WriteCheckPausePoint(int offset)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.MethodHasPausePointAtOffset(_methodDefinition, offset))
		{
			_writer.WriteLine("CHECK_PAUSE_POINT;");
		}
	}

	private void WriteStoreTryId(Node node)
	{
		if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"STORE_TRY_ID({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {node?.Id ?? (-1)});");
		}
	}

	private static string AppendLongLongLiteralSuffix<T>(T value)
	{
		return $"{value}LL";
	}

	private void WriteReturnStatement()
	{
		ResolvedTypeInfo returnType = _resolvedTypeFactory.ResolveReturnType(_methodDefinition);
		bool num = returnType.IsVoid();
		if (!num && _valueStack.Count > 1)
		{
			throw new InvalidOperationException("Attempting to return a value from method '" + _methodDefinition.FullName + "' when there is no value on the stack. Is this invalid IL code?");
		}
		if (num && _valueStack.Count > 0)
		{
			throw new InvalidOperationException("Attempting to return from void method '" + _methodDefinition.FullName + "' when there are values on the stack. Is this invalid IL code?");
		}
		if (!num)
		{
			StackInfo returnValue = _valueStack.Pop();
			string returnExpression = string.Empty;
			returnExpression = ((returnType.IsSameType(returnValue.Type) || (!returnType.IsPointer && !returnType.IsByReference && !returnType.IsEnum())) ? WriteExpressionAndCastIfNeeded(returnType, returnValue) : $"({_context.Global.Services.Naming.ForVariable(returnType)})({returnValue.Expression})");
			if (returnType.GetRuntimeStorage(_context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({"il2cppRetVal"}, {returnExpression}, {_variableSizedTypeSupport.RuntimeSizeFor(_context, returnType)});");
				_writer.WriteReturnStatement();
			}
			else
			{
				_writer.WriteReturnStatement(returnExpression);
			}
		}
		else
		{
			_writer.WriteLine("return;");
		}
	}

	private static bool CanApplyValueTypeBoxBranchOptimizationToInstruction(ResolvedInstruction ins, InstructionBlock block)
	{
		if (ins == null)
		{
			return false;
		}
		return ins.Optimization.CustomOpCode == Il2CppCustomOpCode.BoxBranchOptimization;
	}

	private bool ValueCouldBeNull(StackInfo value)
	{
		if (value.Type.GetRuntimeStorage(_context).IsByValue())
		{
			return false;
		}
		if (value.BoxedType == null)
		{
			return true;
		}
		if (value.Type.IsSystemObject())
		{
			return value.BoxedType.IsNullableGenericInstance();
		}
		return true;
	}

	private void WriteConstrainedCallExpressionFor(ResolvedInstruction ins, ResolvedMethodInfo origMethodToCall, List<StackInfo> poppedValues, out string copyBackBoxedExpr)
	{
		StackInfo thisValue = poppedValues[0];
		ResolvedTypeInfo thisType = null;
		bool shouldUnbox = false;
		ResolvedTypeInfo constrainedType = _constrainedCallThisType;
		bool boxedValueCanBeNull = false;
		if (thisValue.Type.IsByReference)
		{
			thisType = thisValue.Type.GetElementType();
		}
		else if (thisValue.Type.IsPointer)
		{
			if (thisValue.Type.IsSameType(_context.Global.Services.TypeProvider.Resolved.SystemVoidPointer))
			{
				thisType = constrainedType;
				ResolvedTypeInfo pointerType = constrainedType.MakePointerType(_context);
				thisValue = (poppedValues[0] = new StackInfo(Emit.Cast(_context, pointerType, thisValue.Expression), pointerType));
			}
			else
			{
				thisType = thisValue.Type.GetElementType();
			}
		}
		else if (thisValue.BoxedType != null)
		{
			shouldUnbox = true;
			constrainedType = thisValue.BoxedType;
			thisType = thisValue.BoxedType;
			boxedValueCanBeNull = ValueCouldBeNull(thisValue);
		}
		else if (thisValue.Type.IsIntegralPointerType())
		{
			thisType = constrainedType;
			ResolvedTypeInfo pointerType2 = constrainedType.MakePointerType(_context);
			thisValue = (poppedValues[0] = new StackInfo(Emit.Cast(_context, pointerType2, thisValue.Expression), pointerType2));
		}
		if (thisValue.BoxedType == null)
		{
			if (thisType == null)
			{
				throw new InvalidOperationException("Attempting to constrain an invalid type.");
			}
			if (!constrainedType.IsSameType(thisType) && !thisType.IsIntegralType())
			{
				throw new InvalidOperationException($"Attempting to constrain a value of type '{thisType}' to type '{constrainedType}'.");
			}
		}
		copyBackBoxedExpr = null;
		bool isConstrainedGenericType = _sharingType == SharingType.Shared && !shouldUnbox && (constrainedType.UnresolvedType.ContainsGenericParameter || origMethodToCall.UnresovledMethodReference.ContainsGenericParameter || origMethodToCall.UnresovledMethodReference.DeclaringType.ContainsGenericParameter);
		if (!thisType.GetRuntimeStorage(_context).IsByValue() && !thisType.IsPointer)
		{
			poppedValues[0] = new StackInfo(Emit.InParentheses(Emit.Dereference(thisValue.Expression)), thisType);
			_writer.AddIncludeForTypeDefinition(origMethodToCall.DeclaringType);
			if (isConstrainedGenericType)
			{
				WriteCallExpressionFor(origMethodToCall, MethodCallType.Virtual, poppedValues, _runtimeMetadataAccess.ConstrainedMethodMetadataFor(constrainedType, origMethodToCall));
			}
			else
			{
				WriteCallExpressionFor(origMethodToCall, MethodCallType.Virtual, poppedValues, _runtimeMetadataAccess.MethodMetadataFor(origMethodToCall));
			}
			return;
		}
		VTableMultipleGenericInterfaceImpls hasMultipleSharedInterfaces;
		MethodReference targetMethod = _vTableBuilder.GetVirtualMethodTargetMethodForConstrainedCallOnValueType(_context, thisType.ResolvedType, origMethodToCall.ResolvedMethodReference, out hasMultipleSharedInterfaces);
		if (targetMethod != null)
		{
			if (origMethodToCall.IsGenericInstance)
			{
				targetMethod = _typeResolver.Nested((GenericInstanceMethod)origMethodToCall.ResolvedMethodReference).Resolve(targetMethod, resolveGenericParameters: true);
			}
			_writer.AddIncludeForTypeDefinition(_context, targetMethod.DeclaringType);
			_writer.AddIncludeForTypeDefinition(_context, _typeResolver.ResolveReturnType(targetMethod));
		}
		if (targetMethod != null && thisType.IsSameType(targetMethod.DeclaringType) && hasMultipleSharedInterfaces == VTableMultipleGenericInterfaceImpls.None)
		{
			if (shouldUnbox)
			{
				if (boxedValueCanBeNull)
				{
					_nullCheckSupport.WriteNullCheckIfNeeded(_context, thisValue);
				}
				poppedValues[0] = new StackInfo(Unbox(thisType, thisValue), thisType.MakeByReferenceType(_context));
			}
			if (thisType.GetRuntimeStorage(_context).IsByValue() && isConstrainedGenericType)
			{
				ResolvedMethodInfo resolvedTargetMethod = _resolvedTypeFactory.CreateForConstrainedMethod(origMethodToCall, constrainedType, targetMethod);
				WriteCallExpressionFor(resolvedTargetMethod, MethodCallType.Normal, poppedValues, _runtimeMetadataAccess.ConstrainedMethodMetadataFor(constrainedType, origMethodToCall), emitNullCheckForInvocation: false);
				return;
			}
			if (_sharingType == SharingType.NonShared || (!thisType.UnresolvedType.ContainsGenericParameter && !origMethodToCall.UnresovledMethodReference.ContainsGenericParameter))
			{
				WriteCallExpressionFor(targetMethod, MethodCallType.Normal, poppedValues, _runtimeMetadataAccess.MethodMetadataFor(targetMethod), emitNullCheckForInvocation: false);
				return;
			}
		}
		if (hasMultipleSharedInterfaces.HasFlag(VTableMultipleGenericInterfaceImpls.HasDirectImplementation))
		{
			IMethodMetadataAccess methodInfo = _runtimeMetadataAccess.ConstrainedMethodMetadataFor(constrainedType, origMethodToCall);
			string methodInfoName = NewTempName();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteStatement($"const RuntimeMethod* {methodInfoName} = {methodInfo.MethodInfo()}");
			if (hasMultipleSharedInterfaces.HasFlag(VTableMultipleGenericInterfaceImpls.HasDefaultInterfaceImplementation))
			{
				string adjustedThis = $"({_context.Global.Services.Naming.ForVariable(poppedValues[0].Type)})il2cpp_codegen_runtime_box_constrained_this({_runtimeMetadataAccess.TypeInfoFor(thisType)}, {methodInfoName}, {poppedValues[0].Expression})";
				poppedValues[0] = new StackInfo(adjustedThis, poppedValues[0].Type);
			}
			WriteCallViaMethodPointer(ins, "il2cpp_codegen_get_direct_method_pointer(" + methodInfoName + ")", methodInfoName, poppedValues, thisType, FunctionPointerCallType.Managed, _resolvedTypeFactory.Create(targetMethod), "");
			return;
		}
		if (shouldUnbox)
		{
			poppedValues[0] = thisValue;
		}
		else if ((targetMethod != null && targetMethod.IsDefaultInterfaceMethod) || hasMultipleSharedInterfaces == VTableMultipleGenericInterfaceImpls.HasDefaultInterfaceImplementation)
		{
			StackInfo boxed = BoxThisForConstrainedCallIntoNewTemp(thisType, thisValue, constrainedType);
			poppedValues[0] = boxed;
		}
		else
		{
			string fakeBoxedVariable;
			if (thisType.IsNullableGenericInstance())
			{
				ReadOnlyCollection<FieldDefinition> fields = thisType.ResolvedType.Resolve().Fields;
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"if (!{thisValue.Expression}->{fields.Single((FieldDefinition f) => f.Name == "hasValue").CppName})");
				_writer.Indent();
				_writer.WriteLine("il2cpp_codegen_raise_null_reference_exception();");
				_writer.Dedent();
				fakeBoxedVariable = FakeBox(thisType.GetNullableUnderlyingType(), _runtimeMetadataAccess.TypeInfoFor(constrainedType.GetNullableUnderlyingType()), "&" + thisValue.Expression + "->" + fields.Single((FieldDefinition f) => f.Name == "value").CppName);
			}
			else
			{
				fakeBoxedVariable = FakeBox(thisType, _runtimeMetadataAccess.TypeInfoFor(constrainedType), thisValue.Expression);
			}
			poppedValues[0] = new StackInfo(Emit.AddressOf(fakeBoxedVariable), ObjectTypeReference);
			copyBackBoxedExpr = Emit.Assign(Emit.Dereference(thisValue.Expression), fakeBoxedVariable + ".m_Value");
		}
		if (targetMethod != null && targetMethod.DeclaringType.IsSpecialSystemBaseType())
		{
			MethodCallType callType = (_runtimeMetadataAccess.MustDoVirtualCallFor(thisType, targetMethod) ? MethodCallType.Virtual : MethodCallType.Normal);
			WriteCallExpressionFor(targetMethod, callType, poppedValues, _runtimeMetadataAccess.MethodMetadataFor(targetMethod), boxedValueCanBeNull);
			copyBackBoxedExpr = null;
		}
		else if (isConstrainedGenericType)
		{
			WriteCallExpressionFor(origMethodToCall, MethodCallType.Virtual, poppedValues, _runtimeMetadataAccess.ConstrainedMethodMetadataFor(constrainedType, origMethodToCall), boxedValueCanBeNull);
		}
		else
		{
			WriteCallExpressionFor(origMethodToCall, MethodCallType.Virtual, poppedValues, _runtimeMetadataAccess.MethodMetadataFor(origMethodToCall), boxedValueCanBeNull);
		}
	}

	private void WriteVariableSizedConstrainedCallExpressionFor(ResolvedMethodInfo methodToCall, List<StackInfo> poppedValues)
	{
		List<ResolvedTypeInfo> parameterTypes = methodToCall.Parameters.Select((ResolvedParameter p) => p.ParameterType).ToList();
		parameterTypes.Insert(0, _context.Global.Services.TypeProvider.Resolved.SystemVoidPointer);
		IMethodMetadataAccess methodMetadataAccess = _runtimeMetadataAccess.ConstrainedMethodMetadataFor(_constrainedCallThisType, methodToCall);
		List<string> args = FormatArgumentsForMethodCall(methodToCall, parameterTypes, poppedValues, callingViaInvoker: true);
		string bufferName = NewTempName();
		_runtimeMetadataAccess.AddInitializerStatement($"void* {bufferName} = alloca(Il2CppFakeBoxBuffer::SizeNeededFor({_runtimeMetadataAccess.TypeInfoFor(_constrainedCallThisType)}));");
		List<string> callArgs = new List<string>(args.Count + 4);
		callArgs.Add(_runtimeMetadataAccess.TypeInfoFor(_constrainedCallThisType));
		callArgs.Add(methodMetadataAccess.MethodInfo());
		callArgs.Add(bufferName);
		callArgs.AddRange(args);
		string returnVariable = null;
		bool returnAsByRef = false;
		if (methodToCall.ReturnType.IsNotVoid())
		{
			StackInfo returnVar = NewTemp(methodToCall.ReturnType);
			if (!returnVar.Type.GetRuntimeStorage(_context).IsVariableSized())
			{
				_writer.WriteStatement(returnVar.GetIdentifierExpression(_context));
			}
			_valueStack.Push(returnVar);
			returnVariable = returnVar.Expression;
			returnAsByRef = methodToCall.ReturnType.IsReturnedByRef(_context);
			if (returnAsByRef)
			{
				callArgs.Add("(" + _context.Global.Services.Naming.ForVariable(methodToCall.ReturnType) + "*)" + returnVariable);
			}
		}
		WriteCallAndOptionallyAssignReturnValue(_writer, returnVariable, returnAsByRef, Emit.Call(_context, _writer.VirtualCallInvokeMethod(methodToCall.ResolvedMethodReference, _typeResolver, VirtualMethodCallType.ConstrainedInvokerCall), callArgs));
	}

	private string FakeBox(ResolvedTypeInfo thisType, string typeInfoVariable, string pointerToValue)
	{
		string boxedVariable = NewTempName();
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"Il2CppFakeBox<{_context.Global.Services.Naming.ForVariable(thisType)}> {boxedVariable}({typeInfoVariable}, {pointerToValue});");
		return boxedVariable;
	}

	private StackInfo BoxThisForConstrainedCallIntoNewTemp(ResolvedTypeInfo thisType, StackInfo thisValue, ResolvedTypeInfo unresolvedConstrainedType)
	{
		StackInfo boxedValue = NewTemp(ObjectTypeReference);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{boxedValue.GetIdentifierExpression(_context)} = {_writer.WriteCall("Box", _runtimeMetadataAccess.TypeInfoFor(unresolvedConstrainedType, IRuntimeMetadataAccess.TypeInfoForReason.Box), thisValue.Expression)};");
		return new StackInfo(boxedValue.Expression, ObjectTypeReference, unresolvedConstrainedType);
	}

	private static Node NodeForTargetInstructionRecursive(Node node, Instruction ins)
	{
		if (node.Start == ins)
		{
			return node;
		}
		foreach (Node child in node.Children)
		{
			Node foundNode = NodeForTargetInstructionRecursive(child, ins);
			if (foundNode != null)
			{
				return foundNode;
			}
		}
		return null;
	}

	private Node NodeForLeaveTargetInstruction(Node currentNode, Instruction ins)
	{
		Node targetNode = NodeForTargetInstructionRecursive(_exceptionSupport.FlowTree, ins);
		if (targetNode.Type == NodeType.Try && !currentNode.IsChildOf(targetNode))
		{
			targetNode = targetNode.Parent;
		}
		return targetNode;
	}

	private void EmitCodeForLeaveFromTry(Node currentNode, ResolvedInstruction ins)
	{
		Instruction target = (Instruction)ins.Operand;
		_writer.WriteLine(_labeler.ForJump(target, NodeForLeaveTargetInstruction(currentNode, target)));
	}

	private void EmitCodeForLeaveFromCatch(Node currentNode, ResolvedInstruction ins)
	{
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{_exceptionSupport.EmitPopActiveException(SystemExceptionTypeReference)};");
		Instruction target = (Instruction)ins.Operand;
		_writer.WriteLine(_labeler.ForJump(target, NodeForLeaveTargetInstruction(currentNode, target)));
	}

	private void EmitCodeForLeaveFromFinallyOrFault()
	{
		_writer.WriteLine("return;");
	}

	private void EmitCodeForLeaveFromBlock(Node node, ResolvedInstruction ins)
	{
		if (node.IsInFinallyOrFaultBlock)
		{
			EmitCodeForLeaveFromFinallyOrFault();
			return;
		}
		if (node.IsInCatchBlock)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{_exceptionSupport.EmitPopActiveException(SystemExceptionTypeReference)};");
		}
		Instruction target = (Instruction)ins.Operand;
		_writer.WriteLine(_labeler.ForJump(target, NodeForLeaveTargetInstruction(node, target)));
	}

	private bool ShouldStripLeaveInstruction(InstructionBlock block, ResolvedInstruction ins)
	{
		if (!_labeler.NeedsLabel(ins.Instruction))
		{
			if (block.First == block.Last && block.First.Previous != null)
			{
				return block.First.Previous.OpCode.Code == Code.Leave;
			}
			return false;
		}
		return false;
	}

	private void PushExpression(ResolvedTypeInfo typeReference, string expression, ResolvedTypeInfo boxedType = null, ResolvedMethodInfo methodExpressionIsPointingTo = null)
	{
		_valueStack.Push(new StackInfo("(" + expression + ")", typeReference, boxedType, methodExpressionIsPointingTo));
	}

	private string EmitArrayLoadElementAddress(StackInfo array, string indexExpression)
	{
		_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
		string expression = Emit.LoadArrayElementAddress(array.Expression, indexExpression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
		if (_methodReference.ContainsFullySharedGenericTypes)
		{
			ResolvedTypeInfo elementType = array.Type.GetElementType();
			if (elementType.GetRuntimeStorage(_context).IsVariableSized())
			{
				return Emit.CastToByReferenceType(_context, elementType, expression);
			}
		}
		return expression;
	}

	private void LoadArgumentAddress(ResolvedParameter parameter)
	{
		string parameterName = ((!parameter.IsThisArg) ? parameter.CppName : "__this");
		ResolvedTypeInfo parameterType = parameter.ParameterType;
		ResolvedTypeInfo byRefType = parameterType.MakeByReferenceType(_context);
		string expression = ((parameterType.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny) ? Emit.CastToByReferenceType(_context, parameterType, VariableSizedAnyForArgLoad(parameter, parameterName)) : ((parameterType.GetRuntimeStorage(_context) != RuntimeStorageKind.VariableSizedValueType) ? Emit.AddressOf(parameterName) : Emit.CastToByReferenceType(_context, parameterType, parameterName)));
		_valueStack.Push(new StackInfo(expression, byRefType));
	}

	private void WriteLabelForBranchTarget(Instruction ins, Node currentNode)
	{
		if (_labeler.NeedsLabel(ins) && _emittedLabels.Add(_labeler.LabelIdForTarget(ins, currentNode)))
		{
			_writer.WriteLine();
			_writer.WriteUnindented(_labeler.ForLabel(ins, currentNode));
		}
	}

	private void WriteJump(Instruction targetInstruction, Node currentNode)
	{
		_writer.WriteLine(_labeler.ForJump(targetInstruction, currentNode));
	}

	private void LoadLocalAddress(ResolvedVariable variableReference)
	{
		ResolvedTypeInfo referenceType = variableReference.VariableType.MakeByReferenceType(_context);
		string variableName = _context.Global.Services.Naming.ForVariableName(variableReference);
		string expression = ((!variableReference.VariableType.GetRuntimeStorage(_context).IsVariableSized()) ? Emit.AddressOf(variableName) : Emit.CastToByReferenceType(_context, variableReference.VariableType, variableName));
		_valueStack.Push(new StackInfo(expression, referenceType));
	}

	private void WriteDup()
	{
		StackInfo top = _valueStack.Pop();
		StackInfo dup;
		if (top.Expression == "__this")
		{
			dup = new StackInfo("__this", top.Type);
		}
		else if (top.Expression == "NULL" && top.Type.IsSystemObject())
		{
			dup = new StackInfo("NULL", ObjectTypeReference);
		}
		else if (top.Type.GetRuntimeStorage(_context).IsVariableSized())
		{
			dup = NewTemp(top.Type);
			WriteAssignment(dup.Expression, top.Type, top);
		}
		else
		{
			dup = NewTemp(top.Type, top.BoxedType);
			WriteAssignment(dup.GetIdentifierExpression(_context), top.Type, top);
		}
		_valueStack.Push(new StackInfo(dup));
		_valueStack.Push(new StackInfo(dup));
	}

	private void WriteNotOperation()
	{
		StackInfo value = _valueStack.Pop();
		ResolvedTypeInfo resultType = StackTypeConverter.StackTypeFor(_context, value.Type);
		PushExpression(resultType, "(~" + CastTypeIfNeeded(value, resultType) + ")");
	}

	private void WriteNegateOperation()
	{
		StackInfo value = _valueStack.Pop();
		ResolvedTypeInfo stackType = StackTypeConverter.StackTypeFor(_context, value.Type);
		ResolvedTypeInfo resultType = StackAnalysisUtils.CalculateResultTypeForNegate(_context, stackType);
		PushExpression(resultType, "(-" + CastTypeIfNeeded(value, resultType) + ")");
	}

	private void LoadConstant(ResolvedTypeInfo type, string stringValue)
	{
		PushExpression(type, stringValue);
	}

	private void StoreLocalAndPush(ResolvedTypeInfo type, string stringValue)
	{
		StackInfo variable = NewTemp(type);
		if (type.GetRuntimeStorage(_context).IsVariableSized())
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_memcpy({variable.Expression}, {stringValue}, {_variableSizedTypeSupport.RuntimeSizeFor(_context, type)});");
		}
		else
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {stringValue};");
		}
		_valueStack.Push(new StackInfo(variable));
	}

	private void StoreLocalAndPush(ResolvedTypeInfo type, string stringValue, ResolvedTypeInfo boxedType)
	{
		StackInfo variable = NewTemp(type);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {stringValue};");
		_valueStack.Push(new StackInfo(variable.Expression, variable.Type, boxedType));
	}

	private void StoreLocalAndPush(ResolvedTypeInfo type, string stringValue, ResolvedMethodInfo methodExpressionIsPointingTo)
	{
		StackInfo variable = NewTemp(type);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {stringValue};");
		_valueStack.Push(new StackInfo(variable.Expression, variable.Type, null, methodExpressionIsPointingTo));
	}

	private void WriteCallExpressionFor(ResolvedMethodInfo methodToCall, MethodCallType callType, List<StackInfo> poppedValues, IMethodMetadataAccess methodMetadataAccess, bool emitNullCheckForInvocation = true)
	{
		List<ResolvedTypeInfo> parameterTypes = methodToCall.Parameters.Select((ResolvedParameter p) => p.ParameterType).ToList();
		if (methodToCall.HasThis)
		{
			parameterTypes.Insert(0, methodToCall.DeclaringType.GetRuntimeStorage(_context).IsByValue() ? methodToCall.DeclaringType.MakeByReferenceType(_context) : methodToCall.DeclaringType);
		}
		List<string> argsFor = FormatArgumentsForMethodCall(methodToCall, parameterTypes, poppedValues, methodMetadataAccess.DoCallViaInvoker(methodToCall.ResolvedMethodReference, callType));
		WriteCallExpressionFor(_methodReference, methodToCall, callType, argsFor, methodMetadataAccess, emitNullCheckForInvocation);
	}

	private void WriteCallExpressionFor(MethodReference callingMethod, ResolvedMethodInfo methodToCall, MethodCallType callType, List<string> argsFor, IMethodMetadataAccess methodMetadataAccess, bool emitNullCheckForInvocation = true)
	{
		if (emitNullCheckForInvocation)
		{
			_nullCheckSupport.WriteNullCheckForInvocationIfNeeded(methodToCall.ResolvedMethodReference, argsFor);
		}
		if (GenericSharingAnalysis.ShouldTryToCallStaticConstructorBeforeMethodCall(_context, methodMetadataAccess.IsConstrainedCall ? methodToCall.ResolvedMethodReference : methodToCall.UnresovledMethodReference, _methodDefinition))
		{
			WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(methodToCall.DeclaringType, methodMetadataAccess);
		}
		string returnVariable = "";
		if (!methodToCall.ReturnType.IsVoid())
		{
			StackInfo variableName = NewTemp(methodToCall.ReturnType);
			returnVariable = variableName.Expression;
			_valueStack.Push(new StackInfo(variableName.Expression, methodToCall.ReturnType));
			if (!methodToCall.ReturnType.GetRuntimeStorage(_context).IsVariableSized())
			{
				_writer.WriteStatement(variableName.GetIdentifierExpression(_context));
			}
		}
		if (!TryWriteIntrinsicMethodCall(returnVariable, callingMethod, methodToCall, argsFor))
		{
			WriteMethodCallExpression(methodToCall.ReturnType, returnVariable, _writer, methodToCall.ResolvedMethodReference, _typeResolver, callType, methodMetadataAccess, _vTableBuilder, argsFor);
		}
	}

	private void WriteCallExpressionFor(MethodReference unresolvedMethodToCall, MethodCallType callType, List<StackInfo> poppedValues, IMethodMetadataAccess methodRuntimeMetadataAccess, bool emitNullCheckForInvocation = true)
	{
		MethodReference methodToCall = _typeResolver.Resolve(unresolvedMethodToCall);
		TypeResolver typeResolverForMethodToCall = _typeResolver;
		if (methodToCall is GenericInstanceMethod genericInstanceMethod)
		{
			typeResolverForMethodToCall = _typeResolver.Nested(genericInstanceMethod);
		}
		List<TypeReference> parameterTypes = GetParameterTypes(methodToCall, typeResolverForMethodToCall);
		if (methodToCall.HasThis)
		{
			parameterTypes.Insert(0, methodToCall.DeclaringType.IsValueType ? _context.Assembly.Global.Services.TypeFactory.CreateByReferenceType(methodToCall.DeclaringType) : methodToCall.DeclaringType);
		}
		List<string> argsFor = FormatArgumentsForMethodCall(parameterTypes, poppedValues);
		WriteCallExpressionFor(_methodReference, unresolvedMethodToCall, callType, argsFor, methodRuntimeMetadataAccess, emitNullCheckForInvocation);
	}

	private void WriteCallExpressionFor(MethodReference callingMethod, MethodReference unresolvedMethodToCall, MethodCallType callType, List<string> argsFor, IMethodMetadataAccess methodRuntimeMetadataAccess, bool emitNullCheckForInvocation = true)
	{
		MethodReference methodToCall = _typeResolver.Resolve(unresolvedMethodToCall);
		TypeResolver typeResolverForMethodToCall = _typeResolver;
		if (emitNullCheckForInvocation)
		{
			_nullCheckSupport.WriteNullCheckForInvocationIfNeeded(methodToCall, argsFor);
		}
		if (GenericSharingAnalysis.ShouldTryToCallStaticConstructorBeforeMethodCall(_context, unresolvedMethodToCall, _methodDefinition))
		{
			WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(methodToCall.DeclaringType, unresolvedMethodToCall.DeclaringType, methodRuntimeMetadataAccess);
		}
		string returnVariable = "";
		if (!unresolvedMethodToCall.ReturnType.IsVoid)
		{
			ResolvedTypeInfo returnType = _resolvedTypeFactory.ResolveReturnType(unresolvedMethodToCall);
			StackInfo variableName = NewTemp(returnType);
			returnVariable = variableName.Expression;
			_valueStack.Push(new StackInfo(variableName.Expression, returnType));
			_writer.WriteStatement(variableName.GetIdentifierExpression(_context));
		}
		WriteMethodCallExpression(returnVariable, _writer, callingMethod, methodToCall, typeResolverForMethodToCall, callType, methodRuntimeMetadataAccess, _vTableBuilder, argsFor, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
	}

	private static void WriteCallAndAssignReturnValue(IGeneratedMethodCodeWriter writer, string returnVariable, string expression)
	{
		if (string.IsNullOrEmpty(returnVariable))
		{
			writer.WriteStatement(expression);
			return;
		}
		writer.WriteStatement($"{returnVariable} = {expression}");
	}

	private static void WriteCallAndOptionallyAssignReturnValue(IGeneratedMethodCodeWriter writer, string returnVariable, bool returnAsByRef, string expression)
	{
		if (string.IsNullOrEmpty(returnVariable) || returnAsByRef)
		{
			writer.WriteStatement(expression);
			return;
		}
		writer.WriteStatement($"{returnVariable} = {expression}");
	}

	private bool TryWriteIntrinsicMethodCall(string returnVariable, MethodReference callingMethod, ResolvedMethodInfo methodToCall, IReadOnlyList<string> argumentArray)
	{
		bool useArrayBoundsCheck = _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod();
		if (methodToCall.DeclaringType.IsArray && methodToCall.DeclaringType.GetElementType().GetRuntimeStorage(_context).IsVariableSized())
		{
			if (methodToCall.Name == "Set")
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy_with_write_barrier({GetArrayAddressCall(_context, argumentArray.First(), argumentArray.Skip(1).Take(argumentArray.Count - 2).AggregateWithComma(_context), useArrayBoundsCheck)}, {argumentArray.Last()}, {SizeOf(methodToCall.DeclaringType.GetElementType())}, {_runtimeMetadataAccess.TypeInfoFor(methodToCall.DeclaringType.GetElementType())});");
				return true;
			}
			if (methodToCall.Name == "Get")
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({returnVariable}, {GetArrayAddressCall(_context, argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(_context), useArrayBoundsCheck)}, {SizeOf(methodToCall.DeclaringType.GetElementType())});");
				return true;
			}
			if (methodToCall.Name == "Address")
			{
				WriteCallAndAssignReturnValue(_writer, returnVariable, Emit.Cast(_context, methodToCall.ReturnType, GetArrayAddressCall(_context, argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(_context), useArrayBoundsCheck)));
				return true;
			}
		}
		return TryWriteIntrinsicMethodCall(returnVariable, _writer, callingMethod, methodToCall.UnresovledMethodReference, methodToCall.ResolvedMethodReference, _runtimeMetadataAccess, argumentArray, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
	}

	private static bool TryWriteIntrinsicMethodCall(string returnVariable, IGeneratedMethodCodeWriter writer, MethodReference callingMethod, MethodReference unresolvedMethodToCall, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadataAccess, IReadOnlyList<string> argumentArray, bool useArrayBoundsCheck)
	{
		if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Set")
		{
			WriteCallAndAssignReturnValue(writer, returnVariable, GetArraySetCall(writer.Context, argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(writer.Context), useArrayBoundsCheck));
			return true;
		}
		if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Get")
		{
			WriteCallAndAssignReturnValue(writer, returnVariable, GetArrayGetCall(writer.Context, argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(writer.Context), useArrayBoundsCheck));
			return true;
		}
		if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Address")
		{
			WriteCallAndAssignReturnValue(writer, returnVariable, GetArrayAddressCall(writer.Context, argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(writer.Context), useArrayBoundsCheck));
			return true;
		}
		if (methodToCall.DeclaringType.IsSystemArray && methodToCall.Name == "GetGenericValueImpl")
		{
			WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(writer.Context, "GetGenericValueImpl", argumentArray));
			return true;
		}
		if (methodToCall.DeclaringType.IsSystemArray && methodToCall.Name == "SetGenericValueImpl")
		{
			WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(writer.Context, "SetGenericValueImpl", argumentArray));
			return true;
		}
		if (GenericsUtilities.IsGenericInstanceOfCompareExchange(methodToCall))
		{
			string genericInstanceTypeName = ((GenericInstanceMethod)methodToCall).GenericArguments[0].CppNameForVariable;
			WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(writer.Context, "InterlockedCompareExchangeImpl<" + genericInstanceTypeName + ">", argumentArray));
			return true;
		}
		if (GenericsUtilities.IsGenericInstanceOfExchange(methodToCall))
		{
			string genericInstanceTypeName2 = ((GenericInstanceMethod)methodToCall).GenericArguments[0].CppNameForVariable;
			WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(writer.Context, "InterlockedExchangeImpl<" + genericInstanceTypeName2 + ">", argumentArray));
			return true;
		}
		if (IntrinsicRemap.ShouldRemap(writer.Context, methodToCall, callingMethod?.ContainsFullySharedGenericTypes ?? false))
		{
			if (IntrinsicRemap.StillNeedsHiddenMethodInfo(writer.Context, methodToCall, callingMethod?.ContainsFullySharedGenericTypes ?? false))
			{
				List<string> list = argumentArray.ToList();
				list.Add(runtimeMetadataAccess.MethodInfo(unresolvedMethodToCall));
				argumentArray = list;
			}
			IntrinsicRemap.IntrinsicCall intrinsicCall = IntrinsicRemap.GetMappedCallFor(writer, methodToCall, callingMethod, runtimeMetadataAccess, argumentArray);
			WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(writer.Context, intrinsicCall.FunctionName, intrinsicCall.Arguments));
			return true;
		}
		return false;
	}

	internal static void WriteMethodCallExpression(string returnVariable, IGeneratedMethodCodeWriter writer, MethodReference callingMethod, MethodReference methodToCall, TypeResolver typeResolverForMethodToCall, MethodCallType callType, IMethodMetadataAccess runtimeMetadataAccess, IVTableBuilderService vTableBuilder, IReadOnlyList<string> argumentArray, bool useArrayBoundsCheck)
	{
		if (!TryWriteIntrinsicMethodCall(returnVariable, writer, callingMethod, methodToCall, methodToCall, runtimeMetadataAccess.RuntimeMetadataAccess, argumentArray, useArrayBoundsCheck))
		{
			if (callType == MethodCallType.Virtual)
			{
				writer.Context.Global.Collectors.IndirectCalls.Add(writer.Context, methodToCall, IndirectCallUsage.Virtual);
			}
			WriteMethodCallExpression(null, returnVariable, writer, methodToCall, typeResolverForMethodToCall, callType, runtimeMetadataAccess, vTableBuilder, argumentArray);
		}
	}

	private static void WriteMethodCallExpression(ResolvedTypeInfo returnType, string returnVariable, IGeneratedMethodCodeWriter writer, MethodReference methodToCall, TypeResolver typeResolverForMethodToCall, MethodCallType callType, IMethodMetadataAccess runtimeMetadataAccess, IVTableBuilderService vTableBuilder, IEnumerable<string> argumentArray)
	{
		List<string> args = new List<string>(argumentArray);
		if (methodToCall.IsUnmanagedCallersOnly)
		{
			writer.Write($"il2cpp_codegen_raise_execution_engine_exception({runtimeMetadataAccess.MethodInfo()})");
		}
		bool returnAsByRef = !string.IsNullOrEmpty(returnVariable) && returnType != null && returnType.IsReturnedByRef(writer.Context);
		if (returnAsByRef)
		{
			args.Add("(" + writer.Context.Global.Services.Naming.ForVariable(returnType) + "*)" + returnVariable);
		}
		if (callType == MethodCallType.Normal || MethodSignatureWriter.CanDevirtualizeMethodCall(methodToCall.Resolve()))
		{
			WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, returnAsByRef, DirectCallFor(writer, methodToCall, args, typeResolverForMethodToCall, runtimeMetadataAccess));
		}
		else
		{
			WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, returnAsByRef, VirtualCallFor(writer, methodToCall, args, typeResolverForMethodToCall, runtimeMetadataAccess, vTableBuilder));
		}
	}

	private static string DirectCallFor(IGeneratedMethodCodeWriter writer, MethodReference method, List<string> args, TypeResolver typeResolver, IMethodMetadataAccess runtimeMetadataAccess)
	{
		if (runtimeMetadataAccess.DoCallViaInvoker(method, MethodCallType.Normal))
		{
			if (method.IsStatic)
			{
				args.Insert(0, "NULL");
			}
			return InvokerCallFor(writer, method, runtimeMetadataAccess.MethodInfo(), args, typeResolver);
		}
		args.Add(runtimeMetadataAccess.HiddenMethodInfo());
		return Emit.Call(writer.Context, runtimeMetadataAccess.Method(method), args);
	}

	private static string VirtualCallFor(IGeneratedMethodCodeWriter writer, MethodReference method, IEnumerable<string> args, TypeResolver typeResolver, IMethodMetadataAccess runtimeMetadataAccess, IVTableBuilderService vTableBuilder)
	{
		bool isInterface = method.DeclaringType.Resolve().IsInterface;
		List<string> arguments = new List<string> { method.IsGenericInstance ? runtimeMetadataAccess.MethodInfo() : vTableBuilder.IndexForWithComment(writer.Context, method.Resolve(), method) };
		if (isInterface && !method.IsGenericInstance)
		{
			arguments.Add(runtimeMetadataAccess.TypeInfoForDeclaringType());
		}
		arguments.AddRange(args);
		return Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(method, typeResolver, runtimeMetadataAccess.DoCallViaInvoker(method, MethodCallType.Virtual)), arguments);
	}

	private static string InvokerCallFor(IGeneratedMethodCodeWriter writer, MethodReference method, string methodAccess, IEnumerable<string> args, TypeResolver typeResolver)
	{
		List<string> arguments = new List<string>();
		arguments.Add("il2cpp_codegen_get_direct_method_pointer(" + methodAccess + ")");
		arguments.Add(methodAccess);
		arguments.AddRange(args);
		return Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(method, typeResolver, VirtualMethodCallType.InvokerCall), arguments);
	}

	private static string GetArrayAddressCall(ReadOnlyContext context, string array, string arguments, bool useArrayBoundsCheck)
	{
		return Emit.Call(context, "(" + array + ")->" + ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck), arguments);
	}

	private static string GetArrayGetCall(ReadOnlyContext context, string array, string arguments, bool useArrayBoundsCheck)
	{
		return Emit.Call(context, "(" + array + ")->" + ArrayNaming.ForArrayItemGetter(useArrayBoundsCheck), arguments);
	}

	private static string GetArraySetCall(ReadOnlyContext context, string array, string arguments, bool useArrayBoundsCheck)
	{
		return Emit.Call(context, "(" + array + ")->" + ArrayNaming.ForArrayItemSetter(useArrayBoundsCheck), arguments);
	}

	private void WriteUnconditionalJumpTo(InstructionBlock block, Instruction target, Node currentNode)
	{
		if (block.Successors.Count != 1)
		{
			throw new ArgumentException("Expected only one successor for the current block", "target");
		}
		WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(block.Successors.Single()));
		WriteJump(target, currentNode);
	}

	private void WriteCastclassOrIsInst(ResolvedTypeInfo targetType, StackInfo value, string operation)
	{
		if (value.BoxedType != null && targetType.IsInterface() && !value.BoxedType.GetRuntimeStorage(_context).IsVariableSized() && !value.BoxedType.IsNullableGenericInstance())
		{
			for (TypeReference resolvedBoxedType = value.BoxedType.ResolvedType; resolvedBoxedType != null; resolvedBoxedType = resolvedBoxedType.GetBaseType(_context))
			{
				foreach (TypeReference @interface in resolvedBoxedType.GetInterfaces(_context))
				{
					if (@interface == targetType.ResolvedType)
					{
						_valueStack.Push(value);
						return;
					}
				}
			}
			if (operation == "IsInst")
			{
				LoadNull();
				return;
			}
		}
		ResolvedTypeInfo variableType = ((targetType.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType || targetType.GetRuntimeStorage(_context).IsVariableSized()) ? ObjectTypeReference : targetType);
		_writer.AddIncludeForTypeDefinition(targetType);
		string expression = Emit.Cast(_context, variableType, GetCastclassOrIsInstCall(targetType, value, operation, targetType));
		_valueStack.Push(new StackInfo("(" + expression + ")", variableType, value.BoxedType));
	}

	private string GetCastclassOrIsInstCall(ResolvedTypeInfo targetType, StackInfo value, string operation, ResolvedTypeInfo type)
	{
		return Emit.Call(_context, operation + GetOptimizedCastclassOrIsInstMethodSuffix(type), "(RuntimeObject*)" + value.Expression, _runtimeMetadataAccess.TypeInfoFor(targetType));
	}

	private static string GetOptimizedCastclassOrIsInstMethodSuffix(ResolvedTypeInfo resolvedTypeReference)
	{
		if (!resolvedTypeReference.IsUnknownSharedType() && (!resolvedTypeReference.IsDelegate() || !resolvedTypeReference.IsGenericInstance) && !resolvedTypeReference.IsInterface() && !resolvedTypeReference.IsArray && !resolvedTypeReference.IsNullableGenericInstance())
		{
			if (!resolvedTypeReference.IsSealed)
			{
				return "Class";
			}
			return "Sealed";
		}
		return string.Empty;
	}

	private MethodReference GetCreateStringMethod(ResolvedMethodInfo method)
	{
		if (method.DeclaringType.Name != "String")
		{
			throw new Exception("method.DeclaringType.Name != \"String\"");
		}
		foreach (MethodDefinition m in method.DeclaringType.ResolvedType.Resolve().Methods.Where((MethodDefinition meth) => meth.Name == "CreateString"))
		{
			if (m.Parameters.Count != method.Parameters.Count)
			{
				continue;
			}
			bool different = false;
			for (int i = 0; i < m.Parameters.Count; i++)
			{
				if (!method.Parameters[i].ParameterType.IsSameType(m.Parameters[i].ParameterType))
				{
					different = true;
				}
			}
			if (!different)
			{
				return m;
			}
		}
		throw new Exception("Can't find proper CreateString : " + method.FullName);
	}

	private void Unbox(ResolvedInstruction ins)
	{
		StackInfo boxedValue = _valueStack.Pop();
		_writer.AddIncludeForTypeDefinition(ins.TypeInfo);
		string unboxedExpression = Unbox(ins.TypeInfo, boxedValue);
		PushExpression(ins.TypeInfo.MakeByReferenceType(_context), unboxedExpression);
	}

	private string Unbox(ResolvedTypeInfo type, StackInfo boxedValue)
	{
		string boxedExpression = WriteExpressionAndCastIfNeeded(ObjectTypeReference, boxedValue);
		if (type.IsNullableGenericInstance())
		{
			string unboxedStorage = NewTempName();
			_runtimeMetadataAccess.AddInitializerStatement($"void* {unboxedStorage} = alloca({SizeOf(type)});");
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"UnBoxNullable({boxedExpression}, {_runtimeMetadataAccess.TypeInfoFor(type)}, {unboxedStorage});");
			return Emit.CastToPointer(_context, type, unboxedStorage);
		}
		string typeInfo = _runtimeMetadataAccess.TypeInfoFor(type);
		if (type.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			StackInfo unboxStorage = NewTemp(type);
			string unboxedPointer = NewTempName();
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"void* {unboxedPointer} = UnBox_Any({boxedExpression}, {typeInfo}, {unboxStorage.Expression});");
			return Emit.CastToPointer(_context, type, unboxedPointer ?? "");
		}
		return Emit.CastToPointer(_context, type, $"UnBox({boxedExpression}, {typeInfo})");
	}

	private string SizeOf(ResolvedTypeInfo type)
	{
		if (type.GetRuntimeStorage(_context).IsVariableSized())
		{
			return _variableSizedTypeSupport.RuntimeSizeFor(_context, type);
		}
		return "sizeof(" + _context.Global.Services.Naming.ForVariable(type) + ")";
	}

	private void WriteUnsignedArithmeticOperation(string op)
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeForBinaryOperation(_context, left.Type);
		ResolvedTypeInfo rightStackType = StackTypeConverter.StackTypeForBinaryOperation(_context, right.Type);
		ResolvedTypeInfo resultType = ((GetMetadataTypeOrderFor(_context, leftStackType) < GetMetadataTypeOrderFor(_context, rightStackType)) ? GetUnsignedType(rightStackType) : GetUnsignedType(leftStackType));
		WriteBinaryOperation(GetSignedType(resultType), $"({_context.Global.Services.Naming.ForVariable(resultType)})({_context.Global.Services.Naming.ForVariable(leftStackType)})", left.Expression, op, $"({_context.Global.Services.Naming.ForVariable(resultType)})({_context.Global.Services.Naming.ForVariable(rightStackType)})", right.Expression);
	}

	private ResolvedTypeInfo GetUnsignedType(ResolvedTypeInfo type)
	{
		if (type.IsSameType(SystemIntPtr) || type.IsSameType(SystemUIntPtr))
		{
			return SystemUIntPtr;
		}
		switch (type.MetadataType)
		{
		case MetadataType.SByte:
		case MetadataType.Byte:
			return ByteTypeReference;
		case MetadataType.Int16:
		case MetadataType.UInt16:
			return UInt16TypeReference;
		case MetadataType.Int32:
		case MetadataType.UInt32:
			return UInt32TypeReference;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return UInt64TypeReference;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return SystemUIntPtr;
		default:
			return type;
		}
	}

	private ResolvedTypeInfo GetSignedType(ResolvedTypeInfo type)
	{
		if (type.IsSameType(SystemIntPtr) || type.IsSameType(SystemUIntPtr))
		{
			return SystemIntPtr;
		}
		switch (type.MetadataType)
		{
		case MetadataType.SByte:
		case MetadataType.Byte:
			return SByteTypeReference;
		case MetadataType.Int16:
		case MetadataType.UInt16:
			return Int16TypeReference;
		case MetadataType.Int32:
		case MetadataType.UInt32:
			return Int32TypeReference;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return Int64TypeReference;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return SystemIntPtr;
		default:
			return type;
		}
	}

	private static int GetMetadataTypeOrderFor(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		return GetMetadataTypeOrderFor(type.ResolvedType);
	}

	private static int GetMetadataTypeOrderFor(TypeReference type)
	{
		if (type.IsSignedOrUnsignedIntPtr())
		{
			return 3;
		}
		switch (type.MetadataType)
		{
		case MetadataType.SByte:
		case MetadataType.Byte:
			return 0;
		case MetadataType.Int16:
		case MetadataType.UInt16:
			return 1;
		case MetadataType.Int32:
		case MetadataType.UInt32:
			return 2;
		case MetadataType.Pointer:
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return 3;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return 4;
		default:
			throw new Exception($"Invalid metadata type for typereference {type}");
		}
	}

	private void StoreField(ResolvedInstruction ins)
	{
		StackInfo value = _valueStack.Pop();
		StackInfo target = _valueStack.Pop();
		ResolvedFieldInfo fieldReference = ins.FieldInfo;
		if (target.Expression != "__this")
		{
			_nullCheckSupport.WriteNullCheckIfNeeded(_context, target);
		}
		EmitMemoryBarrierIfNecessary(fieldReference);
		if (fieldReference.Name == "m_value" && fieldReference.DeclaringType.IsSameType(IntPtrTypeReference))
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"*{target.Expression} = ({WriteExpressionAndCastIfNeeded(SystemIntPtr, value)});");
		}
		else if (fieldReference.Name == "_pointer" && fieldReference.DeclaringType.IsSameType(UIntPtrTypeReference))
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"*{target.Expression} = ({WriteExpressionAndCastIfNeeded(SystemUIntPtr, value)});");
		}
		else if (fieldReference.FieldType.GetRuntimeStorage(_context).IsVariableSized())
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_write_instance_field_data({target.Expression}, {_runtimeMetadataAccess.FieldInfo(fieldReference)}, {value.Expression}, {SizeOf(fieldReference.FieldType)});");
		}
		else if (fieldReference.DeclaringType.GetRuntimeFieldLayout(_context) == RuntimeFieldLayoutKind.Variable)
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"il2cpp_codegen_write_instance_field_data<{_context.Global.Services.Naming.ForVariable(fieldReference.FieldType)}>({target.Expression}, {_runtimeMetadataAccess.FieldInfo(fieldReference)}, {WriteExpressionAndCastIfNeeded(fieldReference.FieldType, value)});");
		}
		else
		{
			_writer.WriteFieldSetter(fieldReference, CastReferenceTypeOrNativeIntIfNeeded(target, fieldReference.DeclaringType) + "->" + _context.Global.Services.Naming.ForField(fieldReference), WriteExpressionAndCastIfNeeded(fieldReference.FieldType, value));
		}
	}

	private string CastReferenceTypeOrNativeIntIfNeeded(StackInfo originalValue, ResolvedTypeInfo toType)
	{
		if (toType.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			return originalValue.Expression;
		}
		if (toType.GetRuntimeStorage(_context) != RuntimeStorageKind.ValueType)
		{
			return CastTypeIfNeeded(originalValue, toType);
		}
		if (originalValue.Type.IsIntegralPointerType())
		{
			return CastTypeIfNeeded(originalValue, toType.MakeByReferenceType(_context));
		}
		if (originalValue.Type.IsPointer)
		{
			return CastTypeIfNeeded(originalValue, toType.MakePointerType(_context));
		}
		return originalValue.Expression;
	}

	private string CastTypeIfNeeded(StackInfo originalValue, ResolvedTypeInfo toType)
	{
		if (!originalValue.Type.IsSameType(toType))
		{
			return "(" + Emit.Cast(_context, toType, originalValue.Expression) + ")";
		}
		return originalValue.Expression;
	}

	private void EmitLoadToken(ResolvedInstruction ins)
	{
		object operand = ins.Operand;
		if (operand is TypeReference typeReference)
		{
			StoreLocalAndPush(RuntimeTypeHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.Il2CppTypeFor(typeReference) + ") }");
			_writer.AddIncludeForTypeDefinition(RuntimeTypeHandleTypeReference);
			return;
		}
		if (operand is FieldReference fieldReference)
		{
			StoreLocalAndPush(RuntimeFieldHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.FieldInfo(fieldReference) + ") }");
			_writer.AddIncludeForTypeDefinition(_context, RuntimeFieldHandleTypeReference.ResolvedType);
			return;
		}
		if (operand is MethodReference methodReference)
		{
			StoreLocalAndPush(RuntimeMethodHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.MethodInfo(methodReference) + ") }");
			_writer.AddIncludeForTypeDefinition(_context, RuntimeMethodHandleTypeReference.ResolvedType);
			_writer.AddIncludeForTypeDefinition(_context, _typeResolver.Resolve(methodReference.DeclaringType));
			return;
		}
		throw new ArgumentException();
	}

	private void LoadField(ResolvedInstruction ins, bool loadAddress = false)
	{
		StackInfo target = _valueStack.Pop();
		ResolvedFieldInfo fieldReference = ins.FieldInfo;
		ResolvedTypeInfo variableType = fieldReference.FieldType;
		string fieldGetterExpression;
		if (variableType.GetRuntimeStorage(_context).IsVariableSized() || fieldReference.DeclaringType.GetRuntimeFieldLayout(_context) == RuntimeFieldLayoutKind.Variable)
		{
			fieldGetterExpression = $"il2cpp_codegen_get_instance_field_data_pointer({target}, {_runtimeMetadataAccess.FieldInfo(fieldReference)})";
			if (loadAddress)
			{
				fieldGetterExpression = Emit.InParentheses(Emit.CastToByReferenceType(_context, variableType, fieldGetterExpression));
				PushExpression(variableType.MakeByReferenceType(_context), fieldGetterExpression);
			}
			else if (variableType.GetRuntimeStorage(_context).IsVariableSized())
			{
				StackInfo varLocal = NewTemp(variableType);
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({varLocal}, {fieldGetterExpression}, {SizeOf(variableType)});");
				_valueStack.Push(varLocal);
			}
			else
			{
				StackInfo varLocal2 = NewTemp(variableType);
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{varLocal2.GetIdentifierExpression(_context)} = {Emit.Dereference(Emit.CastToPointer(_context, variableType, fieldGetterExpression))};");
				_valueStack.Push(varLocal2);
			}
			EmitMemoryBarrierIfNecessary(fieldReference);
			return;
		}
		if (loadAddress)
		{
			variableType = variableType.MakeByReferenceType(_context);
		}
		else
		{
			if (fieldReference.Name == "m_value" && fieldReference.DeclaringType.IsSameType(IntPtrTypeReference))
			{
				StoreLocalAndPush(SystemIntPtr, (target.Type.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType) ? target.Expression : Emit.Dereference(target.Expression));
				return;
			}
			if (fieldReference.Name == "_pointer" && fieldReference.DeclaringType.IsSameType(UIntPtrTypeReference))
			{
				StoreLocalAndPush(SystemUIntPtr, (target.Type.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType) ? target.Expression : Emit.Dereference(target.Expression));
				return;
			}
		}
		if (target.Expression != "__this")
		{
			_nullCheckSupport.WriteNullCheckIfNeeded(_context, target);
		}
		StackInfo local = NewTemp(variableType);
		_valueStack.Push(new StackInfo(local));
		fieldGetterExpression = _context.Global.Services.Naming.ForField(fieldReference);
		string rhs = ((target.Type.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType && !target.Type.IsIntegralPointerType()) ? Emit.Dot(target.Expression, fieldGetterExpression) : Emit.Arrow(CastReferenceTypeOrNativeIntIfNeeded(target, fieldReference.DeclaringType), fieldGetterExpression));
		if (loadAddress)
		{
			WriteDeclarationAndAssignment(local, fieldReference.FieldType.MakePointerType(_context), Emit.AddressOf(rhs));
		}
		else
		{
			WriteDeclarationAndAssignment(local, fieldReference.FieldType, rhs);
		}
		EmitMemoryBarrierIfNecessary(fieldReference);
	}

	private void StaticFieldAccess(ResolvedInstruction ins)
	{
		ResolvedFieldInfo fieldReference = ins.FieldInfo;
		if (fieldReference.IsLiteral)
		{
			throw new Exception("literal values should always be embedded rather than accessed via the field itself");
		}
		WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(fieldReference.DeclaringType);
		ThrowIfAccessIsForbidden(ins, fieldReference);
		if (ins.OpCode.Code == Code.Stsfld)
		{
			StackInfo value = _valueStack.Pop();
			EmitMemoryBarrierIfNecessary();
			string staticAccessMethod = (fieldReference.IsThreadStatic ? "il2cpp_codegen_write_thread_static_field_data" : "il2cpp_codegen_write_static_field_data");
			if (fieldReference.FieldType.GetRuntimeStorage(_context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{staticAccessMethod}({_runtimeMetadataAccess.FieldInfo(fieldReference)}, {value.Expression}, {_variableSizedTypeSupport.RuntimeSizeFor(_context, fieldReference.FieldType)});");
			}
			else if (fieldReference.RuntimeLayoutForFieldAccess(_context) == RuntimeFieldLayoutKind.Variable)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{staticAccessMethod}<{_context.Global.Services.Naming.ForVariable(fieldReference.FieldType)}>({_runtimeMetadataAccess.FieldInfo(fieldReference)}, {value.Expression});");
			}
			else
			{
				_writer.WriteFieldSetter(fieldReference, TypeStaticsExpressionFor(_context, fieldReference, _runtimeMetadataAccess) + _context.Global.Services.Naming.ForField(fieldReference), WriteExpressionAndCastIfNeeded(fieldReference.FieldType, value));
			}
			return;
		}
		if (ins.OpCode.Code == Code.Ldsflda)
		{
			ResolvedTypeInfo variableType = fieldReference.FieldType.MakeByReferenceType(_context);
			if (fieldReference.FieldReference.FieldDef.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
			{
				PushExpression(variableType, Emit.CastToPointer(_context, fieldReference.FieldType, _runtimeMetadataAccess.FieldRvaData(fieldReference)));
			}
			else
			{
				string staticAccessMethod2 = (fieldReference.IsThreadStatic ? "il2cpp_codegen_get_thread_static_field_data_pointer" : "il2cpp_codegen_get_static_field_data_pointer");
				string fieldAddressExpression = ((!fieldReference.FieldType.GetRuntimeStorage(_context).IsVariableSized() && fieldReference.RuntimeLayoutForFieldAccess(_context) != RuntimeFieldLayoutKind.Variable) ? ("&" + TypeStaticsExpressionFor(_context, fieldReference, _runtimeMetadataAccess) + _context.Global.Services.Naming.ForField(fieldReference)) : Emit.CastToPointer(_context, fieldReference.FieldType, staticAccessMethod2 + "(" + _runtimeMetadataAccess.FieldInfo(fieldReference) + ")"));
				PushExpression(variableType, fieldAddressExpression);
			}
		}
		else
		{
			StackInfo local = NewTemp(fieldReference.FieldType);
			string staticAccessMethod3 = (fieldReference.IsThreadStatic ? "il2cpp_codegen_get_thread_static_field_data_pointer" : "il2cpp_codegen_get_static_field_data_pointer");
			if (fieldReference.FieldType.GetRuntimeStorage(_context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({local}, {staticAccessMethod3}({_runtimeMetadataAccess.FieldInfo(fieldReference)}), {_variableSizedTypeSupport.RuntimeSizeFor(_context, fieldReference.FieldType)});");
			}
			else if (fieldReference.RuntimeLayoutForFieldAccess(_context) == RuntimeFieldLayoutKind.Variable)
			{
				string fieldExpression = Emit.Dereference(Emit.CastToPointer(_context, fieldReference.FieldType, staticAccessMethod3 + "(" + _runtimeMetadataAccess.FieldInfo(fieldReference) + ")"));
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{local.GetIdentifierExpression(_context)} = {fieldExpression};");
			}
			else
			{
				WriteDeclarationAndAssignment(local, fieldReference.FieldType, TypeStaticsExpressionFor(_context, fieldReference, _runtimeMetadataAccess) + _context.Global.Services.Naming.ForField(fieldReference));
			}
			_valueStack.Push(new StackInfo(local));
		}
		EmitMemoryBarrierIfNecessary();
	}

	private void ThrowIfAccessIsForbidden(ResolvedInstruction ins, ResolvedFieldInfo fieldReference)
	{
	}

	private void WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(TypeReference resolvedType, TypeReference unresolvedType, IMethodMetadataAccess metadataAccess = null)
	{
		if (_context.Global.Parameters.NoLazyStaticConstructors || !resolvedType.HasStaticConstructor)
		{
			return;
		}
		if (metadataAccess != null && metadataAccess.IsConstrainedCall)
		{
			if (GenericSharingAnalysis.CouldBeASharedGenericInstanceType(_context, resolvedType))
			{
				_writer.WriteStatement(Emit.Call(_context, "il2cpp_codegen_runtime_class_init_inline", Emit.Call(_context, "il2cpp_codegen_method_get_declaring_type", metadataAccess.MethodInfo())));
				return;
			}
			unresolvedType = resolvedType;
		}
		if (_classesAlreadyInitializedInBlock.Add(unresolvedType) && !resolvedType.IsIntegralPointerType && resolvedType != _context.Global.Services.TypeProvider.GetSystemType(SystemType.BitConverter) && (!_methodDefinition.IsStaticConstructor || !_methodDefinition.DeclaringType.IsReferenceToThisTypeDefinition(unresolvedType)) && (!CompilerServicesSupport.HasEagerStaticClassConstructionEnabled(unresolvedType.Resolve()) || (_methodDefinition.IsStaticConstructor && CompilerServicesSupport.HasEagerStaticClassConstructionEnabled(_methodDefinition.DeclaringType))))
		{
			_writer.WriteStatement(Emit.Call(_context, "il2cpp_codegen_runtime_class_init_inline", _runtimeMetadataAccess.StaticData(unresolvedType)));
		}
	}

	private void WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(ResolvedTypeInfo type, IMethodMetadataAccess metadataAccess = null)
	{
		WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(type.ResolvedType, type.UnresolvedType, metadataAccess);
	}

	internal static string TypeStaticsExpressionFor(ReadOnlyContext context, FieldReference fieldReference, TypeResolver typeResolver, IRuntimeMetadataAccess runtimeMetadataAccess)
	{
		ResolvedTypeFactory resolvedTypeFactory = ResolvedTypeFactory.Create(context, typeResolver);
		return TypeStaticsExpressionFor(context, resolvedTypeFactory.Create(fieldReference), runtimeMetadataAccess);
	}

	private static string TypeStaticsExpressionFor(ReadOnlyContext context, ResolvedFieldInfo fieldReference, IRuntimeMetadataAccess runtimeMetadataAccess)
	{
		string typeInfo = runtimeMetadataAccess.StaticData(fieldReference.DeclaringType);
		if (!fieldReference.IsThreadStatic)
		{
			return $"(({context.Global.Services.Naming.ForStaticFieldsStruct(context, fieldReference.DeclaringType)}*)il2cpp_codegen_static_fields_for({typeInfo}))->";
		}
		return $"(({context.Global.Services.Naming.ForThreadFieldsStruct(context, fieldReference.DeclaringType)}*)il2cpp_codegen_get_thread_static_data({typeInfo}))->";
	}

	private void LoadIndirect(ResolvedTypeInfo valueType, ResolvedTypeInfo storageType)
	{
		StackInfo address = _valueStack.Pop();
		StackInfo variable = NewTemp(storageType);
		if (address.Type.IsSameType(valueType.MakePointerType(_context)))
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = (*({address.Expression}));");
		}
		else
		{
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {GetLoadIndirectExpression(valueType.MakePointerType(_context), address.Expression)};");
		}
		if (_thisInstructionIsVolatile)
		{
			EmitMemoryBarrierIfNecessary();
		}
		_valueStack.Push(new StackInfo(variable));
	}

	private void LoadIndirectReference()
	{
		StackInfo address = _valueStack.Pop();
		StoreLocalAndPush(GetPointerOrByRefType(address), GetLoadIndirectExpression(address.Type, address.Expression));
		EmitMemoryBarrierIfNecessary();
	}

	private string GetLoadIndirectExpression(ResolvedTypeInfo castType, string expression)
	{
		return $"*(({_context.Global.Services.Naming.ForVariable(castType)}){expression})";
	}

	private ResolvedTypeInfo GetPointerOrByRefType(StackInfo address)
	{
		if (address.Type.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr) || address.Type.IsSameType(_context.Global.Services.TypeProvider.SystemUIntPtr))
		{
			return _context.Global.Services.TypeProvider.Resolved.SystemVoid;
		}
		ResolvedTypeInfo typeReference = address.Type;
		if (typeReference.IsPointer)
		{
			return typeReference.GetElementType();
		}
		if (typeReference.IsByReference)
		{
			return typeReference.GetElementType();
		}
		throw new Exception();
	}

	private void LoadElemAndPop(ResolvedTypeInfo type)
	{
		StackInfo index = _valueStack.Pop();
		StackInfo array = _valueStack.Pop();
		LoadElem(array, type, index);
	}

	private void StoreArg(ResolvedInstruction ins)
	{
		StackInfo value = _valueStack.Pop();
		ResolvedParameter parameter = ins.ParameterInfo;
		if (parameter.Index == -1)
		{
			WriteAssignment("__this", parameter.ParameterType, value);
		}
		else if (parameter.ParameterType.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			WriteAssignment(VariableSizedAnyForArgLoad(parameter), parameter.ParameterType, value);
		}
		else
		{
			WriteAssignment(parameter.CppName, parameter.ParameterType, value);
		}
	}

	private void LoadElemRef(StackInfo array, StackInfo index)
	{
		ResolvedTypeInfo elementType = ((array.Expression == "NULL") ? array.Type.MakeArrayType(_context) : ArrayUtilities.ArrayElementTypeOf(array.Type));
		LoadElem(array, elementType, index);
	}

	private void LoadElem(StackInfo array, ResolvedTypeInfo objectType, StackInfo index)
	{
		if (array.Expression == "NULL")
		{
			ResolvedTypeInfo arrayType = objectType.MakeArrayType(_context);
			_writer.AddIncludeForTypeDefinition(arrayType);
			StackInfo local = NewTemp(arrayType);
			_writer.WriteStatement(Emit.Assign(local.GetIdentifierExpression(_context), array.Expression));
			array = local;
		}
		_nullCheckSupport.WriteNullCheckIfNeeded(_context, array);
		StackInfo variable = NewTemp(index.Type);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"{variable.GetIdentifierExpression(_context)} = {index.Expression};");
		_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
		string loadArrayElementExpression;
		if (objectType.GetRuntimeStorage(_context).IsVariableSized())
		{
			loadArrayElementExpression = Emit.LoadArrayElementAddress(array.Expression, variable.Expression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
		}
		else
		{
			loadArrayElementExpression = Emit.LoadArrayElement(array.Expression, variable.Expression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
			if (!array.Type.GetElementType().IsSameType(objectType))
			{
				loadArrayElementExpression = Emit.Cast(_context, objectType, loadArrayElementExpression);
			}
		}
		StoreLocalAndPush(objectType, loadArrayElementExpression);
	}

	private void StoreIndirect(ResolvedTypeInfo type)
	{
		StackInfo value = _valueStack.Pop();
		StackInfo address = _valueStack.Pop();
		EmitMemoryBarrierIfNecessary();
		string target = Emit.CastToPointer(_context, type, address.Expression);
		string newValue = Emit.Cast(_context, type, value.Expression);
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"*({target}) = {newValue};");
		_writer.WriteWriteBarrierIfNeeded(_runtimeMetadataAccess, type, target, newValue);
	}

	private void StoreElement(StackInfo array, StackInfo index, StackInfo value, bool emitElementTypeCheck)
	{
		_nullCheckSupport.WriteNullCheckIfNeeded(_context, array);
		if (!(array.Expression == "NULL"))
		{
			if (emitElementTypeCheck)
			{
				_writer.WriteLine(Emit.ArrayElementTypeCheck(array.Expression, value.Expression));
			}
			ResolvedTypeInfo elementType = ArrayUtilities.ArrayElementTypeOf(array.Type);
			_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
			if (elementType.GetRuntimeStorage(_context).IsVariableSized())
			{
				string access = Emit.LoadArrayElementAddress(array.Expression, index.Expression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({access}, {value.Expression}, {_variableSizedTypeSupport.RuntimeSizeFor(_context, elementType)});");
				_writer.WriteWriteBarrierIfNeeded(_runtimeMetadataAccess, elementType, access, value.Expression);
			}
			else
			{
				_writer.WriteStatement(Emit.StoreArrayElement(array.Expression, index.Expression, Emit.Cast(_context, elementType, value.Expression), _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod()));
			}
		}
	}

	private void LoadNull()
	{
		_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
	}

	private void WriteLdarg(ResolvedParameter parameter, InstructionBlock block, ResolvedInstruction ins)
	{
		if (parameter.IsThisArg)
		{
			_valueStack.Push(new StackInfo("__this", parameter.ParameterType));
			return;
		}
		StackInfo local = NewTemp(parameter.ParameterType);
		string loadExpression = parameter.CppName;
		if (parameter.ParameterType.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			loadExpression = VariableSizedAnyForArgLoad(parameter);
		}
		if (!CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block))
		{
			ResolvedInstruction next = ins.Next;
			if (next == null || next.OpCode.Code != Code.Ldobj || !CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next?.Next, block))
			{
				WriteDeclarationAndAssignment(local, local.Type, loadExpression);
			}
		}
		_valueStack.Push(new StackInfo(local));
	}

	private string VariableSizedAnyForArgLoad(ResolvedParameter parameter, string parameterName = null)
	{
		return Emit.VariableSizedAnyForArgLoad(_runtimeMetadataAccess, parameter.ParameterType.UnresolvedType, parameterName ?? parameter.CppName);
	}

	private string VariableSizedAnyForArgPassing(ResolvedTypeInfo type, string valueTypeExpression, string referenceTypeExpression)
	{
		return Emit.VariableSizedAnyForArgPassing(_runtimeMetadataAccess, type.UnresolvedType, valueTypeExpression, referenceTypeExpression);
	}

	private void WriteLdloc(ResolvedVariable variable, InstructionBlock block, ResolvedInstruction ins)
	{
		StackInfo local = NewTemp(variable.VariableType);
		_valueStack.Push(new StackInfo(local));
		if (!CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block))
		{
			WriteDeclarationAndAssignment(local, variable.VariableType, variable.VariableReference.CppName);
		}
	}

	private void WriteStloc(ResolvedVariable variableDefinition)
	{
		StackInfo value = _valueStack.Pop();
		WriteAssignment(_context.Global.Services.Naming.ForVariableName(variableDefinition), variableDefinition.VariableType, value);
	}

	private void GenerateConditional(string op, Signedness signedness, bool negate = false)
	{
		PushExpression(Int32TypeReference, ConditionalExpressionFor(op, signedness, negate) + "? 1 : 0");
	}

	private string ConditionalExpressionFor(string cppOperator, Signedness signedness, bool negate)
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		if (right.Expression == "0" && signedness == Signedness.Unsigned)
		{
			if (cppOperator == "<")
			{
				if (!negate)
				{
					return "false";
				}
				return "true";
			}
			if (cppOperator == ">=")
			{
				if (!negate)
				{
					return "true";
				}
				return "false";
			}
		}
		string leftCast = CastExpressionForOperandOfComparision(signedness, left);
		string rightCast = CastExpressionForOperandOfComparision(signedness, right);
		if (IsNonPointerReferenceType(right) && IsNonPointerReferenceType(left))
		{
			rightCast = PrependCastToObject(rightCast);
			leftCast = PrependCastToObject(leftCast);
		}
		string conditionalExpression = $"(({leftCast}{left.Expression}) {cppOperator} ({rightCast}{right.Expression}))";
		if (!negate)
		{
			return conditionalExpression;
		}
		return "(!" + conditionalExpression + ")";
	}

	private bool IsNonPointerReferenceType(StackInfo stackEntry)
	{
		if (stackEntry.Type.GetRuntimeStorage(_context) != RuntimeStorageKind.ValueType)
		{
			return !stackEntry.Type.IsPointer;
		}
		return false;
	}

	private string PrependCastToObject(string expression)
	{
		return "(" + _context.Global.Services.Naming.ForType(_context.Global.Services.TypeProvider.SystemObject) + "*)" + expression;
	}

	private string CastExpressionForOperandOfComparision(Signedness signedness, StackInfo left)
	{
		return "(" + _context.Global.Services.Naming.ForVariable(TypeForComparison(signedness, left.Type)) + ")";
	}

	private ResolvedTypeInfo TypeForComparison(Signedness signedness, ResolvedTypeInfo type)
	{
		ResolvedTypeInfo stackTypeFor = StackTypeConverter.StackTypeFor(_context, type);
		if (stackTypeFor.IsSameType(SystemIntPtr))
		{
			if (signedness != 0)
			{
				return SystemUIntPtr;
			}
			return SystemIntPtr;
		}
		switch (stackTypeFor.MetadataType)
		{
		case MetadataType.Int32:
			if (signedness != 0)
			{
				return UInt32TypeReference;
			}
			return Int32TypeReference;
		case MetadataType.Int64:
			if (signedness != 0)
			{
				return UInt64TypeReference;
			}
			return Int64TypeReference;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			if (signedness != 0)
			{
				return UIntPtrTypeReference;
			}
			return IntPtrTypeReference;
		case MetadataType.Pointer:
		case MetadataType.ByReference:
			if (signedness != 0)
			{
				return SystemUIntPtr;
			}
			return SystemIntPtr;
		default:
			return type;
		}
	}

	private void GenerateConditionalJump(InstructionBlock block, ResolvedInstruction ins, Node currentNode, bool isTrue)
	{
		StackInfo left = _valueStack.Pop();
		GenerateConditionalJump(block, ins, currentNode, (isTrue ? "" : "!") + left.Expression);
	}

	private void WriteGlobalVariableAssignmentForRightBranch(InstructionBlock block, Instruction targetInstruction)
	{
		GlobalVariable[] rightInputVariables = _stackAnalysis.InputVariablesFor(block.Successors.First((InstructionBlock b) => b.First.Offset == targetInstruction.Offset));
		WriteAssignGlobalVariables(rightInputVariables);
	}

	private void WriteGlobalVariableAssignmentForLeftBranch(InstructionBlock block, Instruction targetInstruction)
	{
		if (block.Successors.Count == 1)
		{
			WriteGlobalVariableAssignmentForRightBranch(block, targetInstruction);
			return;
		}
		GlobalVariable[] leftInputVariables = _stackAnalysis.InputVariablesFor(block.Successors.First((InstructionBlock b) => b.First.Offset != targetInstruction.Offset));
		WriteAssignGlobalVariables(leftInputVariables);
	}

	private void GenerateConditionalJump(InstructionBlock block, ResolvedInstruction ins, Node currentNode, string cppOperator, Signedness signedness, bool negate = false)
	{
		string conditionalExpression = ConditionalExpressionFor(cppOperator, signedness, negate);
		GenerateConditionalJump(block, ins, currentNode, conditionalExpression);
	}

	private void GenerateConditionalJump(InstructionBlock block, ResolvedInstruction ins, Node currentNode, string conditionalExpression)
	{
		Instruction targetInstruction = (Instruction)ins.Operand;
		using (NewIfBlock(conditionalExpression))
		{
			GenerateRightBranch(block, targetInstruction, currentNode);
		}
		GenerateLeftBranch(block, targetInstruction);
	}

	private void GenerateRightBranch(InstructionBlock block, Instruction targetInstruction, Node currentNode)
	{
		if (_valueStack.Count != 0)
		{
			WriteGlobalVariableAssignmentForRightBranch(block, targetInstruction);
		}
		WriteJump(targetInstruction, currentNode);
	}

	private void GenerateLeftBranch(InstructionBlock block, Instruction targetInstruction)
	{
		if (_valueStack.Count != 0)
		{
			WriteGlobalVariableAssignmentForLeftBranch(block, targetInstruction);
		}
	}

	private void WriteAssignGlobalVariables(GlobalVariable[] globalVariables)
	{
		if (globalVariables.Length != _valueStack.Count)
		{
			throw new ArgumentException("Invalid global variables count", "globalVariables");
		}
		int stackIndex = 0;
		foreach (StackInfo stackInfo in _valueStack)
		{
			GlobalVariable globalVariable = globalVariables.Single((GlobalVariable v) => v.Index == stackIndex);
			if (stackInfo.Type.GetRuntimeStorage(_context).IsVariableSized() || globalVariable.Type.GetRuntimeStorage(_context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp_codegen_memcpy({globalVariable.VariableName}, {stackInfo.Expression}, {SizeOf(globalVariable.Type)});");
			}
			else if (!stackInfo.Type.IsSameType(globalVariable.Type))
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{globalVariable.VariableName} = (({_context.Global.Services.Naming.ForVariable(globalVariable.Type)}){((stackInfo.Type.MetadataType == MetadataType.Pointer) ? "(intptr_t)" : "")}({stackInfo.Expression}));");
			}
			else if (globalVariable.Type.GetRuntimeFieldLayout(_context) == RuntimeFieldLayoutKind.Variable)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{globalVariable.VariableName} = (({_context.Global.Services.Naming.ForVariable(globalVariable.Type)}){stackInfo.Expression});");
			}
			else
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"{globalVariable.VariableName} = {stackInfo.Expression};");
			}
			int num = stackIndex + 1;
			stackIndex = num;
		}
	}

	private void WriteBinaryOperation(ResolvedTypeInfo destType, string lcast, string left, string op, string rcast, string right)
	{
		PushExpression(destType, $"({_context.Global.Services.Naming.ForVariable(destType)})({lcast}{left}{op}{rcast}{right})");
	}

	private void WriteRemainderOperation()
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		if (right.Type.MetadataType == MetadataType.Single || left.Type.MetadataType == MetadataType.Single)
		{
			PushExpression(SingleTypeReference, $"fmodf({left.Expression}, {right.Expression})");
		}
		else if (right.Type.MetadataType == MetadataType.Double || left.Type.MetadataType == MetadataType.Double)
		{
			PushExpression(DoubleTypeReference, $"fmod({left.Expression}, {right.Expression})");
		}
		else
		{
			WriteBinaryOperation("%", left, right, left.Type);
		}
	}

	private void WriteBinaryOperationUsingLargestOperandTypeAsResultType(string op)
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		WriteBinaryOperation(op, left, right, StackAnalysisUtils.CorrectLargestTypeFor(_context, left.Type, right.Type));
	}

	private void WriteBinaryOperationUsingLeftOperandTypeAsResultType(string op)
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		WriteBinaryOperation(op, left, right, left.Type);
	}

	private void WriteBinaryOperation(string op, StackInfo left, StackInfo right, ResolvedTypeInfo resultType)
	{
		string rcast = CastExpressionForBinaryOperator(right);
		string lcast = CastExpressionForBinaryOperator(left);
		if (!resultType.IsPointer)
		{
			try
			{
				resultType = StackTypeConverter.StackTypeFor(_context, resultType);
			}
			catch (ArgumentException)
			{
			}
		}
		WriteBinaryOperation(resultType, lcast, left.Expression, op, rcast, right.Expression);
	}

	private string CastExpressionForBinaryOperator(StackInfo right)
	{
		try
		{
			return StackTypeConverter.CppStackTypeFor(_context, right.Type);
		}
		catch (ArgumentException)
		{
			return "";
		}
	}

	private void WriteShrUn()
	{
		StackInfo right = _valueStack.Pop();
		StackInfo left = _valueStack.Pop();
		string lcast = "";
		ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeFor(_context, left.Type);
		if (leftStackType.MetadataType == MetadataType.Int32)
		{
			lcast = "(uint32_t)";
		}
		if (leftStackType.MetadataType == MetadataType.Int64)
		{
			lcast = "(uint64_t)";
		}
		if (leftStackType.MetadataType == MetadataType.IntPtr)
		{
			lcast = "(uintptr_t)";
		}
		WriteBinaryOperation(leftStackType, lcast, left.Expression, ">>", "", right.Expression);
	}

	private string NewTempName()
	{
		return "L_" + _tempIndex++;
	}

	private StackInfo NewTemp(ResolvedTypeInfo type, ResolvedTypeInfo boxedType = null)
	{
		string variableName = NewTempName();
		StackInfo local = new StackInfo(variableName, type, boxedType);
		if (type.GetRuntimeStorage(_context).IsVariableSized())
		{
			_variableSizedTypeSupport.TrackLocal(_context, local);
		}
		return local;
	}

	private void LoadPrimitiveTypeSByte(ResolvedInstruction ins, ResolvedTypeInfo type)
	{
		PushExpression(type, Emit.Cast(_context, type, ((sbyte)ins.Operand).ToString()));
	}

	private void LoadPrimitiveTypeInt32(ResolvedInstruction ins, ResolvedTypeInfo type)
	{
		int value = (int)ins.Operand;
		string valueAsString = value.ToString();
		long longValue = value;
		if (longValue <= int.MinValue || longValue >= int.MaxValue)
		{
			valueAsString += "LL";
		}
		PushExpression(type, Emit.Cast(_context, type, valueAsString));
	}

	private void LoadLong(ResolvedInstruction ins, ResolvedTypeInfo type)
	{
		long longValue = (long)ins.Operand;
		string valueAsString = longValue + "LL";
		if (longValue == long.MinValue)
		{
			valueAsString = "(std::numeric_limits<int64_t>::min)()";
		}
		if (longValue == long.MaxValue)
		{
			valueAsString = "(std::numeric_limits<int64_t>::max)()";
		}
		PushExpression(type, Emit.Cast(_context, type, valueAsString));
	}

	private void LoadInt32Constant(int value)
	{
		_valueStack.Push((value < 0) ? new StackInfo($"({value})", Int32TypeReference) : new StackInfo(value.ToString(), Int32TypeReference));
	}

	private List<string> FormatArgumentsForMethodCall(ResolvedMethodInfo methodToCall, List<ResolvedTypeInfo> parameterTypes, List<StackInfo> stackValues, bool callingViaInvoker)
	{
		int paramCount = parameterTypes.Count;
		List<string> argList = new List<string>(paramCount);
		for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
		{
			StackInfo stackValue = stackValues[paramIndex];
			ResolvedTypeInfo paramType = parameterTypes[paramIndex];
			string argumentExpression = stackValue.Expression;
			if (paramType.IsPointer || paramType.IsByReference)
			{
				if (!stackValue.Type.IsSameTypeInCodegen(paramType))
				{
					argumentExpression = "(" + _context.Global.Services.Naming.ForVariable(paramType) + ")" + stackValue.Expression;
				}
			}
			else if (stackValue.Type.GetRuntimeStorage(_context).IsVariableSized())
			{
				if (methodToCall == null || !ArrayNaming.IsSpecialArrayMethod(methodToCall.ResolvedMethodReference))
				{
					string valueTypeExpression = stackValue.Expression;
					if (!callingViaInvoker)
					{
						StackInfo local = NewTemp(stackValue.Type);
						valueTypeExpression = $"il2cpp_codegen_memcpy({local.Expression}, {stackValue.Expression}, {SizeOf(stackValue.Type)})";
					}
					argumentExpression = ((stackValue.Type.GetRuntimeStorage(_context) != RuntimeStorageKind.VariableSizedValueType) ? VariableSizedAnyForArgPassing(stackValue.Type, valueTypeExpression, stackValue.Expression) : valueTypeExpression);
				}
			}
			else
			{
				argumentExpression = WriteExpressionAndCastIfNeeded(paramType, stackValue);
			}
			argList.Add(argumentExpression);
		}
		return argList;
	}

	private List<string> FormatArgumentsForMethodCall(List<TypeReference> parameterTypes, List<StackInfo> stackValues)
	{
		int paramCount = parameterTypes.Count;
		List<string> argList = new List<string>(paramCount);
		for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
		{
			StackInfo stackValue = stackValues[paramIndex];
			TypeReference paramType = parameterTypes[paramIndex];
			string argumentExpression = stackValue.Expression;
			if (paramType.IsPointer || paramType.IsByReference)
			{
				if (!stackValue.Type.IsSameTypeInCodegen(paramType))
				{
					argumentExpression = "(" + paramType.CppNameForVariable + ")" + stackValue.Expression;
				}
			}
			else
			{
				argumentExpression = WriteExpressionAndCastIfNeeded(paramType, stackValue);
			}
			argList.Add(argumentExpression);
		}
		return argList;
	}

	private List<TypeReference> GetParameterTypes(MethodReference method, TypeResolver typeResolverForMethodToCall)
	{
		return new List<TypeReference>(method.Parameters.Select((ParameterDefinition parameter) => typeResolverForMethodToCall.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(_context.Global.Services.TypeFactory, method, parameter))));
	}

	private static List<StackInfo> PopItemsFromStack(int amount, Stack<StackInfo> valueStack)
	{
		if (amount > valueStack.Count)
		{
			throw new Exception($"Attempting to pop '{amount}' values from a stack of depth '{valueStack.Count}'.");
		}
		List<StackInfo> poppedValues = new List<StackInfo>();
		for (int i = 0; i != amount; i++)
		{
			poppedValues.Add(valueStack.Pop());
		}
		poppedValues.Reverse();
		return poppedValues;
	}

	private bool ShouldEmitReversePInvokeWrapper(MethodReference targetMethod)
	{
		if (ReversePInvokeMethodBodyWriter.IsReversePInvokeWrapperNecessary(_context, targetMethod))
		{
			return true;
		}
		if (_context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers && !targetMethod.DeclaringType.ContainsGenericParameter)
		{
			return !targetMethod.ContainsGenericParameter;
		}
		return false;
	}

	private IDisposable NewIfBlock(string conditional)
	{
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"if ({conditional})");
		return NewBlock();
	}

	private IDisposable NewElseBlock()
	{
		_writer.WriteLine("else");
		return NewBlock();
	}

	private IDisposable NewBlock()
	{
		return new BlockWriter(_writer);
	}

	private void EmitMemoryBarrierIfNecessary(ResolvedFieldInfo fieldInfo)
	{
		EmitMemoryBarrierIfNecessary(fieldInfo.FieldReference);
	}

	private void EmitMemoryBarrierIfNecessary(FieldReference fieldReference = null)
	{
		if (_thisInstructionIsVolatile || (fieldReference != null && fieldReference.IsVolatile))
		{
			_context.Global.Collectors.Stats.RecordMemoryBarrierEmitted(_methodDefinition);
			_writer.WriteStatement(Emit.MemoryBarrier());
			_thisInstructionIsVolatile = false;
		}
	}

	private void AddVolatileStackEntry()
	{
		_thisInstructionIsVolatile = true;
	}

	private void WriteLoadObject(ResolvedInstruction ins, InstructionBlock block)
	{
		StackInfo address = _valueStack.Pop();
		ResolvedTypeInfo targetType = ins.TypeInfo;
		if (CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block))
		{
			_valueStack.Push(new StackInfo($"(*({_context.Global.Services.Naming.ForPointerToVariable(targetType)}){address.Expression})", targetType));
		}
		else
		{
			_writer.AddIncludeForTypeDefinition(targetType);
			StackInfo temp = NewTemp(targetType);
			if (targetType.GetRuntimeStorage(_context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteStatement($"il2cpp_codegen_memcpy({temp.Expression}, {address.Expression}, {_variableSizedTypeSupport.RuntimeSizeFor(_context, targetType)})");
			}
			else
			{
				IGeneratedMethodCodeWriter writer = _writer;
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(0, 1, writer);
				left.AppendFormatted(temp.GetIdentifierExpression(_context));
				generatedMethodCodeWriter.WriteAssignStatement(ref left, $"(*({_context.Global.Services.Naming.ForPointerToVariable(targetType)}){address.Expression})");
			}
			_valueStack.Push(temp);
		}
		EmitMemoryBarrierIfNecessary();
	}

	private void WriteStoreObject(ResolvedTypeInfo type)
	{
		StackInfo value = _valueStack.Pop();
		StackInfo address = _valueStack.Pop();
		EmitMemoryBarrierIfNecessary();
		string addressExpression = Emit.Cast(_context, type.MakePointerType(_context), address.Expression);
		string leftExpression = ((!type.GetRuntimeStorage(_context).IsVariableSized()) ? Emit.Dereference(addressExpression) : addressExpression);
		WriteAssignment(leftExpression, type, value);
		_writer.WriteWriteBarrierIfNeeded(_runtimeMetadataAccess, type, addressExpression, value.Expression);
	}

	private void LoadVirtualFunction(ResolvedInstruction ins)
	{
		ResolvedMethodInfo methodReference = ins.MethodInfo;
		StackInfo target = _valueStack.Pop();
		if (methodReference.IsVirtual)
		{
			PushCallToLoadVirtualFunction(methodReference, target.Expression);
		}
		else
		{
			PushCallToLoadFunction(methodReference);
		}
	}

	private void PushCallToLoadFunction(ResolvedMethodInfo method)
	{
		if (method.IsUnmanagedCallersOnly)
		{
			UnmanagedCallersOnlyUtils.WriteCallToRaiseInvalidCallingConvsIfNeeded(_writer, _runtimeMetadataAccess, method);
			PushExpression(SystemVoidPointer, Emit.Cast(_context, SystemVoidPointer, _runtimeMetadataAccess.Method(method)), null, method);
		}
		else
		{
			_writer.AddIncludeForTypeDefinition(method.DeclaringType);
			PushExpression(SystemVoidPointer, Emit.Cast(_context, SystemVoidPointer, _runtimeMetadataAccess.MethodInfo(method)), null, method);
		}
	}

	private void PushCallToLoadVirtualFunction(ResolvedMethodInfo method, string targetExpression)
	{
		bool isInterfaceMethod = method.DeclaringType.IsInterface();
		string methodInfo = (method.IsGenericInstance ? (isInterfaceMethod ? Emit.Call(_context, "il2cpp_codegen_get_generic_interface_method", _runtimeMetadataAccess.MethodInfo(method), targetExpression) : Emit.Call(_context, "il2cpp_codegen_get_generic_virtual_method", _runtimeMetadataAccess.MethodInfo(method), targetExpression)) : ((!isInterfaceMethod) ? Emit.Call(_context, "GetVirtualMethodInfo", targetExpression, _vTableBuilder.IndexFor(_context, method.UnresovledMethodReference.Resolve()).ToString()) : Emit.Call(_context, "GetInterfaceMethodInfo", targetExpression, _vTableBuilder.IndexFor(_context, method.UnresovledMethodReference.Resolve()).ToString(), _runtimeMetadataAccess.TypeInfoFor(method.DeclaringType))));
		_writer.AddIncludeForTypeDefinition(method.DeclaringType);
		PushExpression(SystemVoidPointer, Emit.Cast(_context, SystemVoidPointer, methodInfo), null, method);
	}
}
