using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Contexts.Components;

public class VTableBuilderComponent : StatefulComponentBase<IVTableBuilderService, object, VTableBuilderComponent>, IVTableBuilderService
{
	private class NotAvailable : IVTableBuilderService
	{
		public int IndexFor(ReadOnlyContext context, MethodDefinition method)
		{
			throw new NotSupportedException();
		}

		public VTable VTableFor(ReadOnlyContext context, TypeReference typeReference)
		{
			throw new NotSupportedException();
		}

		public MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method, out VTableMultipleGenericInterfaceImpls multipleGenericInterfaceImpls)
		{
			throw new NotSupportedException();
		}
	}

	private readonly Dictionary<MethodReference, int> _methodSlots;

	private readonly Dictionary<TypeReference, VTable> _vtables;

	private readonly List<(MethodReference, int)> _newItemsAddedToMethodSlots = new List<(MethodReference, int)>();

	private readonly List<(TypeReference, VTable)> _newItemsAddedToVTables = new List<(TypeReference, VTable)>();

	private readonly bool _trackNewItemsForMerging;

	public const int InvalidMethodSlot = 65535;

	public VTableBuilderComponent()
	{
		_methodSlots = new Dictionary<MethodReference, int>();
		_vtables = new Dictionary<TypeReference, VTable>();
		_trackNewItemsForMerging = false;
	}

	private VTableBuilderComponent(Dictionary<MethodReference, int> methodSlots, Dictionary<TypeReference, VTable> vtables)
	{
		_methodSlots = new Dictionary<MethodReference, int>(methodSlots);
		_vtables = new Dictionary<TypeReference, VTable>(vtables);
		_trackNewItemsForMerging = true;
	}

	public bool ForTestingOnlyIsCached(TypeReference type)
	{
		return _vtables.ContainsKey(type);
	}

	public int IndexFor(ReadOnlyContext context, MethodDefinition method)
	{
		if (!method.IsVirtual)
		{
			return 65535;
		}
		if (method.DeclaringType.IsInterface)
		{
			SetupMethodSlotsForInterface(context, method.DeclaringType);
			return GetSlot(method);
		}
		VTableFor(context, method.DeclaringType);
		return _methodSlots[method];
	}

	private int GetSlot(MethodReference method)
	{
		return _methodSlots[method];
	}

	private void SetSlot(MethodReference method, int slot)
	{
		_methodSlots[method] = slot;
		if (_trackNewItemsForMerging)
		{
			_newItemsAddedToMethodSlots.Add((method, slot));
		}
	}

	public VTable VTableFor(ReadOnlyContext context, TypeReference typeReference)
	{
		if (_vtables.TryGetValue(typeReference, out var vtable))
		{
			return vtable;
		}
		if (typeReference.IsArray)
		{
			throw new InvalidOperationException("Calculating vtable for arrays is not supported.");
		}
		TypeDefinition typeDefinition = typeReference.Resolve();
		if (typeDefinition.IsInterface && !typeDefinition.IsComOrWindowsRuntimeInterface(context) && context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition) == null)
		{
			throw new InvalidOperationException("Calculating vtable for non-COM interface is not supported.");
		}
		int currentSlot = ((typeDefinition.BaseType != null) ? VTableFor(context, typeReference.GetBaseType(context)).Slots.Count : 0);
		Dictionary<TypeReference, int> interfaceOffsets = SetupInterfaceOffsets(context, typeReference, ref currentSlot);
		if (!(typeReference is GenericInstanceType genericInstanceType))
		{
			return VTableForType(context, typeDefinition, interfaceOffsets, currentSlot);
		}
		return VTableForGenericInstance(context, genericInstanceType, interfaceOffsets);
	}

	private static int VirtualMethodCount(TypeReference type)
	{
		return type.Resolve().Methods.Count((MethodDefinition m) => m.IsVirtual && !m.IsStripped);
	}

	private Dictionary<TypeReference, int> SetupInterfaceOffsets(ReadOnlyContext context, TypeReference type, ref int currentSlot)
	{
		Dictionary<TypeReference, int> offsets = new Dictionary<TypeReference, int>();
		if (type.IsInterface)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.Module == context.Global.Services.TypeProvider.Corlib.MainModule && typeDefinition.Namespace == "System.Collections.Generic" && typeDefinition.Name == "IEnumerable`1")
			{
				foreach (InterfaceImplementation @interface in typeDefinition.Interfaces)
				{
					TypeReference interfaceType = @interface.InterfaceType;
					if (interfaceType.Namespace == "System.Collections" && interfaceType.Name == "IEnumerable")
					{
						int ienumerableMethodCount = VirtualMethodCount(type);
						offsets.Add(interfaceType, ienumerableMethodCount);
						currentSlot = ienumerableMethodCount + 1;
						break;
					}
				}
			}
			return offsets;
		}
		for (TypeReference t = type.GetBaseType(context); t != null; t = t.GetBaseType(context))
		{
			VTable vt = VTableFor(context, t);
			foreach (TypeReference itf in t.GetInterfaces(context))
			{
				SetupMethodSlotsForInterface(context, itf);
				int offset = vt.InterfaceOffsets[itf];
				offsets[itf] = offset;
			}
		}
		foreach (TypeReference itf2 in type.GetInterfaces(context))
		{
			if (!offsets.ContainsKey(itf2))
			{
				SetupMethodSlotsForInterface(context, itf2);
				offsets.Add(itf2, currentSlot);
				currentSlot += VirtualMethodCount(itf2);
			}
		}
		return offsets;
	}

	private void SetupMethodSlotsForInterface(ReadOnlyContext context, TypeReference typeReference)
	{
		if (!typeReference.Resolve().IsInterface)
		{
			throw new Exception();
		}
		int slot = 0;
		foreach (MethodReference method in typeReference.GetVirtualMethods(context))
		{
			SetSlot(method, slot++);
		}
	}

	private VTable VTableForGenericInstance(ReadOnlyContext context, GenericInstanceType genericInstanceType, Dictionary<TypeReference, int> offsets)
	{
		TypeDefinition genericTypeDefinition = genericInstanceType.Resolve();
		List<VTableSlot> slots = new List<VTableSlot>(VTableFor(context, genericTypeDefinition).Slots);
		TypeResolver resolver = context.Global.Services.TypeFactory.ResolverFor(genericInstanceType);
		for (int i = 0; i < slots.Count; i++)
		{
			MethodReference method = slots[i].Method;
			if (method != null)
			{
				MethodReference inflated = resolver.Resolve(method);
				slots[i] = new VTableSlot(inflated, slots[i].Attr);
				SetSlot(inflated, GetSlot(method));
			}
		}
		for (int j = 0; j < genericTypeDefinition.Methods.Count; j++)
		{
			MethodDefinition method2 = genericTypeDefinition.Methods[j];
			if (method2.IsVirtual)
			{
				MethodReference inflatedMethod = resolver.Resolve(method2);
				if (!_methodSlots.ContainsKey(inflatedMethod))
				{
					int slot = GetSlot(method2);
					SetSlot(inflatedMethod, slot);
				}
			}
		}
		return AddVTable(genericInstanceType, new VTable(slots.AsReadOnly(), offsets));
	}

	private VTable AddVTable(TypeReference key, VTable value)
	{
		_vtables[key] = value;
		if (_trackNewItemsForMerging)
		{
			_newItemsAddedToVTables.Add((key, value));
		}
		return value;
	}

	private VTable VTableForType(ReadOnlyContext context, TypeDefinition typeDefinition, Dictionary<TypeReference, int> interfaceOffsets, int currentSlot)
	{
		TypeReference baseType = typeDefinition.BaseType;
		List<VTableSlot> slots = ((baseType != null) ? new List<VTableSlot>(VTableFor(context, baseType).Slots) : new List<VTableSlot>());
		if (currentSlot > slots.Count)
		{
			slots.AddRange(new VTableSlot[currentSlot - slots.Count]);
		}
		Dictionary<MethodReference, MethodDefinition> overrides = CollectOverrides(typeDefinition);
		Dictionary<MethodReference, MethodReference> overrideMap = new Dictionary<MethodReference, MethodReference>();
		if (!typeDefinition.IsInterface)
		{
			OverrideInterfaceMethods(interfaceOffsets, slots, overrides, overrideMap);
		}
		SetupInterfaceMethods(context, typeDefinition, interfaceOffsets, overrideMap, slots);
		SetupClassMethods(context, slots, typeDefinition, overrideMap);
		OverriddenonInterfaceMethods(overrides, slots, overrideMap);
		ReplaceOverriddenMethods(overrideMap, slots);
		return AddVTable(typeDefinition, new VTable(slots.AsReadOnly(), interfaceOffsets));
	}

	private static Dictionary<MethodReference, MethodDefinition> CollectOverrides(TypeDefinition typeDefinition)
	{
		Dictionary<MethodReference, MethodDefinition> overrides = new Dictionary<MethodReference, MethodDefinition>();
		foreach (MethodDefinition method in typeDefinition.Methods.Where((MethodDefinition m) => m.HasOverrides))
		{
			foreach (MethodReference overridden in method.Overrides)
			{
				overrides.Add(overridden, method);
			}
		}
		return overrides;
	}

	private void OverrideInterfaceMethods(Dictionary<TypeReference, int> interfaceOffsets, List<VTableSlot> slots, Dictionary<MethodReference, MethodDefinition> overrides, Dictionary<MethodReference, MethodReference> overrideMap)
	{
		foreach (KeyValuePair<MethodReference, MethodDefinition> overriddenPair in overrides)
		{
			MethodReference overriddenDefinition = overriddenPair.Key;
			if (overriddenDefinition.DeclaringType.Resolve().IsInterface)
			{
				int slot = GetSlot(overriddenDefinition);
				slot += interfaceOffsets[overriddenPair.Key.DeclaringType];
				slots[slot] = new VTableSlot(overriddenPair.Value);
				SetSlot(overriddenPair.Value, slot);
				overrideMap.Add(overriddenPair.Key, overriddenPair.Value);
			}
		}
	}

	private void SetupInterfaceMethods(ReadOnlyContext context, TypeDefinition typeDefinition, Dictionary<TypeReference, int> interfaceOffsets, Dictionary<MethodReference, MethodReference> overrideMap, List<VTableSlot> slots)
	{
		foreach (KeyValuePair<TypeReference, int> offset in interfaceOffsets)
		{
			TypeReference itf = offset.Key;
			int itfOffset = offset.Value;
			SetupMethodSlotsForInterface(context, itf);
			bool interfaceIsExplicitlyImplementedByClass = InterfaceIsExplicitlyImplementedByClass(typeDefinition, itf);
			foreach (MethodReference itfMethod in itf.GetVirtualMethods(context))
			{
				int itfMethodSlot = itfOffset + GetSlot(itfMethod);
				MethodReference @override;
				if (typeDefinition.IsInterface)
				{
					MethodDefinition itfMethodDef = itfMethod.Resolve();
					TypeDefinition adapterClass = context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition);
					slots[itfMethodSlot] = adapterClass.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == itfMethodDef));
				}
				else if (!overrideMap.TryGetValue(itfMethod, out @override))
				{
					foreach (MethodDefinition virtualMethod in typeDefinition.GetVirtualMethods())
					{
						if (CheckInterfaceMethodOverride(context, itfMethod, virtualMethod, requireNewslot: true, interfaceIsExplicitlyImplementedByClass, slots[itfMethodSlot].Method == null))
						{
							slots[itfMethodSlot] = virtualMethod;
							SetSlot(virtualMethod, itfMethodSlot);
						}
					}
					if (slots[itfMethodSlot].Method == null && typeDefinition.BaseType != null)
					{
						VTable parentVTable = VTableFor(context, typeDefinition.BaseType);
						for (int i = parentVTable.Slots.Count - 1; i >= 0; i--)
						{
							MethodReference parentMethod = parentVTable.Slots[i].Method;
							if (parentMethod != null && CheckInterfaceMethodOverride(context, itfMethod, parentMethod, requireNewslot: false, interfaceIsExplicitlyImplementedByClass: false, slotIsEmpty: true))
							{
								slots[itfMethodSlot] = parentMethod;
								if (!_methodSlots.ContainsKey(parentMethod))
								{
									SetSlot(parentMethod, itfMethodSlot);
								}
							}
						}
					}
					if (slots[itfMethodSlot].Method == null || slots[itfMethodSlot].Method.DeclaringType.IsInterface)
					{
						FindDefaultInterfaceMethodImplementation(context, typeDefinition, slots, itfMethod, itfMethodSlot);
					}
				}
				else if (slots[itfMethodSlot].Method != @override)
				{
					throw new Exception();
				}
			}
		}
	}

	private static void FindDefaultInterfaceMethodImplementation(ReadOnlyContext context, TypeDefinition typeDefinition, List<VTableSlot> slots, MethodReference itfMethod, int itfMethodSlot)
	{
		HashSet<MethodReference> candidates = new HashSet<MethodReference>();
		BuildDefaultInterfaceCandidates(context, typeDefinition, typeDefinition, itfMethod, candidates);
		List<MethodReference> matches = new List<MethodReference>(candidates.Count);
		foreach (MethodReference candidate in candidates)
		{
			bool isMatch = true;
			foreach (MethodReference otherCandidate in candidates)
			{
				if (otherCandidate != candidate && IsMoreDerivedInterface(context, otherCandidate.DeclaringType, candidate.DeclaringType))
				{
					isMatch = false;
					break;
				}
			}
			if (isMatch)
			{
				matches.Add(candidate);
			}
		}
		if (matches.Count == 1)
		{
			if (!matches[0].IsAbstract)
			{
				slots[itfMethodSlot] = new VTableSlot(matches[0], VTableSlotAttr.Normal);
			}
		}
		else if (matches.Count > 0)
		{
			slots[itfMethodSlot] = new VTableSlot(itfMethod, VTableSlotAttr.AmbiguousDefaultInterfaceMethod);
		}
	}

	private static void BuildDefaultInterfaceCandidates(ReadOnlyContext context, TypeDefinition typeDefinition, TypeReference interfaceType, MethodReference itfMethod, HashSet<MethodReference> candidates)
	{
		TypeResolver interfaceTypeResolver = context.Global.Services.TypeFactory.ResolverFor(interfaceType);
		foreach (InterfaceImplementation @interface in interfaceType.Resolve().Interfaces)
		{
			TypeReference iface = @interface.ResolveInterfaceImplementation(typeDefinition, interfaceTypeResolver);
			if (iface == itfMethod.DeclaringType)
			{
				candidates.Add(itfMethod);
				continue;
			}
			bool foundMatch = false;
			foreach (MethodReference implementedInterfaceMethod in iface.GetVirtualMethods(context))
			{
				TypeResolver resolver = context.Global.Services.TypeFactory.ResolverFor(implementedInterfaceMethod.DeclaringType, implementedInterfaceMethod);
				foreach (MethodReference @override in implementedInterfaceMethod.Resolve().Overrides)
				{
					if (resolver.Resolve(@override) == itfMethod)
					{
						candidates.Add(implementedInterfaceMethod);
						foundMatch = true;
						break;
					}
				}
			}
			if (!foundMatch)
			{
				BuildDefaultInterfaceCandidates(context, typeDefinition, iface, itfMethod, candidates);
			}
		}
	}

	private static bool IsMoreDerivedInterface(ReadOnlyContext context, TypeReference testingInterface, TypeReference baseInterface)
	{
		foreach (TypeReference implementedInterface in testingInterface.GetInterfaces(context))
		{
			if (implementedInterface == baseInterface || IsMoreDerivedInterface(context, implementedInterface, baseInterface))
			{
				return true;
			}
		}
		return false;
	}

	private void SetupClassMethods(ReadOnlyContext context, List<VTableSlot> slots, TypeDefinition typeDefinition, Dictionary<MethodReference, MethodReference> overrideMap)
	{
		int filledInterfaceSlots = 0;
		foreach (MethodDefinition method in typeDefinition.Methods.Where((MethodDefinition m) => m.IsVirtual && !m.IsStripped))
		{
			if (!method.IsNewSlot)
			{
				int slot = -1;
				for (TypeReference b = typeDefinition.GetBaseType(context); b != null; b = b.GetBaseType(context))
				{
					foreach (MethodReference virtualMethod in b.GetVirtualMethods(context))
					{
						if (!(method.Name != virtualMethod.Name) && VirtualMethodResolution.MethodSignaturesMatch(method, virtualMethod, context.Global.Services.TypeFactory))
						{
							slot = GetSlot(virtualMethod);
							overrideMap.Add(virtualMethod, method);
							break;
						}
					}
					if (slot >= 0)
					{
						break;
					}
				}
				if (slot >= 0)
				{
					SetSlot(method, slot);
				}
			}
			if (method.IsNewSlot && !method.IsFinal && _methodSlots.ContainsKey(method))
			{
				_methodSlots.Remove(method);
			}
			if (!_methodSlots.ContainsKey(method))
			{
				if (typeDefinition.IsInterface)
				{
					if (slots.Count == filledInterfaceSlots)
					{
						slots.Add(null);
					}
					SetSlot(method, filledInterfaceSlots++);
				}
				else
				{
					int slot2 = slots.Count;
					slots.Add(null);
					SetSlot(method, slot2);
				}
			}
			int methodSlot = GetSlot(method);
			if (!method.IsAbstract || typeDefinition.IsComOrWindowsRuntimeInterface(context))
			{
				slots[methodSlot] = method;
			}
			else if (typeDefinition.IsInterface)
			{
				TypeDefinition adapterClass = context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition);
				slots[methodSlot] = adapterClass.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == method));
			}
			else
			{
				slots[methodSlot] = null;
			}
		}
	}

	private void OverriddenonInterfaceMethods(Dictionary<MethodReference, MethodDefinition> overrides, List<VTableSlot> slots, Dictionary<MethodReference, MethodReference> overrideMap)
	{
		foreach (KeyValuePair<MethodReference, MethodDefinition> overriddenPair in overrides)
		{
			MethodReference declaration = overriddenPair.Key;
			MethodDefinition @override = overriddenPair.Value;
			TypeReference declaringType = declaration.DeclaringType;
			if (declaringType.Resolve().IsInterface)
			{
				continue;
			}
			int slot = GetSlot(declaration);
			slots[slot] = @override;
			SetSlot(@override, slot);
			overrideMap.TryGetValue(declaration, out var foundOverride);
			if (foundOverride != null)
			{
				if (foundOverride != @override)
				{
					throw new InvalidOperationException($"Error while creating VTable for {declaringType}. The base method {declaration} is implemented both by {foundOverride} and {@override}.");
				}
			}
			else
			{
				overrideMap.Add(declaration, @override);
			}
		}
	}

	private static void ReplaceOverriddenMethods(Dictionary<MethodReference, MethodReference> overrideMap, List<VTableSlot> slots)
	{
		if (overrideMap.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < slots.Count; i++)
		{
			if (slots[i].Method != null && overrideMap.TryGetValue(slots[i].Method, out var cm))
			{
				slots[i] = cm;
			}
		}
	}

	private static bool InterfaceIsExplicitlyImplementedByClass(TypeDefinition typeDefinition, TypeReference itf)
	{
		if (typeDefinition.BaseType != null)
		{
			return typeDefinition.Interfaces.Any((InterfaceImplementation classItf) => itf == classItf.InterfaceType);
		}
		return true;
	}

	private static bool CheckInterfaceMethodOverride(ReadOnlyContext context, MethodReference itfMethod, MethodReference virtualMethod, bool requireNewslot, bool interfaceIsExplicitlyImplementedByClass, bool slotIsEmpty)
	{
		if (itfMethod.Name == virtualMethod.Name)
		{
			if (!virtualMethod.Resolve().IsPublic)
			{
				return false;
			}
			if (!slotIsEmpty && requireNewslot)
			{
				if (!interfaceIsExplicitlyImplementedByClass)
				{
					return false;
				}
				if (!virtualMethod.Resolve().IsNewSlot)
				{
					return false;
				}
			}
			return VirtualMethodResolution.MethodSignaturesMatch(itfMethod, virtualMethod, context.Global.Services.TypeFactory);
		}
		return false;
	}

	public MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method, out VTableMultipleGenericInterfaceImpls multipleGenericInterfaceImpls)
	{
		multipleGenericInterfaceImpls = VTableMultipleGenericInterfaceImpls.None;
		MethodDefinition methodDefinition = method.Resolve();
		if (!methodDefinition.IsVirtual)
		{
			return method;
		}
		if (type.IsPointer)
		{
			return null;
		}
		int methodSlot = IndexFor(context, methodDefinition);
		VTable constrainedVTable = VTableFor(context, type);
		if (method.DeclaringType.IsInterface)
		{
			if (constrainedVTable.InterfaceOffsets.TryGetValue(method.DeclaringType, out var index))
			{
				MethodReference vTableMethod = constrainedVTable.Slots[index + methodSlot].Method;
				if (!vTableMethod.IsDefaultInterfaceMethod || !vTableMethod.DeclaringType.IsGenericInstance)
				{
					return vTableMethod;
				}
			}
			MethodReference foundInterfaceMethod = null;
			{
				foreach (KeyValuePair<TypeReference, int> interfaceOffset in constrainedVTable.InterfaceOffsets)
				{
					if (!interfaceOffset.Key.IsGenericInstance)
					{
						continue;
					}
					GenericInstanceType sharedInterfaceType = GenericSharingAnalysis.GetSharedType(context, interfaceOffset.Key);
					if (method.DeclaringType != sharedInterfaceType)
					{
						continue;
					}
					MethodReference currentInterfaceMethod = constrainedVTable.Slots[interfaceOffset.Value + methodSlot].Method;
					if (foundInterfaceMethod != null && foundInterfaceMethod != currentInterfaceMethod)
					{
						if (foundInterfaceMethod.DeclaringType == type || currentInterfaceMethod.DeclaringType == type)
						{
							multipleGenericInterfaceImpls |= VTableMultipleGenericInterfaceImpls.HasDirectImplementation;
						}
						if (foundInterfaceMethod.IsDefaultInterfaceMethod || currentInterfaceMethod.IsDefaultInterfaceMethod)
						{
							multipleGenericInterfaceImpls |= VTableMultipleGenericInterfaceImpls.HasDefaultInterfaceImplementation;
						}
					}
					foundInterfaceMethod = currentInterfaceMethod;
				}
				return foundInterfaceMethod;
			}
		}
		if (methodSlot >= constrainedVTable.Slots.Count)
		{
			return null;
		}
		MethodReference targetMethod = constrainedVTable.Slots[methodSlot].Method;
		if (targetMethod.Name != methodDefinition.Name)
		{
			return null;
		}
		return targetMethod;
	}

	protected override void DumpState(StringBuilder builder)
	{
		builder.AppendLine("-------MethodSlots-------");
		foreach (KeyValuePair<MethodReference, int> item in _methodSlots.ToSortedCollectionBy((KeyValuePair<MethodReference, int> i) => i.Key))
		{
			builder.AppendLine(item.Key.FullName);
			StringBuilder stringBuilder = builder;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder);
			handler.AppendLiteral("  Slot = ");
			handler.AppendFormatted(item.Value);
			stringBuilder2.AppendLine(ref handler);
		}
		builder.AppendLine("-------VTables-------");
		foreach (KeyValuePair<TypeReference, VTable> item2 in _vtables.ToSortedCollectionBy((KeyValuePair<TypeReference, VTable> i) => i.Key))
		{
			builder.AppendLine(item2.Key.FullName);
			if (item2.Value.Slots == null)
			{
				builder.AppendLine("  Slots: null");
			}
			else
			{
				StringBuilder stringBuilder = builder;
				StringBuilder stringBuilder3 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder);
				handler.AppendLiteral("  Slots:\n");
				handler.AppendFormatted(item2.Value.Slots.Select((VTableSlot m) => (m.Method != null) ? ("    " + m.Method.FullName) : "    null").AggregateWithNewLine());
				stringBuilder3.AppendLine(ref handler);
			}
			if (item2.Value.InterfaceOffsets == null)
			{
				builder.AppendLine("  InterfaceOffsets: null");
				continue;
			}
			if (item2.Value.InterfaceOffsets.Count == 0)
			{
				builder.AppendLine("  InterfaceOffsets: Empty");
				continue;
			}
			builder.AppendLine("  InterfaceOffsets:");
			foreach (KeyValuePair<TypeReference, int> interfaceOffsetItem in item2.Value.InterfaceOffsets.ToSortedCollectionBy((KeyValuePair<TypeReference, int> i) => i.Key))
			{
				StringBuilder stringBuilder = builder;
				StringBuilder stringBuilder4 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder);
				handler.AppendLiteral("    ");
				handler.AppendFormatted(interfaceOffsetItem.Key.FullName);
				stringBuilder4.AppendLine(ref handler);
				stringBuilder = builder;
				StringBuilder stringBuilder5 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
				handler.AppendLiteral("      Offset = ");
				handler.AppendFormatted(interfaceOffsetItem.Value);
				stringBuilder5.AppendLine(ref handler);
			}
		}
	}

	protected override void HandleMergeForAdd(VTableBuilderComponent forked)
	{
		foreach (var item in ItemsForMerging(forked._methodSlots, forked._newItemsAddedToMethodSlots, forked._trackNewItemsForMerging))
		{
			if (_trackNewItemsForMerging && !_methodSlots.ContainsKey(item.Item1))
			{
				_newItemsAddedToMethodSlots.Add(item);
			}
			_methodSlots[item.Item1] = item.Item2;
		}
		foreach (var item2 in ItemsForMerging(forked._vtables, forked._newItemsAddedToVTables, forked._trackNewItemsForMerging))
		{
			if (_trackNewItemsForMerging && !_vtables.ContainsKey(item2.Item1))
			{
				_newItemsAddedToVTables.Add(item2);
			}
			_vtables[item2.Item1] = item2.Item2;
		}
	}

	private static IEnumerable<(TKey, TValue)> ItemsForMerging<TKey, TValue>(Dictionary<TKey, TValue> cache, List<(TKey, TValue)> newItems, bool instanceIsUsingTrackNewItemsForMerging)
	{
		if (instanceIsUsingTrackNewItemsForMerging)
		{
			foreach (var newItem in newItems)
			{
				yield return newItem;
			}
			yield break;
		}
		foreach (KeyValuePair<TKey, TValue> item in cache)
		{
			yield return (item.Key, item.Value);
		}
	}

	protected override void HandleMergeForMergeValues(VTableBuilderComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotImplementedException();
	}

	protected override void SyncPooledInstanceWithParent(VTableBuilderComponent parent)
	{
		throw new NotImplementedException();
	}

	protected override VTableBuilderComponent CreateEmptyInstance()
	{
		return new VTableBuilderComponent();
	}

	protected override VTableBuilderComponent CreateCopyInstance()
	{
		return new VTableBuilderComponent(_methodSlots, _vtables);
	}

	protected override VTableBuilderComponent CreatePooledInstance()
	{
		throw new NotImplementedException();
	}

	protected override VTableBuilderComponent ThisAsFull()
	{
		return this;
	}

	protected override object ThisAsRead()
	{
		throw new NotSupportedException();
	}

	protected override IVTableBuilderService GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override object GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IVTableBuilderService writer, out object reader, out VTableBuilderComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IVTableBuilderService writer, out object reader, out VTableBuilderComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IVTableBuilderService writer, out object reader, out VTableBuilderComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IVTableBuilderService writer, out object reader, out VTableBuilderComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
	}
}
