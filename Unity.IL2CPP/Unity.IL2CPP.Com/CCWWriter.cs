using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Com;

public class CCWWriter : CCWWriterBase
{
	private sealed class InterfaceMethodMapping
	{
		public readonly MethodReference InterfaceMethod;

		public MethodReference ManagedMethod;

		public InterfaceMethodMapping(MethodReference interfaceMethod, MethodReference managedMethod)
		{
			InterfaceMethod = interfaceMethod;
			ManagedMethod = managedMethod;
		}
	}

	private readonly string _typeName;

	private readonly List<TypeReference> _interfacesToImplement;

	private readonly List<InterfaceMethodMapping> _interfaceMethodMappings;

	private readonly TypeReference[] _allInteropInterfaces;

	private readonly TypeReference[] _interfacesToForwardToBaseClass;

	private readonly bool _hasBaseClass;

	private readonly bool _implementsAnyIInspectableInterfaces;

	private readonly List<MethodReference> _implementedIReferenceMethods;

	private readonly GenericInstanceType _ireferenceOfType;

	private readonly TypeReference _boxedType;

	protected override bool ImplementsAnyIInspectableInterfaces => _implementsAnyIInspectableInterfaces;

	protected override IEnumerable<TypeReference> AllImplementedInterfaces => _interfacesToImplement;

	protected override bool HasBaseClass => _hasBaseClass;

	protected override IList<TypeReference> InterfacesToForwardToBaseClass => _interfacesToForwardToBaseClass;

	public CCWWriter(SourceWritingContext context, TypeReference type)
		: base(context, type)
	{
		_typeName = _context.Global.Services.Naming.ForComCallableWrapperClass(type);
		_interfaceMethodMappings = new List<InterfaceMethodMapping>();
		_interfacesToImplement = new List<TypeReference>();
		_implementedIReferenceMethods = new List<MethodReference>();
		_hasBaseClass = !type.IsArray && type.Resolve().GetTypeHierarchy().Any((TypeDefinition t) => t.IsComOrWindowsRuntimeType());
		_allInteropInterfaces = type.GetInterfacesImplementedByComCallableWrapper(context).ToArray();
		VTable vtable = ((!type.IsArray) ? context.Global.Services.VTable.VTableFor(context, type) : null);
		foreach (TypeReference iface in GetInterfacesToPotentiallyImplement(_allInteropInterfaces, GetInterfacesToNotImplement(type)))
		{
			int interfaceOffset = 0;
			bool isProjectedInterface = type.IsArray || !vtable.InterfaceOffsets.TryGetValue(iface, out interfaceOffset);
			bool atLeastOneMethodIsManaged = false;
			List<InterfaceMethodMapping> interfaceMappings = new List<InterfaceMethodMapping>();
			int methodIndex = 0;
			foreach (MethodReference interfaceMethod in from m in iface.GetMethods(_context)
				where m.HasThis && m.IsVirtual
				select m)
			{
				if (!interfaceMethod.IsStripped && !isProjectedInterface)
				{
					MethodReference managedMethod = vtable.Slots[interfaceOffset + methodIndex].Method;
					interfaceMappings.Add(new InterfaceMethodMapping(interfaceMethod, managedMethod));
					methodIndex++;
					if (!managedMethod.DeclaringType.Resolve().IsComOrWindowsRuntimeType())
					{
						atLeastOneMethodIsManaged = true;
					}
				}
				else
				{
					interfaceMappings.Add(new InterfaceMethodMapping(interfaceMethod, null));
				}
			}
			if (!_hasBaseClass || atLeastOneMethodIsManaged || isProjectedInterface)
			{
				_interfacesToImplement.Add(iface);
				_interfaceMethodMappings.AddRange(interfaceMappings);
			}
		}
		_ireferenceOfType = GetIReferenceInterface(_type, out _boxedType);
		if (_ireferenceOfType != null)
		{
			_interfacesToImplement.Add(_ireferenceOfType);
			_interfacesToImplement.Add(_context.Global.Services.TypeProvider.IPropertyValueType);
			MethodDefinition getValueMethodDef = _ireferenceOfType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_Value");
			_implementedIReferenceMethods.Add(context.Global.Services.TypeFactory.ResolverFor(_ireferenceOfType).Resolve(getValueMethodDef));
			foreach (MethodDefinition method in _context.Global.Services.TypeProvider.IPropertyValueType.Resolve().Methods)
			{
				_implementedIReferenceMethods.Add(method);
			}
		}
		_interfacesToForwardToBaseClass = _allInteropInterfaces.Except(_interfacesToImplement).ToArray();
		_implementsAnyIInspectableInterfaces = _interfacesToImplement.Any((TypeReference i) => i.Resolve().IsExposedToWindowsRuntime());
	}

