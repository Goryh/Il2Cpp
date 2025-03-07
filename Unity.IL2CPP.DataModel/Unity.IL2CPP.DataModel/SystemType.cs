namespace Unity.IL2CPP.DataModel;

public enum SystemType
{
	Array,
	Attribute,
	BitConverter,
	Boolean,
	Byte,
	Char,
	Double,
	Enum,
	Int16,
	Int32,
	Int64,
	IntPtr,
	MulticastDelegate,
	Object,
	RuntimeTypeHandle,
	RuntimeMethodHandle,
	RuntimeFieldHandle,
	RuntimeArgumentHandle,
	SByteEnum,
	Int16Enum,
	Int32Enum,
	Int64Enum,
	ByteEnum,
	UInt16Enum,
	UInt32Enum,
	UInt64Enum,
	SByte,
	Single,
	String,
	Type,
	TypedReference,
	UInt16,
	UInt32,
	UInt64,
	UIntPtr,
	ValueType,
	Void,
	[SystemTypeName("Nullable`1")]
	Nullable,
	[SystemTypeName("IEquatable`1")]
	IEquatable_1,
	Exception,
	Delegate,
	[SystemTypeName("Action`1")]
	Action_1,
	[SystemTypeName("Func`2")]
	Func_2,
	[SystemTypeName("EventHandler`1")]
	EventHandler_1,
	[SystemTypeName("System.Runtime.InteropServices", "HandleRef")]
	HandleRef,
	[SystemTypeName("System.Runtime.InteropServices", "SafeHandle")]
	SafeHandle,
	[SystemTypeName("System.Runtime.InteropServices", "Marshal")]
	Marshal,
	[SystemTypeName("System.Runtime.InteropServices", "GuidAttribute")]
	GuidAttribute,
	[SystemTypeName("System.Runtime.CompilerServices", "IsByRefLikeAttribute")]
	IsByRefLikeAttribute,
	[SystemTypeName("System.Text", "StringBuilder")]
	StringBuilder,
	ArgumentException,
	ArgumentNullException,
	ArgumentOutOfRangeException,
	IDisposable,
	IConvertible,
	InvalidOperationException,
	NotSupportedException,
	OverflowException,
	[SystemTypeName("System.Collections", "ICollection")]
	ICollection,
	[SystemTypeName("System.Collections", "IEnumerable")]
	IEnumerable,
	[SystemTypeName("System.Collections", "IEnumerator")]
	IEnumerator,
	IComparable,
	[SystemTypeName("System", "IComparable`1")]
	IComparable_1,
	[SystemTypeName("System.Collections.Generic", "EqualityComparer`1")]
	EqualityComparer,
	[SystemTypeName("System.Collections.Generic", "GenericEqualityComparer`1")]
	GenericEqualityComparer,
	[SystemTypeName("System.Collections.Generic", "GenericComparer`1")]
	GenericComparer,
	[SystemTypeName("System.Collections.Generic", "Comparer`1")]
	Comparer_1,
	[SystemTypeName("System.Collections.Generic", "ObjectComparer`1")]
	ObjectComparer_1,
	[SystemTypeName("System.Collections.Generic", "ICollection`1")]
	ICollection_1,
	[SystemTypeName("System.Collections.Generic", "IList`1")]
	IList_1,
	[SystemTypeName("System.Collections.Generic", "IEnumerable`1")]
	IEnumerable_1,
	[SystemTypeName("System.Collections.Generic", "IEnumerator`1")]
	IEnumerator_1,
	[SystemTypeName("System.Collections.Generic", "IReadOnlyList`1")]
	IReadOnlyList_1,
	[SystemTypeName("System.Collections.Generic", "IReadOnlyCollection`1")]
	IReadOnlyCollection_1,
	[SystemTypeName("System.Collections.Generic", "KeyNotFoundException")]
	KeyNotFoundException,
	[SystemTypeName("System.Collections.Generic", "KeyValuePair`2")]
	KeyValuePair,
	[SystemTypeName("System.Collections.Generic", "NullableEqualityComparer`1")]
	NullableEqualityComparer,
	[SystemTypeName("System.Collections.Generic", "NullableComparer`1")]
	NullableComparer,
	[SystemTypeName("System.Collections.ObjectModel", "ReadOnlyCollection`1")]
	ReadOnlyCollection,
	[SystemTypeName("System.Collections.ObjectModel", "ReadOnlyDictionary`2")]
	ReadOnlyDictionary,
	[SystemTypeName("System.Collections.Generic", "SByteEnumEqualityComparer`1")]
	SByteEnumEqualityComparer_1,
	[SystemTypeName("System.Collections.Generic", "ShortEnumEqualityComparer`1")]
	ShortEnumEqualityComparer_1,
	[SystemTypeName("System.Collections.Generic", "LongEnumEqualityComparer`1")]
	LongEnumEqualityComparer_1,
	[SystemTypeName("System.Collections.Generic", "EnumEqualityComparer`1")]
	EnumEqualityComparer_1,
	[SystemTypeName("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken")]
	EventRegistrationToken,
	[SystemTypeName("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeMarshal")]
	WindowsRuntimeMarshal,
	[SystemTypeName("System.Runtime.InteropServices.WindowsRuntime.Xaml", "ListToBindableVectorViewAdapter", AssemblyName = "System.Runtime.WindowsRuntime.UI.Xaml", Version = "4.0.0.0")]
	ListToBindableVectorViewAdapter,
	[SystemTypeName("Windows.UI.Xaml.Input", "ICommandEventHelper", AssemblyName = "System.Runtime.WindowsRuntime.UI.Xaml", Version = "4.0.0.0")]
	ICommandEventHelper,
	[SystemTypeName("System.Runtime.InteropServices.WindowsRuntime", "ConstantSplittableMap`2", AssemblyName = "System.Runtime.WindowsRuntime", Version = "4.0.0.0")]
	ConstantSplittableMap_2,
	[SystemTypeName("Windows.Foundation", "IPropertyValue", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IPropertyValue,
	[SystemTypeName("Windows.Foundation", "IReferenceArray`1", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IReferenceArray,
	[SystemTypeName("Windows.Foundation", "IReference`1", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IReference,
	[SystemTypeName("Windows.Foundation", "IStringable", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IStringable,
	[SystemTypeName("Windows.Foundation.Collections", "IIterable`1", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IIterable,
	[SystemTypeName("Windows.Foundation.Collections", "IIterator`1", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IIterator,
	[SystemTypeName("Windows.UI.Xaml.Interop", "IBindableIterable", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IBindableIterable,
	[SystemTypeName("Windows.UI.Xaml.Interop", "IBindableIterator", AssemblyName = "WindowsRuntimeMetadata", Version = "255.255.255.255", IsWindowsRuntime = true)]
	IBindableIterator
}
