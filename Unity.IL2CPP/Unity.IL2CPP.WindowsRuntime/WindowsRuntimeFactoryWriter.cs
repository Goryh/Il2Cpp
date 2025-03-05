using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

internal class WindowsRuntimeFactoryWriter : CCWWriterBase
{
	private readonly string _typeName;

	private readonly MethodDefinition _parameterlessConstructor;

	private readonly List<TypeReference> _interfacesToImplement = new List<TypeReference>();

	private readonly List<(MethodDefinition, MethodReference)> _methodMappings = new List<(MethodDefinition, MethodReference)>();

	protected override IEnumerable<TypeReference> AllImplementedInterfaces => _interfacesToImplement;

	protected override bool ImplementsAnyIInspectableInterfaces => true;

	protected override bool IsManagedObjectHolder => false;

	public WindowsRuntimeFactoryWriter(SourceWritingContext context, TypeDefinition type)
		: base(context, type)
	{
		_typeName = _context.Global.Services.Naming.ForWindowsRuntimeFactory(type);
		List<TypeReference> staticFactories = new List<TypeReference>();
		List<TypeReference> activationFactories = new List<TypeReference>();
		foreach (CustomAttribute customAttribute in type.CustomAttributes)
		{
			TypeReference attributeType = customAttribute.AttributeType;
			if (!(attributeType.Namespace != "Windows.Foundation.Metadata") && customAttribute.ConstructorArguments.Count != 0 && customAttribute.ConstructorArguments[0].Value is TypeReference factoryInterface)
			{
				if (attributeType.Name == "StaticAttribute")
				{
					staticFactories.Add(factoryInterface);
				}
				else if (attributeType.Name == "ActivatableAttribute")
				{
					activationFactories.Add(factoryInterface);
				}
			}
		}
		_interfacesToImplement.Add(_context.Global.Services.TypeProvider.IActivationFactoryTypeReference);
		_interfacesToImplement.AddRange(activationFactories);
		_interfacesToImplement.AddRange(staticFactories);
		foreach (MethodDefinition method in type.Methods)
		{
			if (!method.IsPublic)
			{
				continue;
			}
			MethodReference interfaceMethod;
			if (method.IsConstructor)
			{
				if (method.Parameters.Count == 0)
				{
					_parameterlessConstructor = method;
					continue;
				}
				interfaceMethod = method.GetFactoryMethodForConstructor(activationFactories, isComposing: false);
			}
			else
			{
				if (method.HasThis)
				{
					continue;
				}
				interfaceMethod = method.GetOverriddenInterfaceMethod(context, staticFactories);
			}
			if (interfaceMethod != null)
			{
				_methodMappings.Add((method, interfaceMethod));
			}
		}
	}

	public override void Write(IGeneratedMethodCodeWriter writer)
	{
		writer.AddInclude("vm/ActivationFactoryBase.h");
		AddIncludes(writer);
		string baseTypeName = GetBaseTypeName();
		writer.WriteLine();
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("Factory for " + _type.FullName);
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"struct {_typeName} IL2CPP_FINAL : {baseTypeName}");
		using (new BlockWriter(writer, semicolon: true))
		{
			if (_parameterlessConstructor != null)
			{
				WriteIActivationFactoryImplementation(writer);
			}
			foreach (var mapping in _methodMappings)
			{
				var (managedMethod, interfaceMethod) = mapping;
				if (writer.Context.Global.Parameters.EmitComments)
				{
					writer.WriteCommentedLine("Native wrapper method for " + (managedMethod ?? interfaceMethod).FullName);
				}
				string signature = ComInterfaceWriter.GetSignature(writer.Context, interfaceMethod, interfaceMethod, writer.Context.Global.Services.TypeFactory.EmptyResolver());
				string methodName = interfaceMethod.CppName;
				writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					if (managedMethod != null)
					{
						GetMethodWriter(writer.Context, managedMethod, interfaceMethod).WriteMethodBody(bodyWriter, metadataAccess);
					}
					else
					{
						bodyWriter.WriteLine("return IL2CPP_E_NOTIMPL;");
					}
				}, methodName, interfaceMethod);
			}
			WriteCommonInterfaceMethods(writer);
		}
	}

	private void WriteIActivationFactoryImplementation(IGeneratedMethodCodeWriter writer)
	{
		INamingService naming = writer.Context.Global.Services.Naming;
		string signature = "virtual il2cpp_hresult_t STDCALL ActivateInstance(Il2CppIInspectable** " + naming.ForComInterfaceReturnParameterName() + ") IL2CPP_OVERRIDE";
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference iActivationFactoryTypeReference = writer.Context.Global.Services.TypeProvider.IActivationFactoryTypeReference;
			GetMethodWriter(writer.Context, _parameterlessConstructor, iActivationFactoryTypeReference.Resolve().Methods.Single()).WriteMethodBody(bodyWriter, metadataAccess);
		}, _typeName + "_ActivateInstance", null);
	}

	private static InteropMethodBodyWriter GetMethodWriter(ReadOnlyContext context, MethodDefinition managedMethod, MethodReference interfaceMethod)
	{
		if (managedMethod.IsConstructor)
		{
			return new ConstructorFactoryMethodBodyWriter(context, managedMethod, interfaceMethod);
		}
		return new StaticFactoryMethodBodyWriter(context, managedMethod, interfaceMethod);
	}

	private string GetBaseTypeName()
	{
		StringBuilder baseTypeName = new StringBuilder("il2cpp::vm::ActivationFactoryBase<");
		baseTypeName.Append(_typeName);
		baseTypeName.Append('>');
		foreach (TypeReference itf in AllImplementedInterfaces)
		{
			if (!itf.Is(Il2CppCustomType.IActivationFactory))
			{
				baseTypeName.Append(", ");
				baseTypeName.Append(itf.CppName);
			}
		}
		return baseTypeName.ToString();
	}

	public override void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"return static_cast<Il2CppIActivationFactory*>({_typeName}::__CreateInstance());");
	}
}
