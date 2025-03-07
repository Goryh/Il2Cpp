using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class WindowsRuntimeProjectionsBuilder
{
	private readonly Dictionary<TypeDefinition, TypeDefinition> _clrTypeToWindowsRuntimeTypeMap;

	private readonly Dictionary<TypeDefinition, TypeDefinition> _windowsRuntimeTypeToCLRTypeMap;

	private readonly Dictionary<WindowsRuntimeProjections.ClrProjectionKey, WindowsRuntimeProjections.Mapping> _projectedTypes = new Dictionary<WindowsRuntimeProjections.ClrProjectionKey, WindowsRuntimeProjections.Mapping>();

	private readonly TypeContext _context;

	public WindowsRuntimeProjectionsBuilder(TypeContext context)
	{
		_context = context;
		_clrTypeToWindowsRuntimeTypeMap = new Dictionary<TypeDefinition, TypeDefinition>();
		_windowsRuntimeTypeToCLRTypeMap = new Dictionary<TypeDefinition, TypeDefinition>();
	}

	public WindowsRuntimeProjections Build()
	{
		if (!_context.WindowsRuntimeAssembliesLoaded)
		{
			return new WindowsRuntimeProjections(_context, _clrTypeToWindowsRuntimeTypeMap.AsReadOnly(), _windowsRuntimeTypeToCLRTypeMap.AsReadOnly(), _projectedTypes.AsReadOnly());
		}
		AddProjection("System.ObjectModel", "System.Collections.Specialized", "INotifyCollectionChanged", "Windows.UI.Xaml.Interop", "INotifyCollectionChanged");
		AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedAction", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction");
		AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs");
		AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler");
		AddProjection("System.ObjectModel", "System.ComponentModel", "INotifyPropertyChanged", "Windows.UI.Xaml.Data", "INotifyPropertyChanged");
		AddProjection("System.ObjectModel", "System.ComponentModel", "PropertyChangedEventArgs", "Windows.UI.Xaml.Data", "PropertyChangedEventArgs");
		AddProjection("System.ObjectModel", "System.ComponentModel", "PropertyChangedEventHandler", "Windows.UI.Xaml.Data", "PropertyChangedEventHandler");
		AddProjection("System.ObjectModel", "System.Windows.Input", "ICommand", "Windows.UI.Xaml.Input", "ICommand");
		AddProjection("System.Runtime", "System", "AttributeTargets", "Windows.Foundation.Metadata", "AttributeTargets");
		AddProjection("System.Runtime", "System", "AttributeUsageAttribute", "Windows.Foundation.Metadata", "AttributeUsageAttribute");
		AddProjection("System.Runtime", "System", "DateTimeOffset", "Windows.Foundation", "DateTime");
		AddProjection("System.Runtime", "System", "EventHandler`1", "Windows.Foundation", "EventHandler`1");
		AddProjection("System.Runtime", "System", "Exception", "Windows.Foundation", "HResult");
		AddProjection("System.Runtime", "System", "IDisposable", "Windows.Foundation", "IClosable");
		AddProjection("System.Runtime", "System", "Nullable`1", "Windows.Foundation", "IReference`1");
		AddProjection("System.Runtime", "System", "TimeSpan", "Windows.Foundation", "TimeSpan");
		AddProjection("System.Runtime", "System", "Type", "Windows.UI.Xaml.Interop", "TypeName");
		AddProjection("System.Runtime", "System", "Uri", "Windows.Foundation", "Uri");
		AddProjection("System.Runtime", "System.Collections", "IEnumerable", "Windows.UI.Xaml.Interop", "IBindableIterable");
		AddProjection("System.Runtime", "System.Collections", "IList", "Windows.UI.Xaml.Interop", "IBindableVector");
		AddProjection("System.Runtime", "System.Collections.Generic", "IEnumerable`1", "Windows.Foundation.Collections", "IIterable`1");
		AddProjection("System.Runtime", "System.Collections.Generic", "IDictionary`2", "Windows.Foundation.Collections", "IMap`2");
		AddProjection("System.Runtime", "System.Collections.Generic", "IList`1", "Windows.Foundation.Collections", "IVector`1");
		AddProjection("System.Runtime", "System.Collections.Generic", "IReadOnlyDictionary`2", "Windows.Foundation.Collections", "IMapView`2");
		AddProjection("System.Runtime", "System.Collections.Generic", "IReadOnlyList`1", "Windows.Foundation.Collections", "IVectorView`1");
		AddProjection("System.Runtime", "System.Collections.Generic", "KeyValuePair`2", "Windows.Foundation.Collections", "IKeyValuePair`2");
		AddProjection("System.Runtime.InteropServices.WindowsRuntime", "System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken");
		AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Point", "Windows.Foundation", "Point");
		AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Rect", "Windows.Foundation", "Rect");
		AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Size", "Windows.Foundation", "Size");
		AddProjection("System.Runtime.WindowsRuntime", "Windows.UI", "Color", "Windows.UI", "Color");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "CornerRadius", "Windows.UI.Xaml", "CornerRadius");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "Duration", "Windows.UI.Xaml", "Duration");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "DurationType", "Windows.UI.Xaml", "DurationType");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "GridLength", "Windows.UI.Xaml", "GridLength");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "GridUnitType", "Windows.UI.Xaml", "GridUnitType");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "Thickness", "Windows.UI.Xaml", "Thickness");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media", "Matrix", "Windows.UI.Xaml.Media", "Matrix");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime");
		AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Matrix3x2", "Windows.Foundation.Numerics", "Matrix3x2");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Matrix4x4", "Windows.Foundation.Numerics", "Matrix4x4");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Plane", "Windows.Foundation.Numerics", "Plane");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Quaternion", "Windows.Foundation.Numerics", "Quaternion");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector2", "Windows.Foundation.Numerics", "Vector2");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector3", "Windows.Foundation.Numerics", "Vector3");
		AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector4", "Windows.Foundation.Numerics", "Vector4");
		return new WindowsRuntimeProjections(_context, _clrTypeToWindowsRuntimeTypeMap.AsReadOnly(), _windowsRuntimeTypeToCLRTypeMap.AsReadOnly(), _projectedTypes.AsReadOnly());
	}

	private void AddProjection(string clrAssembly, string clrNamespace, string clrName, string windowsRuntimeNamespace, string windowsRuntimeName)
	{
		TypeDefinition clrType = _context.ThisIsSlowFindType(clrAssembly, clrNamespace, clrName);
		TypeDefinition windowsRuntimeType = _context.WindowsRuntimeMetadataAssembly.ThisIsSlowFindType(windowsRuntimeNamespace, windowsRuntimeName);
		if (clrType != null && windowsRuntimeType != null)
		{
			_clrTypeToWindowsRuntimeTypeMap.Add(clrType, windowsRuntimeType);
			_windowsRuntimeTypeToCLRTypeMap.Add(windowsRuntimeType, clrType);
			if (windowsRuntimeType.Methods.Any((MethodDefinition m) => !m.IsStripped))
			{
				_projectedTypes.Add(new WindowsRuntimeProjections.ClrProjectionKey(clrAssembly, clrNamespace, clrName), new WindowsRuntimeProjections.Mapping(clrType, windowsRuntimeType));
			}
		}
	}
}
