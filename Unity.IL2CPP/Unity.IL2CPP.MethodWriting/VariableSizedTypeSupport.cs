using System;
using System.Collections.Generic;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.MethodWriting;

public class VariableSizedTypeSupport
{
	private readonly IRuntimeMetadataAccess _runtimeMetadataAccess;

	private readonly HashSet<ResolvedTypeInfo> _runtimeSizes = new HashSet<ResolvedTypeInfo>(ResolvedTypeEqualityComparer.Instance);

	private readonly Dictionary<ResolvedTypeInfo, List<List<string>>> _stackSlots = new Dictionary<ResolvedTypeInfo, List<List<string>>>(ResolvedTypeEqualityComparer.Instance);

	private readonly Stack<Dictionary<ResolvedTypeInfo, int>> _nestedBlockUsedSlots = new Stack<Dictionary<ResolvedTypeInfo, int>>();

	private Dictionary<ResolvedTypeInfo, int> _currentUsedSlots;

	public VariableSizedTypeSupport(IRuntimeMetadataAccess runtimeMetadataAccess)
	{
		_runtimeMetadataAccess = runtimeMetadataAccess;
	}

	public void EnterBlock()
	{
		if (_currentUsedSlots != null)
		{
			_nestedBlockUsedSlots.Push(_currentUsedSlots);
			_currentUsedSlots = new Dictionary<ResolvedTypeInfo, int>(_currentUsedSlots, ResolvedTypeEqualityComparer.Instance);
		}
		else
		{
			_currentUsedSlots = new Dictionary<ResolvedTypeInfo, int>(ResolvedTypeEqualityComparer.Instance);
		}
	}

	public void LeaveBlock()
	{
		_currentUsedSlots = ((_nestedBlockUsedSlots.Count > 0) ? _nestedBlockUsedSlots.Pop() : null);
	}

	public string RuntimeSizeFor(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		if (!type.GetRuntimeStorage(context).IsVariableSized())
		{
			throw new NotSupportedException(type.FullName + " is not a variable sized type");
		}
		string nameForSize = "SizeOf_" + type.UnresolvedType.CppName;
		if (_runtimeSizes.Add(type))
		{
			if (context.Global.Parameters.EmitComments)
			{
				_runtimeMetadataAccess.AddInitializerStatement("// sizeof(" + type.FullName + ")");
			}
			_runtimeMetadataAccess.AddInitializerStatement($"const uint32_t {nameForSize} = il2cpp_codegen_sizeof({_runtimeMetadataAccess.TypeInfoFor(type, IRuntimeMetadataAccess.TypeInfoForReason.Size)});");
		}
		return nameForSize;
	}

	public void TrackLocal(ReadOnlyContext context, StackInfo local)
	{
		if (!local.Type.GetRuntimeStorage(context).IsVariableSized())
		{
			throw new NotSupportedException(local.Type.FullName + " is not a variable sized type");
		}
		int currentSlot = (_currentUsedSlots.TryGetValue(local.Type, out currentSlot) ? (currentSlot + 1) : 0);
		_currentUsedSlots[local.Type] = currentSlot;
		if (!_stackSlots.TryGetValue(local.Type, out var slotList))
		{
			slotList = new List<List<string>>();
			_stackSlots.Add(local.Type, slotList);
		}
		if (slotList.Count > currentSlot)
		{
			slotList[currentSlot].Add(local.Expression);
			return;
		}
		slotList.Add(new List<string> { local.Expression });
	}

	public void GenerateInitializerStatements(ReadOnlyContext context)
	{
		foreach (ResolvedTypeInfo type in _stackSlots.Keys.ToSortedCollectionByUnresolvedType())
		{
			if (context.Global.Parameters.EmitComments)
			{
				_runtimeMetadataAccess.AddInitializerStatement("// " + type.FullName);
			}
			foreach (List<string> slotList in _stackSlots[type])
			{
				string firstVarName = slotList[0];
				string typeName = context.Global.Services.Naming.ForVariable(type);
				_runtimeMetadataAccess.AddInitializerStatement($"const {typeName} {firstVarName} = alloca({RuntimeSizeFor(context, type)});");
				for (int i = 1; i < slotList.Count; i++)
				{
					_runtimeMetadataAccess.AddInitializerStatement($"const {typeName} {slotList[i]} = {firstVarName};");
				}
			}
		}
	}
}
