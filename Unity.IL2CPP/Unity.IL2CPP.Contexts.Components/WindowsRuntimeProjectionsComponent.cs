using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Contexts.Components;

public class WindowsRuntimeProjectionsComponent : ReusedServiceComponentBase<IWindowsRuntimeProjections, WindowsRuntimeProjectionsComponent>, IWindowsRuntimeProjections
{
	private readonly Dictionary<TypeDefinition, IProjectedComCallableWrapperMethodWriter> _projectedComCallableWrapperWriterMap;

	private Dictionary<TypeDefinition, TypeDefinition> _nativeToManagedInterfaceAdapterClasses;

	private TypeContext _typeContext;

	public bool AreWindowsRuntimeLibrariesLoaded => _typeContext.WindowsRuntimeAssembliesLoaded;

	public WindowsRuntimeProjectionsComponent()
	{
		_projectedComCallableWrapperWriterMap = new Dictionary<TypeDefinition, IProjectedComCallableWrapperMethodWriter>();
		_nativeToManagedInterfaceAdapterClasses = new Dictionary<TypeDefinition, TypeDefinition>();
	}

	public void Initialize(PrimaryCollectionContext context, TypeContext typeContext)
	{
		_typeContext = typeContext;
		if (!context.Global.InputData.Profile.SupportsWindowsRuntime || !typeContext.WindowsRuntimeAssembliesLoaded)
		{
			return;
		}
		EditContext editContext = typeContext.CreateEditContext();
		Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> interfaceAdapterMethodBodyWriters = new Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>>();
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.ObjectModel", "System.Windows.Input", "ICommand", out var clrType, out var windowsRuntimeType))
		{
			ICommandProjectedMethodBodyWriter icommandMethodBodyWriter = new ICommandProjectedMethodBodyWriter(context, typeContext, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "ICommand", "add_CanExecuteChanged", icommandMethodBodyWriter.WriteAddCanExecuteChanged);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "ICommand", "remove_CanExecuteChanged", icommandMethodBodyWriter.WriteRemoveCanExecuteChanged);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "ICommand", "CanExecute", icommandMethodBodyWriter.WriteCanExecute);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "ICommand", "Execute", icommandMethodBodyWriter.WriteExecute);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new CommandCCWWriter(context, clrType));
		}
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System", "IDisposable", out clrType, out windowsRuntimeType))
		{
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDisposable", "Dispose", new IDisposableDisposeMethodBodyWriter(windowsRuntimeType.Methods.Single((MethodDefinition m) => m.Name == "Close")).WriteDispose);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DisposableCCWWriter());
		}
		TypeDefinition ienumeratorType = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator);
		TypeDefinition ibindableIteratorType = typeContext.WindowsRuntimeMetadataAssembly.ThisIsSlowFindType("Windows.UI.Xaml.Interop", "IBindableIterator");
		if (ienumeratorType != null && ibindableIteratorType != null && typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections", "IEnumerable", out clrType, out windowsRuntimeType))
		{
			TypeDefinition iteratorToEnumeratorAdapter = new IteratorToEnumeratorAdapterTypeGenerator(context, editContext, ibindableIteratorType, ienumeratorType).Generate();
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IEnumerable", "GetEnumerator", new IEnumerableMethodBodyWriter(context, editContext, iteratorToEnumeratorAdapter, windowsRuntimeType).WriteGetEnumerator);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new EnumerableCCWWriter());
		}
		TypeDefinition iListType = null;
		TypeDefinition iVectorType = null;
		TypeDefinition iDictionaryType = null;
		TypeDefinition iMapType = null;
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections", "IList", out clrType, out windowsRuntimeType))
		{
			TypeDefinition iCollectionType = clrType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection").InterfaceType.Resolve();
			IListProjectedMethodBodyWriter iListMethodBodyWriter = new IListProjectedMethodBodyWriter(context, editContext, windowsRuntimeType);
			ICollectionProjectedMethodBodyWriter iCollectionMethodBodyWriter = new ICollectionProjectedMethodBodyWriter(context, editContext, iCollectionType, null, null, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "get_Item", iListMethodBodyWriter.WriteGetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "IndexOf", iListMethodBodyWriter.WriteIndexOf);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "Insert", iListMethodBodyWriter.WriteInsert);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "RemoveAt", iListMethodBodyWriter.WriteRemoveAt);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "set_Item", iListMethodBodyWriter.WriteSetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "Add", iCollectionMethodBodyWriter.WriteAdd);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "Clear", iCollectionMethodBodyWriter.WriteClear);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "Contains", iCollectionMethodBodyWriter.WriteContains);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "get_IsFixedSize", iCollectionMethodBodyWriter.WriteGetIsFixedSize);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "get_IsReadOnly", iCollectionMethodBodyWriter.WriteGetIsReadOnly);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList", "Remove", iCollectionMethodBodyWriter.WriteRemove);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType, "ICollection", "CopyTo", iCollectionMethodBodyWriter.WriteCopyTo);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType, "ICollection", "get_Count", iCollectionMethodBodyWriter.WriteGetCount);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType, "ICollection", "get_IsSynchronized", iCollectionMethodBodyWriter.WriteGetIsSynchronized);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType, "ICollection", "get_SyncRoot", iCollectionMethodBodyWriter.WriteGetSyncRoot);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
		}
		TypeDefinition genericIEnumeratorType = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator_1);
		TypeDefinition genericIIteratorType = typeContext.WindowsRuntimeMetadataAssembly.ThisIsSlowFindType("Windows.Foundation.Collections", "IIterator`1");
		TypeDefinition genericIIterableType = null;
		if (genericIEnumeratorType != null && genericIIteratorType != null && typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "IEnumerable`1", out clrType, out windowsRuntimeType))
		{
			genericIIterableType = windowsRuntimeType;
			TypeDefinition iteratorToEnumeratorAdapter2 = new IteratorToEnumeratorAdapterTypeGenerator(context, editContext, genericIIteratorType, genericIEnumeratorType).Generate();
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IEnumerable`1", "GetEnumerator", new IEnumerableMethodBodyWriter(context, editContext, iteratorToEnumeratorAdapter2, genericIIterableType).WriteGetEnumerator);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IEnumerable", "GetEnumerator", new IEnumerableMethodBodyWriter(context, editContext, iteratorToEnumeratorAdapter2, genericIIterableType).WriteGetEnumerator);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new EnumerableCCWWriter());
		}
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "IDictionary`2", out clrType, out windowsRuntimeType))
		{
			IDictionaryProjectedMethodBodyWriter iDictionaryMethodBodyWriter = new IDictionaryProjectedMethodBodyWriter(context, editContext, clrType, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "get_Item", iDictionaryMethodBodyWriter.WriteGetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "get_Keys", iDictionaryMethodBodyWriter.WriteGetKeys);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "get_Values", iDictionaryMethodBodyWriter.WriteGetValues);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "Add", iDictionaryMethodBodyWriter.WriteAdd);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "ContainsKey", iDictionaryMethodBodyWriter.WriteContainsKey);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "Remove", iDictionaryMethodBodyWriter.WriteRemove);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "set_Item", iDictionaryMethodBodyWriter.WriteSetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IDictionary`2", "TryGetValue", iDictionaryMethodBodyWriter.WriteTryGetValue);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DictionaryCCWWriter(clrType));
			iDictionaryType = clrType;
			iMapType = windowsRuntimeType;
		}
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "IList`1", out clrType, out windowsRuntimeType))
		{
			IListProjectedMethodBodyWriter iListMethodBodyWriter2 = new IListProjectedMethodBodyWriter(context, editContext, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList`1", "get_Item", iListMethodBodyWriter2.WriteGetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList`1", "IndexOf", iListMethodBodyWriter2.WriteIndexOf);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList`1", "Insert", iListMethodBodyWriter2.WriteInsert);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList`1", "RemoveAt", iListMethodBodyWriter2.WriteRemoveAt);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IList`1", "set_Item", iListMethodBodyWriter2.WriteSetItem);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
			iListType = clrType;
			iVectorType = windowsRuntimeType;
		}
		TypeDefinition iReadOnlyDictionaryType = null;
		TypeDefinition iMapViewType = null;
		TypeDefinition iReadOnlyListType = null;
		TypeDefinition iVectorViewType = null;
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "IReadOnlyDictionary`2", out clrType, out windowsRuntimeType))
		{
			IDictionaryProjectedMethodBodyWriter iReadOnlyDictionaryMethodBodyWriter = new IDictionaryProjectedMethodBodyWriter(context, editContext, clrType, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyDictionary`2", "get_Item", iReadOnlyDictionaryMethodBodyWriter.WriteGetItem);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyDictionary`2", "get_Keys", iReadOnlyDictionaryMethodBodyWriter.WriteGetKeys);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyDictionary`2", "get_Values", iReadOnlyDictionaryMethodBodyWriter.WriteGetValues);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyDictionary`2", "ContainsKey", iReadOnlyDictionaryMethodBodyWriter.WriteContainsKey);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyDictionary`2", "TryGetValue", iReadOnlyDictionaryMethodBodyWriter.WriteTryGetValue);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DictionaryCCWWriter(clrType));
			iReadOnlyDictionaryType = clrType;
			iMapViewType = windowsRuntimeType;
		}
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "IReadOnlyList`1", out clrType, out windowsRuntimeType))
		{
			IListProjectedMethodBodyWriter iReadOnlyListMethodBodyWriter = new IListProjectedMethodBodyWriter(context, editContext, windowsRuntimeType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, clrType, "IReadOnlyList`1", "get_Item", iReadOnlyListMethodBodyWriter.WriteGetItem);
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
			iReadOnlyListType = clrType;
			iVectorViewType = windowsRuntimeType;
		}
		if (iVectorType != null || iMapType != null)
		{
			TypeDefinition iCollectionType2 = ((iListType != null) ? iListType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection`1").InterfaceType.Resolve() : iDictionaryType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection`1").InterfaceType.Resolve());
			ICollectionProjectedMethodBodyWriter iCollectionMethodBodyWriter2 = new ICollectionProjectedMethodBodyWriter(context, editContext, iCollectionType2, iDictionaryType, iMapType, iVectorType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "Add", iCollectionMethodBodyWriter2.WriteAdd);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "Clear", iCollectionMethodBodyWriter2.WriteClear);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "Contains", iCollectionMethodBodyWriter2.WriteContains);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "CopyTo", iCollectionMethodBodyWriter2.WriteCopyTo);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "get_Count", iCollectionMethodBodyWriter2.WriteGetCount);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "get_IsReadOnly", iCollectionMethodBodyWriter2.WriteGetIsReadOnly);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iCollectionType2, "ICollection`1", "Remove", iCollectionMethodBodyWriter2.WriteRemove);
		}
		if (iVectorViewType != null || iMapViewType != null)
		{
			TypeDefinition iReadOnlyCollectionType = ((iReadOnlyListType != null) ? iReadOnlyListType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType.Resolve() : iReadOnlyDictionaryType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType.Resolve());
			ICollectionProjectedMethodBodyWriter iCollectionMethodBodyWriter3 = new ICollectionProjectedMethodBodyWriter(context, editContext, iReadOnlyCollectionType, iReadOnlyCollectionType, iMapViewType, iVectorViewType);
			AddInterfaceAdapterMethodBodyWriter(context, interfaceAdapterMethodBodyWriters, iReadOnlyCollectionType, "IReadOnlyCollection`1", "get_Count", iCollectionMethodBodyWriter3.WriteGetCount);
		}
		if (typeContext.WindowsRuntimeProjections.WasProjected("System.Runtime", "System.Collections.Generic", "KeyValuePair`2", out clrType, out windowsRuntimeType))
		{
			_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new KeyValuePairCCWWriter(clrType));
		}
		_nativeToManagedInterfaceAdapterClasses = InterfaceNativeToManagedAdapterGenerator.Generate(context, editContext, _typeContext.WindowsRuntimeProjections.ClrTypeToWindowsRuntimeTypeMap, interfaceAdapterMethodBodyWriters);
	}

	private void AddInterfaceAdapterMethodBodyWriter(ReadOnlyContext context, Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> interfaceAdapterMethodBodyWriters, TypeDefinition clrType, string clrDeclaringTypeName, string clrMethodName, InterfaceAdapterMethodBodyWriter methodWriter)
	{
		MethodDefinition method = new TypeDefinition[1] { clrType }.Union(from i in clrType.GetInterfaces(context)
			select i.Resolve()).SelectMany((TypeDefinition t) => t.Methods).SingleOrDefault((MethodDefinition m) => m.DeclaringType.Name == clrDeclaringTypeName && m.Name == clrMethodName);
		if (method != null)
		{
			if (!interfaceAdapterMethodBodyWriters.ContainsKey(clrType))
			{
				interfaceAdapterMethodBodyWriters[clrType] = new Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>();
			}
			interfaceAdapterMethodBodyWriters[clrType].Add(method, methodWriter);
		}
	}

	public TypeReference ProjectToWindowsRuntime(ReadOnlyContext context, TypeReference clrType)
	{
		if (_typeContext == null)
		{
			return clrType;
		}
		return _typeContext.WindowsRuntimeProjections.ProjectToWindowsRuntime(clrType, context.Global.Services.TypeFactory);
	}

	public TypeDefinition ProjectToWindowsRuntime(TypeDefinition clrType)
	{
		if (_typeContext == null)
		{
			return clrType;
		}
		return _typeContext.WindowsRuntimeProjections.ProjectToWindowsRuntime(clrType);
	}

	public TypeReference ProjectToCLR(TypeReference windowsRuntimeType)
	{
		return _typeContext.WindowsRuntimeProjections.ProjectToCLR(windowsRuntimeType);
	}

	public TypeDefinition ProjectToCLR(TypeDefinition windowsRuntimeType)
	{
		return _typeContext.WindowsRuntimeProjections.ProjectToCLR(windowsRuntimeType);
	}

	public IProjectedComCallableWrapperMethodWriter GetProjectedComCallableWrapperMethodWriterFor(TypeDefinition type)
	{
		_projectedComCallableWrapperWriterMap.TryGetValue(type, out var writer);
		return writer;
	}

	public TypeDefinition GetNativeToManagedAdapterClassFor(TypeDefinition interfaceType)
	{
		TypeDefinition result = null;
		_nativeToManagedInterfaceAdapterClasses.TryGetValue(interfaceType, out result);
		return result;
	}

	public IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetClrToWindowsRuntimeProjectedTypes()
	{
		return _typeContext.WindowsRuntimeProjections.ClrTypeToWindowsRuntimeTypeMap;
	}

	public IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetNativeToManagedInterfaceAdapterClasses()
	{
		return _nativeToManagedInterfaceAdapterClasses;
	}

	protected override WindowsRuntimeProjectionsComponent ThisAsFull()
	{
		return this;
	}

	protected override IWindowsRuntimeProjections ThisAsRead()
	{
		return this;
	}
}
