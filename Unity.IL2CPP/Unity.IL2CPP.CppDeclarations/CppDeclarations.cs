using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.CppDeclarations;

public class CppDeclarations : CppDeclarationsBasic, ICppDeclarations, ICppDeclarationsBasic
{
	public readonly HashSet<TypeReference> _typeIncludes = new HashSet<TypeReference>();

	public readonly HashSet<IIl2CppRuntimeType> _typeExterns = new HashSet<IIl2CppRuntimeType>(Il2CppRuntimeTypeEqualityComparer.Default);

	public readonly HashSet<IIl2CppRuntimeType[]> _genericInstExterns = new HashSet<IIl2CppRuntimeType[]>(Il2CppRuntimeTypeArrayEqualityComparer.Default);

	public readonly HashSet<TypeReference> _genericClassExterns = new HashSet<TypeReference>();

	public readonly HashSet<TypeReference> _forwardDeclarations = new HashSet<TypeReference>();

	public readonly HashSet<ArrayType> _arrayTypes = new HashSet<ArrayType>();

	public readonly HashSet<string> _rawFileLevelPreprocessorStmts = new HashSet<string>();

	public readonly HashSet<MethodReference> _methods = new HashSet<MethodReference>();

	public readonly HashSet<MethodReference> _sharedMethods = new HashSet<MethodReference>();

	public readonly HashSet<VirtualMethodDeclarationData> _virtualMethods = new HashSet<VirtualMethodDeclarationData>(VirtualMethodDeclarationDataComparer.Default);

	public readonly Dictionary<string, string> _internalPInvokeMethodDeclarations = new Dictionary<string, string>();

	public readonly Dictionary<string, string> _internalPInvokeMethodDeclarationsForForcedInternalPInvoke = new Dictionary<string, string>();

	public ReadOnlyHashSet<TypeReference> TypeIncludes => _typeIncludes.AsReadOnly();

	public ReadOnlyHashSet<IIl2CppRuntimeType> TypeExterns => _typeExterns.AsReadOnly();

	public ReadOnlyHashSet<IIl2CppRuntimeType[]> GenericInstExterns => _genericInstExterns.AsReadOnly();

	public ReadOnlyHashSet<TypeReference> GenericClassExterns => _genericClassExterns.AsReadOnly();

	public ReadOnlyHashSet<TypeReference> ForwardDeclarations => _forwardDeclarations.AsReadOnly();

	public ReadOnlyHashSet<ArrayType> ArrayTypes => _arrayTypes.AsReadOnly();

	public ReadOnlyHashSet<string> RawFileLevelPreprocessorStmts => _rawFileLevelPreprocessorStmts.AsReadOnly();

	public ReadOnlyHashSet<MethodReference> Methods => _methods.AsReadOnly();

	public ReadOnlyHashSet<MethodReference> SharedMethods => _sharedMethods.AsReadOnly();

	public ReadOnlyHashSet<VirtualMethodDeclarationData> VirtualMethods => _virtualMethods.AsReadOnly();

	public IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarations => _internalPInvokeMethodDeclarations.AsReadOnly();

	public IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarationsForForcedInternalPInvoke => _internalPInvokeMethodDeclarationsForForcedInternalPInvoke.AsReadOnly();

	public void Add(ICppDeclarations other)
	{
		_includes.UnionWith(other.Includes);
		_typeIncludes.UnionWith(other.TypeIncludes);
		_typeExterns.UnionWith(other.TypeExterns);
		_genericInstExterns.UnionWith(other.GenericInstExterns);
		_genericClassExterns.UnionWith(other.GenericClassExterns);
		_forwardDeclarations.UnionWith(other.ForwardDeclarations);
		_arrayTypes.UnionWith(other.ArrayTypes);
		_rawTypeForwardDeclarations.UnionWith(other.RawTypeForwardDeclarations);
		_rawMethodForwardDeclarations.UnionWith(other.RawMethodForwardDeclarations);
		_rawFileLevelPreprocessorStmts.UnionWith(other.RawFileLevelPreprocessorStmts);
		_methods.UnionWith(other.Methods);
		_sharedMethods.UnionWith(other.SharedMethods);
		_virtualMethods.UnionWith(other.VirtualMethods);
		foreach (string methodName in other.InternalPInvokeMethodDeclarations.Keys)
		{
			if (!_internalPInvokeMethodDeclarations.ContainsKey(methodName))
			{
				_internalPInvokeMethodDeclarations.Add(methodName, other.InternalPInvokeMethodDeclarations[methodName]);
			}
		}
		foreach (string methodName2 in other.InternalPInvokeMethodDeclarationsForForcedInternalPInvoke.Keys)
		{
			if (!_internalPInvokeMethodDeclarationsForForcedInternalPInvoke.ContainsKey(methodName2))
			{
				_internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Add(methodName2, other.InternalPInvokeMethodDeclarationsForForcedInternalPInvoke[methodName2]);
			}
		}
	}
}