	private GenericInstanceType GetIReferenceInterface(TypeReference type, out TypeReference boxedType)
	{
		boxedType = null;
		TypeReference ireferenceGenericArgument;
		TypeDefinition ireferenceTypeDef;
		if (type is ArrayType arrayType)
		{
			TypeDefinition elementTypeDef = arrayType.ElementType.Resolve();
			if (type.CanBoxToWindowsRuntime(_context))
			{
				ireferenceGenericArgument = arrayType.ElementType;
				boxedType = arrayType;
			}
			else
			{
				if (elementTypeDef.MetadataType != MetadataType.Class || !elementTypeDef.IsWindowsRuntime)
				{
					return null;
				}
				ireferenceGenericArgument = _context.Global.Services.TypeProvider.SystemObject;
				boxedType = _context.Global.Services.TypeFactory.CreateArrayType(ireferenceGenericArgument);
			}
			ireferenceTypeDef = _context.Global.Services.TypeProvider.IReferenceArrayType.Resolve();
		}
		else
		{
			if (!type.CanBoxToWindowsRuntime(_context))
			{
				return null;
			}
			ireferenceTypeDef = _context.Global.Services.TypeProvider.IReferenceType.Resolve();
			ireferenceGenericArgument = (boxedType = type);
		}
		return _context.Global.Services.TypeFactory.CreateGenericInstanceType(ireferenceTypeDef, ireferenceTypeDef.DeclaringType, ireferenceGenericArgument);
	}

