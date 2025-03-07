using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.Modify.Builders;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public class EditContext
{
	public readonly TypeContext Context;

	internal EditContext(TypeContext context)
	{
		Context = context;
	}

	public FieldDefinitionBuilder BuildField(string fieldName, FieldAttributes attributes, TypeReference fieldType)
	{
		return new FieldDefinitionBuilder(this, fieldName, attributes, fieldType);
	}

	public TypeDefinitionBuilder BuildClass(string @namespace, string name, TypeAttributes attributes)
	{
		return BuildType(@namespace, name, attributes, Context.GetSystemType(SystemType.Object), MetadataType.Class);
	}

	public TypeDefinitionBuilder BuildStruct(string @namespace, string name, TypeAttributes attributes)
	{
		return BuildType(@namespace, name, attributes, Context.GetSystemType(SystemType.ValueType), MetadataType.ValueType);
	}

	public TypeDefinitionBuilder BuildType(string @namespace, string name, TypeAttributes attributes, TypeReference baseType, MetadataType metadataType)
	{
		return new TypeDefinitionBuilder(this, @namespace, name, attributes, baseType, metadataType);
	}

	public MethodDefinitionBuilder BuildMethod(string methodName, MethodAttributes attributes)
	{
		return BuildMethod(methodName, attributes, Context.GetSystemType(SystemType.Void));
	}

	public MethodDefinitionBuilder BuildMethod(string methodName, MethodAttributes attributes, TypeReference returnType)
	{
		return new MethodDefinitionBuilder(this, methodName, attributes, returnType);
	}

	public EventDefinitionBuilder BuildEvent(string name, EventAttributes attributes, TypeReference eventType)
	{
		return new EventDefinitionBuilder(this, name, attributes, eventType);
	}

	public PropertyDefinitionBuilder BuildProperty(string name, PropertyAttributes attributes, TypeReference propertyType)
	{
		return new PropertyDefinitionBuilder(this, name, attributes, propertyType);
	}

	public FunctionPointerBuilder BuildFunctionPointer(TypeReference returnType, MethodCallingConvention callingConvention = MethodCallingConvention.Default)
	{
		return new FunctionPointerBuilder(this, returnType, callingConvention);
	}

	public void ChangeAttributes(MethodDefinition method, MethodAttributes attributes)
	{
		((IMethodDefinitionUpdater)method).UpdateAttributes(attributes);
	}

	public void ChangeInstructionOperand(Instruction ins, object operand)
	{
		((IInstructionUpdater)ins).UpdateOperand(operand);
	}

	public VariableDefinition AddVariableToMethod(ILProcessor ilProcessor, TypeReference variableType)
	{
		return AddVariableToMethod(ilProcessor.Body.Method, variableType);
	}

	public void AddInterfaceImplementationToType(TypeDefinition type, TypeReference interfaceType)
	{
		AddInterfaceImplementationToType(type, interfaceType, out var _);
	}

	public void AddInterfaceImplementationToType(TypeDefinition type, TypeReference interfaceType, out ReadOnlyCollection<MethodDefinition> generatedMethods)
	{
		HashSet<TypeReference> interfacesToAdd = new HashSet<TypeReference>();
		CollectInterfaceTypesToAdd(type, interfaceType, interfacesToAdd);
		List<InterfaceImplementation> newInterfaceImplementations = new List<InterfaceImplementation>();
		List<MethodDefinition> allGeneratedMethods = new List<MethodDefinition>();
		foreach (TypeReference iface in interfacesToAdd)
		{
			InterfaceImplementation newInterfaceImplementation = new InterfaceImplementation(ReadOnlyCollectionCache<CustomAttribute>.Empty, type.Assembly.IssueNewInterfaceImplementationToken());
			newInterfaceImplementation.InitializeInterfaceType(iface);
			newInterfaceImplementations.Add(newInterfaceImplementation);
			AddInterfaceMembersToType(type, iface, out var genMethods);
			allGeneratedMethods.AddRange(genMethods);
		}
		generatedMethods = allGeneratedMethods.AsReadOnly();
		((ITypeDefinitionUpdater)type).AddInterfaceImplementations((IEnumerable<InterfaceImplementation>)newInterfaceImplementations);
		((ITypeReferenceUpdater)type).ClearInterfaceTypesCache();
	}

	public VariableDefinition AddVariableToMethod(MethodDefinition method, TypeReference variableType)
	{
		return method.Body.AddVariable(variableType);
	}

	public ExceptionHandler AddExceptionHandlerToMethod(ILProcessor ilProcessor, TypeReference catchType, ExceptionHandlerType handlerType, Instruction tryStart, Instruction tryEnd, Instruction filterStart, Instruction handlerStart, Instruction handlerEnd)
	{
		return AddExceptionHandlerToMethod(ilProcessor.Body.Method, catchType, handlerType, tryStart, tryEnd, filterStart, handlerStart, handlerEnd);
	}

	public ExceptionHandler AddExceptionHandlerToMethod(MethodDefinition method, TypeReference catchType, ExceptionHandlerType handlerType, Instruction tryStart, Instruction tryEnd, Instruction filterStart, Instruction handlerStart, Instruction handlerEnd)
	{
		return method.Body.AddExceptionHandler(catchType, handlerType, tryStart, tryEnd, filterStart, handlerStart, handlerEnd);
	}

	private void CollectInterfaceTypesToAdd(TypeDefinition type, TypeReference interfaceType, HashSet<TypeReference> newInterfaces)
	{
		if (type.Interfaces.Any((InterfaceImplementation i) => i.InterfaceType == interfaceType) || newInterfaces.Contains(interfaceType))
		{
			return;
		}
		newInterfaces.Add(interfaceType);
		TypeResolver typeResolver = new TypeResolver(interfaceType as GenericInstanceType, null, Context, Context.CreateThreadSafeFactoryForFullConstruction());
		foreach (InterfaceImplementation iface in interfaceType.Resolve().Interfaces)
		{
			CollectInterfaceTypesToAdd(type, typeResolver.Resolve(iface.InterfaceType), newInterfaces);
		}
	}

	private void AddInterfaceMembersToType(TypeDefinition type, TypeReference interfaceType, out ReadOnlyCollection<MethodDefinition> generatedMethods)
	{
		TypeResolver typeResolver = new TypeResolver(interfaceType as GenericInstanceType, null, Context, Context.CreateThreadSafeFactoryForFullConstruction());
		Dictionary<MethodDefinition, MethodDefinition> addedMethods = new Dictionary<MethodDefinition, MethodDefinition>();
		TypeDefinition interfaceTypeDef = interfaceType.Resolve();
		foreach (MethodDefinition interfaceMethodDef in interfaceTypeDef.Methods.Where((MethodDefinition m) => !m.IsStripped))
		{
			MethodReference interfaceMethod = typeResolver.Resolve(interfaceMethodDef);
			TypeReference returnType = typeResolver.Resolve(interfaceMethod.ReturnType);
			MethodDefinition newMethod = BuildMethod(interfaceTypeDef.FullName + "." + interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask | MethodAttributes.Abstract, returnType).WithParametersClonedFrom(interfaceMethodDef, typeResolver).WithOverride(interfaceMethod).WithEmptyBody()
				.CompleteWithoutUpdatingInflations(type);
			addedMethods.Add(interfaceMethodDef, newMethod);
		}
		generatedMethods = addedMethods.Values.ToArray().AsReadOnly();
		foreach (PropertyDefinition interfaceProperty in interfaceTypeDef.Properties)
		{
			TypeReference propertyType = typeResolver.Resolve(interfaceProperty.PropertyType);
			PropertyDefinitionBuilder builder = BuildProperty(interfaceType.FullName + "." + interfaceProperty.Name, interfaceProperty.Attributes, propertyType);
			if (interfaceProperty.GetMethod != null)
			{
				builder.WithGetMethod(addedMethods[interfaceProperty.GetMethod]);
			}
			if (interfaceProperty.SetMethod != null)
			{
				builder.WithSetMethod(addedMethods[interfaceProperty.SetMethod]);
			}
			builder.Complete(type);
		}
		foreach (EventDefinition interfaceEvent in interfaceTypeDef.Events)
		{
			TypeReference eventType = typeResolver.Resolve(interfaceEvent.EventType);
			EventDefinitionBuilder builder2 = BuildEvent(interfaceType.FullName + "." + interfaceEvent.Name, interfaceEvent.Attributes, eventType);
			if (interfaceEvent.AddMethod != null)
			{
				builder2.WithAddMethod(addedMethods[interfaceEvent.AddMethod]);
			}
			if (interfaceEvent.RemoveMethod != null)
			{
				builder2.WithRemoveMethod(addedMethods[interfaceEvent.RemoveMethod]);
			}
			if (interfaceEvent.InvokeMethod != null)
			{
				builder2.WithInvokeMethod(addedMethods[interfaceEvent.InvokeMethod]);
			}
			foreach (MethodDefinition otherMethod in interfaceEvent.OtherMethods)
			{
				builder2.WithOtherMethod(addedMethods[otherMethod]);
			}
			builder2.Complete(type);
		}
		UpdateInflatedInstances(type);
	}

	private void UpdateInflatedInstances(TypeDefinition modifiedType)
	{
		foreach (TypeReference inflatedType in Context.AllKnownNonDefinitionTypesUnordered())
		{
			if (inflatedType.Resolve() == modifiedType)
			{
				((ITypeReferenceUpdater)inflatedType).ClearMethodsCache();
				((ITypeReferenceUpdater)inflatedType).ClearInterfaceTypesCache();
			}
		}
	}
}
