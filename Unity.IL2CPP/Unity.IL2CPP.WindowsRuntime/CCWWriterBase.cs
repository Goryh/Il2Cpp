using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

public abstract class CCWWriterBase : ICCWWriter
{
	private static readonly Guid IID_IMarshal = new Guid(3, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	protected readonly TypeReference _type;

	protected readonly SourceWritingContext _context;

	protected virtual bool ImplementsAnyIInspectableInterfaces => false;

	protected virtual bool HasBaseClass => false;

	protected virtual IList<TypeReference> InterfacesToForwardToBaseClass => new TypeReference[0];

	protected abstract IEnumerable<TypeReference> AllImplementedInterfaces { get; }

	private IEnumerable<TypeReference> AllInteropInterfaces => AllImplementedInterfaces.Concat(InterfacesToForwardToBaseClass);

	protected virtual IEnumerable<string> AllQueryableInterfaceNames => AllImplementedInterfaces.Select((TypeReference t) => t.CppName);

	protected virtual bool IsManagedObjectHolder => true;

	protected CCWWriterBase(SourceWritingContext context, TypeReference type)
	{
		_context = context;
		_type = type;
	}

	protected void AddIncludes(IGeneratedCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _type);
		foreach (TypeReference iface in AllImplementedInterfaces)
		{
			writer.AddIncludeForTypeDefinition(writer.Context, iface);
		}
	}

	public abstract void Write(IGeneratedMethodCodeWriter writer);

	public virtual void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string className = _context.Global.Services.Naming.ForComCallableWrapperClass(_type);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"void* memory = il2cpp::utils::Memory::Malloc(sizeof({className}));");
		writer.WriteLine("if (memory == NULL)");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("il2cpp_codegen_raise_out_of_memory_exception();");
		}
		writer.WriteLine();
		writer.AddInclude("utils/New.h");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"return static_cast<Il2CppIManagedObjectHolder*>(new(memory) {className}(obj));");
	}

	protected void WriteCommonInterfaceMethods(IGeneratedMethodCodeWriter writer)
	{
		WriteQueryInterfaceDefinition(writer);
		WriteAddRefDefinition(writer);
		WriteReleaseDefinition(writer);
		WriteGetIidsDefinition(writer);
		if (ImplementsAnyIInspectableInterfaces)
		{
			WriteGetRuntimeClassNameDefinition(writer);
			WriteGetTrustLevelDefinition(writer);
		}
	}

	private void WriteQueryInterfaceDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("virtual il2cpp_hresult_t STDCALL QueryInterface(const Il2CppGuid& iid, void** object) IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			bool implementsIMarshal = false;
			foreach (TypeReference iface in AllImplementedInterfaces)
			{
				if (!iface.Is(Il2CppCustomType.IActivationFactory) && iface.GetGuid(_context) == IID_IMarshal)
				{
					implementsIMarshal = true;
					break;
				}
			}
			writer.WriteLine("if (::memcmp(&iid, &Il2CppIUnknown::IID, sizeof(Il2CppGuid)) == 0");
			writer.Write(" || ::memcmp(&iid, &Il2CppIInspectable::IID, sizeof(Il2CppGuid)) == 0");
			if (!implementsIMarshal)
			{
				writer.WriteLine();
				writer.WriteLine(" || ::memcmp(&iid, &Il2CppIAgileObject::IID, sizeof(Il2CppGuid)) == 0)");
			}
			else
			{
				writer.WriteLine(")");
			}
			using (new BlockWriter(writer))
			{
				writer.WriteLine("*object = GetIdentity();");
				writer.WriteLine("AddRefImpl();");
				writer.WriteLine("return IL2CPP_S_OK;");
			}
			writer.WriteLine();
			if (IsManagedObjectHolder)
			{
				WriteQueryInterfaceForInterface(writer, "Il2CppIManagedObjectHolder");
			}
			foreach (string iface2 in AllQueryableInterfaceNames)
			{
				WriteQueryInterfaceForInterface(writer, iface2);
			}
			if (!implementsIMarshal)
			{
				WriteQueryInterfaceForInterface(writer, "Il2CppIMarshal");
			}
			if (IsManagedObjectHolder)
			{
				WriteQueryInterfaceForInterface(writer, "Il2CppIWeakReferenceSource");
			}
			if (HasBaseClass)
			{
				string managedTypeName = _type.CppNameForVariable;
				string identityField = _context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
				writer.WriteLine($"return (({managedTypeName})GetManagedObjectInline())->{identityField}->QueryInterface(iid, object);");
			}
			else
			{
				writer.WriteLine("*object = NULL;");
				writer.WriteLine("return IL2CPP_E_NOINTERFACE;");
			}
		}
	}

	private static void WriteQueryInterfaceForInterface(IGeneratedMethodCodeWriter writer, string interfaceName)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if (::memcmp(&iid, &{interfaceName}::IID, sizeof(Il2CppGuid)) == 0)");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"*object = static_cast<{interfaceName}*>(this);");
			writer.WriteLine("AddRefImpl();");
			writer.WriteLine("return IL2CPP_S_OK;");
		}
		writer.WriteLine();
	}

	private void WriteAddRefDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("virtual uint32_t STDCALL AddRef() IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("return AddRefImpl();");
		}
	}

	private void WriteReleaseDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("virtual uint32_t STDCALL Release() IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("return ReleaseImpl();");
		}
	}

	private void WriteGetIidsDefinition(IGeneratedMethodCodeWriter writer)
	{
		int interfaceCount = 0;
		foreach (TypeReference allInteropInterface in AllInteropInterfaces)
		{
			if (allInteropInterface.Resolve().IsExposedToWindowsRuntime())
			{
				interfaceCount++;
			}
		}
		if (!ImplementsAnyIInspectableInterfaces && interfaceCount == 0)
		{
			return;
		}
		writer.WriteLine();
		writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetIids(uint32_t* iidCount, Il2CppGuid** iids) IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			if (interfaceCount > 0)
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"Il2CppGuid* interfaceIds = il2cpp_codegen_marshal_allocate_array<Il2CppGuid>({interfaceCount});");
				int interfaceIndex = 0;
				foreach (TypeReference iface in AllInteropInterfaces)
				{
					if (iface.Resolve().IsExposedToWindowsRuntime())
					{
						string interfaceName = iface.CppName;
						writer.AddIncludeForTypeDefinition(writer.Context, iface);
						generatedMethodCodeWriter = writer;
						generatedMethodCodeWriter.WriteLine($"interfaceIds[{interfaceIndex}] = {interfaceName}::IID;");
						interfaceIndex++;
					}
				}
				writer.WriteLine();
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"*iidCount = {interfaceCount};");
				writer.WriteLine("*iids = interfaceIds;");
				writer.WriteLine("return IL2CPP_S_OK;");
			}
			else
			{
				writer.WriteLine("return ComObjectBase::GetIids(iidCount, iids);");
			}
		}
	}

	private void WriteGetRuntimeClassNameDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetRuntimeClassName(Il2CppHString* className) IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("return GetRuntimeClassNameImpl(className);");
		}
	}

	private void WriteGetTrustLevelDefinition(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetTrustLevel(int32_t* trustLevel) IL2CPP_OVERRIDE");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("return ComObjectBase::GetTrustLevel(trustLevel);");
		}
	}
}