	public override void Write(IGeneratedMethodCodeWriter writer)
	{
		writer.AddInclude("vm/CachedCCWBase.h");
		AddIncludes(writer);
		string baseTypeName = GetBaseTypeName();
		writer.WriteLine();
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("COM Callable Wrapper for " + _type.FullName);
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"struct {_typeName} IL2CPP_FINAL : {baseTypeName}");
		using (new BlockWriter(writer, semicolon: true))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"inline {_typeName}(RuntimeObject* obj) : il2cpp::vm::CachedCCWBase<{_typeName}>(obj) {{}}");
			WriteCommonInterfaceMethods(writer);
			foreach (InterfaceMethodMapping mapping in _interfaceMethodMappings)
			{
				WriteImplementedMethodDefinition(writer, mapping);
			}
			foreach (MethodReference method in _implementedIReferenceMethods)
			{
				WriteImplementedIReferenceMethodDefinition(writer, method);
			}
		}
		_context.Global.Collectors.Stats.RecordComCallableWrapper();
	}

	private void WriteImplementedMethodDefinition(IGeneratedMethodCodeWriter writer, InterfaceMethodMapping mapping)
	{
		MarshalType marshalType = ((!mapping.InterfaceMethod.DeclaringType.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
		MethodReference methodForSignature = mapping.ManagedMethod ?? mapping.InterfaceMethod;
		string signature = ComInterfaceWriter.GetSignature(_context, methodForSignature, mapping.InterfaceMethod, writer.Context.Global.Services.TypeFactory.ResolverFor(methodForSignature.DeclaringType));
		bool preserveSig = mapping.InterfaceMethod.Resolve().IsPreserveSig;
		WriteMethodDefinition(writer, signature, mapping.ManagedMethod, mapping.InterfaceMethod, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			if (mapping.InterfaceMethod.IsStripped)
			{
				if (_hasBaseClass)
				{
					ForwardCallToBaseClassMethod(mapping.InterfaceMethod, mapping.InterfaceMethod, bodyWriter, marshalType, preserveSig);
				}
				else
				{
					if (writer.Context.Global.Parameters.EmitComments)
					{
						bodyWriter.WriteCommentedLine("Managed method has been stripped");
					}
					bodyWriter.WriteLine("return IL2CPP_E_ILLEGAL_METHOD_CALL;");
					_context.Global.Collectors.Stats.RecordStrippedComCallableWrapperMethod();
				}
			}
			else if (mapping.ManagedMethod == null)
			{
				TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(mapping.InterfaceMethod.DeclaringType);
				string value = writer.Context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(mapping.InterfaceMethod);
				string value2 = MethodSignatureWriter.FormatComMethodParameterList(_context, mapping.InterfaceMethod, mapping.InterfaceMethod, typeResolver, marshalType, includeTypeNames: false, preserveSig);
				bodyWriter.AddMethodForwardDeclaration(MethodSignatureWriter.FormatProjectedComCallableWrapperMethodDeclaration(_context, mapping.InterfaceMethod, typeResolver, marshalType));
				if (string.IsNullOrEmpty(value2))
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = bodyWriter;
					generatedMethodCodeWriter.WriteLine($"return {value}(GetManagedObjectInline());");
				}
				else
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = bodyWriter;
					generatedMethodCodeWriter.WriteLine($"return {value}(GetManagedObjectInline(), {value2});");
				}
			}
			else if (!mapping.ManagedMethod.Resolve().DeclaringType.IsComOrWindowsRuntimeType())
			{
				new ComCallableWrapperMethodBodyWriter(writer.Context, mapping.ManagedMethod, mapping.InterfaceMethod, marshalType).WriteMethodBody(bodyWriter, metadataAccess);
				_context.Global.Collectors.Stats.RecordImplementedComCallableWrapperMethod();
			}
			else
			{
				ForwardCallToBaseClassMethod(mapping.InterfaceMethod, mapping.ManagedMethod, bodyWriter, marshalType, preserveSig);
			}
		});
	}

	private void ForwardCallToBaseClassMethod(MethodReference interfaceMethod, MethodReference managedMethod, IGeneratedMethodCodeWriter bodyWriter, MarshalType marshalType, bool preserveSig)
	{
		string thisTypeName = _type.CppNameForVariable;
		string interfaceTypeName = interfaceMethod.DeclaringType.CppName;
		string methodName = interfaceMethod.CppName;
		string parameters = MethodSignatureWriter.FormatComMethodParameterList(_context, managedMethod, interfaceMethod, _context.Global.Services.TypeFactory.ResolverFor(_type), marshalType, includeTypeNames: false, preserveSig);
		bodyWriter.WriteLine($"return il2cpp_codegen_com_query_interface<{interfaceTypeName}>(({thisTypeName})GetManagedObjectInline())->{methodName}({parameters});");
		_context.Global.Collectors.Stats.RecordForwardedToBaseClassComCallableWrapperMethod();
	}

	private void WriteImplementedIReferenceMethodDefinition(IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		string signature = ComInterfaceWriter.GetSignature(_context, method, method, _context.Global.Services.TypeFactory.ResolverFor(_ireferenceOfType));
		WriteMethodDefinition(writer, signature, null, method, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			new IReferenceComCallableWrapperMethodBodyWriter(writer.Context, method, _boxedType).WriteMethodBody(bodyWriter, metadataAccess);
		});
		_context.Global.Collectors.Stats.RecordImplementedComCallableWrapperMethod();
	}

	private void WriteMethodDefinition(IGeneratedMethodCodeWriter writer, string signature, MethodReference managedMethod, MethodReference interfaceMethod, Action<IGeneratedMethodCodeWriter, IRuntimeMetadataAccess> writeAction)
	{
		writer.WriteLine();
		writer.WriteMethodWithMetadataInitialization(signature, writeAction, interfaceMethod.CppName + "_CCW_" + ((managedMethod != null) ? (_typeName + "_" + managedMethod.CppName) : _typeName), managedMethod ?? interfaceMethod);
	}

	private string GetBaseTypeName()
	{
		StringBuilder baseTypeName = new StringBuilder("il2cpp::vm::CachedCCWBase<");
		baseTypeName.Append(_typeName);
		baseTypeName.Append('>');
		foreach (TypeReference itf in _interfacesToImplement)
		{
			baseTypeName.Append(", ");
			baseTypeName.Append(itf.CppName);
		}
		return baseTypeName.ToString();
	}

	private HashSet<TypeReference> GetInterfacesToNotImplement(TypeReference type)
	{
		HashSet<TypeReference> interfacesToNotImplement = new HashSet<TypeReference>();
		if (type.IsArray)
		{
			return interfacesToNotImplement;
		}
		do
		{
			TypeDefinition typeDefinition = type.Resolve();
			TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(type);
			if (typeDefinition.IsComOrWindowsRuntimeType())
			{
				foreach (InterfaceImplementation iface in typeDefinition.Interfaces)
				{
					if (!iface.CustomAttributes.Any((CustomAttribute ca) => ca.AttributeType.Name == "OverridableAttribute" && ca.AttributeType.Namespace == "Windows.Foundation.Metadata"))
					{
						interfacesToNotImplement.Add(typeResolver.Resolve(iface.InterfaceType));
					}
				}
			}
			type = typeResolver.Resolve(typeDefinition.BaseType);
		}
		while (type != null);
		return interfacesToNotImplement;
	}

	private static IEnumerable<TypeReference> GetInterfacesToPotentiallyImplement(IEnumerable<TypeReference> allInteropInterfaces, HashSet<TypeReference> interfacesToNotImplement)
	{
		foreach (TypeReference interfaceType in allInteropInterfaces)
		{
			if (!interfacesToNotImplement.Contains(interfaceType))
			{
				yield return interfaceType;
			}
		}
	}
}
