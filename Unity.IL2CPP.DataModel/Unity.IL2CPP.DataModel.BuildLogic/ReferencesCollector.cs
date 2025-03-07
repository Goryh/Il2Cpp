using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal static class ReferencesCollector
{
	private class AssemblyCollections
	{
		private readonly HashSet<Mono.Cecil.TypeReference> _types = new HashSet<Mono.Cecil.TypeReference>();

		private readonly HashSet<Mono.Cecil.FieldReference> _fields = new HashSet<Mono.Cecil.FieldReference>();

		private readonly HashSet<Mono.Cecil.MethodReference> _methods = new HashSet<Mono.Cecil.MethodReference>();

		private readonly HashSet<GenericTypeReference> _genericTypeReferences = new HashSet<GenericTypeReference>();

		public bool ExportsOnly { get; }

		public AssemblyCollections(bool exportsOnly)
		{
			ExportsOnly = exportsOnly;
		}

		public bool AddField(Mono.Cecil.FieldReference field)
		{
			if (field is Mono.Cecil.FieldDefinition)
			{
				return false;
			}
			return _fields.Add(field);
		}

		public bool AddType(Mono.Cecil.TypeReference type, Mono.Cecil.IGenericInstance genericInstance)
		{
			if (type is Mono.Cecil.TypeDefinition)
			{
				return false;
			}
			if (genericInstance != null && type.ContainsGenericParameter)
			{
				return _genericTypeReferences.Add(new GenericTypeReference(type, genericInstance));
			}
			return _types.Add(type);
		}

		public void AddGenericParameter(Mono.Cecil.GenericParameter genericParameter, Mono.Cecil.IGenericInstance genericInstance)
		{
			if (genericParameter.Owner.IsDefinition)
			{
				if (genericInstance != null)
				{
					_genericTypeReferences.Add(new GenericTypeReference(genericParameter, genericInstance));
				}
				return;
			}
			if (genericInstance == null)
			{
				if (genericParameter.Type == Mono.Cecil.GenericParameterType.Type)
				{
					if (TypeReferenceEqualityComparer.AreEqual(genericParameter.DeclaringType, genericParameter.DeclaringType.Resolve()))
					{
						_types.Add(genericParameter);
						return;
					}
				}
				else if (genericParameter.Type == Mono.Cecil.GenericParameterType.Method && MethodReferenceComparer.AreEqual(genericParameter.DeclaringMethod, genericParameter.DeclaringMethod.Resolve()))
				{
					_types.Add(genericParameter);
					return;
				}
				throw new InvalidOperationException("Cannot add a generic parameter reference without an instance");
			}
			_genericTypeReferences.Add(new GenericTypeReference(genericParameter, genericInstance));
		}

		public bool AddMethod(Mono.Cecil.MethodReference method)
		{
			if (method is Mono.Cecil.MethodDefinition)
			{
				return false;
			}
			return _methods.Add(method);
		}

		public ReadOnlyHashSet<Mono.Cecil.TypeReference> CompleteTypes()
		{
			return _types.AsReadOnly();
		}

		public ReadOnlyHashSet<Mono.Cecil.FieldReference> CompleteFields()
		{
			return _fields.AsReadOnly();
		}

		public ReadOnlyHashSet<Mono.Cecil.MethodReference> CompleteMethods()
		{
			return _methods.AsReadOnly();
		}

		public ReadOnlyHashSet<GenericTypeReference> CompleteGenericTypeReferences()
		{
			return _genericTypeReferences.AsReadOnly();
		}
	}

	public static ReferenceUsages Collect(Mono.Cecil.AssemblyDefinition assembly, bool exportsOnly)
	{
		AssemblyCollections collections = new AssemblyCollections(exportsOnly);
		ProcessAssembly(collections, assembly);
		return new ReferenceUsages(collections.CompleteTypes(), collections.CompleteFields(), collections.CompleteMethods(), collections.CompleteGenericTypeReferences());
	}

	private static void ProcessAssembly(AssemblyCollections collections, Mono.Cecil.AssemblyDefinition assembly)
	{
		ProcessCustomAttributeProvider(collections, assembly);
		ProcessModule(collections, assembly.MainModule);
	}

	private static void ProcessModule(AssemblyCollections collections, Mono.Cecil.ModuleDefinition module)
	{
		ProcessCustomAttributeProvider(collections, module);
		foreach (Mono.Cecil.TypeDefinition typeDefinition in module.GetAllTypes())
		{
			ProcessType(collections, typeDefinition);
		}
	}

	private static void ProcessType(AssemblyCollections collections, Mono.Cecil.TypeDefinition type)
	{
		ProcessCustomAttributeProvider(collections, type);
		ProcessTypeReference(collections, type.BaseType);
		if (type.HasFields)
		{
			foreach (Mono.Cecil.FieldDefinition field in type.Fields)
			{
				ProcessField(collections, field);
			}
		}
		if (type.HasMethods)
		{
			foreach (Mono.Cecil.MethodDefinition method in type.Methods)
			{
				ProcessMethod(collections, method);
			}
		}
		if (type.HasProperties)
		{
			foreach (Mono.Cecil.PropertyDefinition property in type.Properties)
			{
				ProcessProperty(collections, property);
			}
		}
		if (type.HasEvents)
		{
			foreach (Mono.Cecil.EventDefinition @event in type.Events)
			{
				ProcessEvent(collections, @event);
			}
		}
		if (type.HasInterfaces)
		{
			foreach (Mono.Cecil.InterfaceImplementation iface in type.Interfaces)
			{
				ProcessInterfaceImplementation(collections, iface);
			}
		}
		if (type.HasGenericParameters)
		{
			ProcessGenericParameterProvider(collections, type);
		}
	}

	private static void ProcessField(AssemblyCollections collections, Mono.Cecil.FieldDefinition field)
	{
		ProcessTypeReference(collections, field.FieldType);
		ProcessCustomAttributeProvider(collections, field);
		ProcessMarshalInfoProvider(collections, field);
	}

	private static void ProcessMethod(AssemblyCollections collections, Mono.Cecil.MethodDefinition method)
	{
		ProcessCustomAttributeProvider(collections, method);
		ProcessMethodSignature(collections, method);
		if (method.HasGenericParameters)
		{
			ProcessGenericParameterProvider(collections, method);
		}
		if (method.HasOverrides)
		{
			foreach (Mono.Cecil.MethodReference methodOverride in method.Overrides)
			{
				ProcessMethodReference(collections, methodOverride);
			}
		}
		if (!method.HasBody || collections.ExportsOnly)
		{
			return;
		}
		if (method.Body.HasVariables)
		{
			foreach (Mono.Cecil.Cil.VariableDefinition local in method.Body.Variables)
			{
				ProcessTypeReference(collections, local.VariableType);
			}
		}
		if (method.Body.ThisParameter != null)
		{
			ProcessTypeReference(collections, method.Body.ThisParameter.ParameterType);
		}
		foreach (Mono.Cecil.Cil.Instruction instruction in method.Body.Instructions)
		{
			object operand = instruction.Operand;
			if (!(operand is Mono.Cecil.TypeReference typeReference))
			{
				if (!(operand is Mono.Cecil.MethodReference methodReference))
				{
					if (!(operand is Mono.Cecil.FieldReference fieldReference))
					{
						if (!(operand is ParameterReference parameterReference))
						{
							if (operand is Mono.Cecil.CallSite callSite)
							{
								ProcessMethodSignature(collections, callSite);
							}
						}
						else if (parameterReference.Index == -1)
						{
							ProcessTypeReference(collections, parameterReference.ParameterType);
						}
					}
					else
					{
						ProcessFieldReference(collections, fieldReference);
					}
				}
				else
				{
					ProcessMethodReference(collections, methodReference);
				}
			}
			else
			{
				ProcessTypeReference(collections, typeReference);
			}
		}
		if (!method.Body.HasExceptionHandlers)
		{
			return;
		}
		foreach (Mono.Cecil.Cil.ExceptionHandler exceptionHandler in method.Body.ExceptionHandlers)
		{
			ProcessTypeReference(collections, exceptionHandler.CatchType);
		}
	}

	private static void ProcessMethodSignature(AssemblyCollections collections, Mono.Cecil.IMethodSignature method, Mono.Cecil.IGenericInstance genericInstance = null)
	{
		ProcessTypeReference(collections, method.ReturnType, genericInstance);
		ProcessCustomAttributeProvider(collections, method.MethodReturnType);
		ProcessMarshalInfoProvider(collections, method.MethodReturnType);
		if (!method.HasParameters)
		{
			return;
		}
		foreach (Mono.Cecil.ParameterDefinition param in method.Parameters)
		{
			ProcessCustomAttributeProvider(collections, param);
			ProcessMarshalInfoProvider(collections, param);
			ProcessTypeReference(collections, param.ParameterType, genericInstance);
		}
	}

	private static void ProcessProperty(AssemblyCollections collections, Mono.Cecil.PropertyDefinition property)
	{
		ProcessTypeReference(collections, property.PropertyType);
		ProcessCustomAttributeProvider(collections, property);
	}

	private static void ProcessEvent(AssemblyCollections collections, Mono.Cecil.EventDefinition @event)
	{
		ProcessTypeReference(collections, @event.EventType);
		ProcessCustomAttributeProvider(collections, @event);
	}

	private static void ProcessInterfaceImplementation(AssemblyCollections collections, Mono.Cecil.InterfaceImplementation @interface)
	{
		ProcessTypeReference(collections, @interface.InterfaceType);
		ProcessCustomAttributeProvider(collections, @interface);
	}

	private static void ProcessCustomAttributeProvider(AssemblyCollections collections, Mono.Cecil.ICustomAttributeProvider provider)
	{
		if (!provider.HasCustomAttributes)
		{
			return;
		}
		foreach (Mono.Cecil.CustomAttribute attr in provider.CustomAttributes)
		{
			if (!CustomAttributeSupport.ShouldProcess(attr))
			{
				continue;
			}
			ProcessTypeReference(collections, attr.AttributeType);
			ProcessMethodReference(collections, attr.Constructor);
			if (attr.HasConstructorArguments)
			{
				foreach (Mono.Cecil.CustomAttributeArgument ctorArg in attr.ConstructorArguments)
				{
					ProcessCustomAttributeArgument(collections, ctorArg);
				}
			}
			if (attr.HasFields)
			{
				foreach (Mono.Cecil.CustomAttributeNamedArgument field in attr.Fields)
				{
					ProcessCustomAttributeArgument(collections, field.Argument);
				}
			}
			if (!attr.HasProperties)
			{
				continue;
			}
			foreach (Mono.Cecil.CustomAttributeNamedArgument property in attr.Properties)
			{
				ProcessCustomAttributeArgument(collections, property.Argument);
			}
		}
	}

	private static void ProcessCustomAttributeArgument(AssemblyCollections collections, Mono.Cecil.CustomAttributeArgument argument)
	{
		ProcessTypeReference(collections, argument.Type);
		object value = argument.Value;
		if (!(value is Mono.Cecil.TypeReference t))
		{
			if (!(value is Mono.Cecil.CustomAttributeArgument innerArgument))
			{
				if (value is Mono.Cecil.CustomAttributeArgument[] innerArguments)
				{
					Mono.Cecil.CustomAttributeArgument[] array = innerArguments;
					foreach (Mono.Cecil.CustomAttributeArgument arg in array)
					{
						ProcessCustomAttributeArgument(collections, arg);
					}
				}
			}
			else
			{
				ProcessCustomAttributeArgument(collections, innerArgument);
			}
		}
		else
		{
			ProcessTypeReference(collections, t);
		}
	}

	private static void ProcessMarshalInfoProvider(AssemblyCollections collections, Mono.Cecil.IMarshalInfoProvider marshalInfoProvider)
	{
		if (marshalInfoProvider.HasMarshalInfo && marshalInfoProvider.MarshalInfo is Mono.Cecil.CustomMarshalInfo customMarshalInfo)
		{
			ProcessTypeReference(collections, customMarshalInfo.ManagedType);
		}
	}

	private static void ProcessTypeReference(AssemblyCollections collections, Mono.Cecil.TypeReference typeReference, Mono.Cecil.IGenericInstance genericInstance = null)
	{
		if (typeReference == null)
		{
			return;
		}
		if (typeReference is Mono.Cecil.GenericParameter genericParameter)
		{
			collections.AddGenericParameter(genericParameter, genericInstance);
			ProcessGenericParameter(collections, genericParameter, genericInstance);
		}
		else
		{
			if (!collections.AddType(typeReference, genericInstance))
			{
				return;
			}
			ProcessTypeReference(collections, typeReference.DeclaringType, genericInstance);
			if (!(typeReference is Mono.Cecil.GenericInstanceType genericInstanceType))
			{
				if (!(typeReference is IModifierType modifierType))
				{
					if (!(typeReference is Mono.Cecil.FunctionPointerType functionPointerType))
					{
						if (typeReference is Mono.Cecil.TypeSpecification typeSpecification)
						{
							ProcessTypeReference(collections, typeSpecification.ElementType, genericInstance);
						}
					}
					else
					{
						ProcessMethodSignature(collections, functionPointerType, genericInstance);
					}
				}
				else
				{
					ProcessTypeReference(collections, modifierType.ModifierType, genericInstance);
					ProcessTypeReference(collections, modifierType.ElementType, genericInstance);
				}
			}
			else
			{
				ProcessGenericInstanceType(collections, genericInstanceType, genericInstance);
				ProcessTypeReference(collections, genericInstanceType.ElementType, genericInstance);
			}
		}
	}

	private static void ProcessGenericInstanceType(AssemblyCollections collections, Mono.Cecil.GenericInstanceType genericInstanceType, Mono.Cecil.IGenericInstance genericInstance)
	{
		foreach (Mono.Cecil.TypeReference genericArgument in genericInstanceType.GenericArguments)
		{
			ProcessTypeReference(collections, genericArgument, genericInstance);
		}
	}

	private static void ProcessGenericParameterProvider(AssemblyCollections collections, Mono.Cecil.IGenericParameterProvider genericParamProvider)
	{
		foreach (Mono.Cecil.GenericParameter genericParameter in genericParamProvider.GenericParameters)
		{
			ProcessGenericParameter(collections, genericParameter, null);
		}
	}

	private static void ProcessGenericParameter(AssemblyCollections collections, Mono.Cecil.GenericParameter genericParameter, Mono.Cecil.IGenericInstance genericInstance)
	{
		ProcessCustomAttributeProvider(collections, genericParameter);
		if (!genericParameter.HasConstraints)
		{
			return;
		}
		foreach (Mono.Cecil.GenericParameterConstraint constraint in genericParameter.Constraints)
		{
			ProcessTypeReference(collections, constraint.ConstraintType, genericInstance);
			ProcessCustomAttributeProvider(collections, constraint);
		}
	}

	private static void ProcessMethodReference(AssemblyCollections collections, Mono.Cecil.MethodReference methodReference, Mono.Cecil.IGenericInstance genericInstance = null)
	{
		if (genericInstance == null)
		{
			genericInstance = (methodReference as Mono.Cecil.IGenericInstance) ?? (methodReference.DeclaringType as Mono.Cecil.IGenericInstance);
		}
		if (!collections.AddMethod(methodReference))
		{
			return;
		}
		if (methodReference is Mono.Cecil.GenericInstanceMethod genericInstanceMethod)
		{
			foreach (Mono.Cecil.TypeReference genericArgument in genericInstanceMethod.GenericArguments)
			{
				ProcessTypeReference(collections, genericArgument);
			}
		}
		if (methodReference.HasGenericParameters)
		{
			foreach (Mono.Cecil.GenericParameter genericParameter in methodReference.GenericParameters)
			{
				ProcessTypeReference(collections, genericParameter, genericInstance);
			}
		}
		ProcessTypeReference(collections, methodReference.DeclaringType);
		ProcessTypeReference(collections, methodReference.ReturnType, genericInstance);
		if (!methodReference.HasParameters)
		{
			return;
		}
		foreach (Mono.Cecil.ParameterDefinition parameter in methodReference.Parameters)
		{
			ProcessTypeReference(collections, parameter.ParameterType, genericInstance);
		}
	}

	private static void ProcessFieldReference(AssemblyCollections collections, Mono.Cecil.FieldReference fieldReference)
	{
		if (collections.AddField(fieldReference))
		{
			Mono.Cecil.GenericInstanceType genericInstance = fieldReference.DeclaringType as Mono.Cecil.GenericInstanceType;
			ProcessTypeReference(collections, fieldReference.DeclaringType);
			ProcessTypeReference(collections, fieldReference.FieldType, genericInstance);
		}
	}
}
