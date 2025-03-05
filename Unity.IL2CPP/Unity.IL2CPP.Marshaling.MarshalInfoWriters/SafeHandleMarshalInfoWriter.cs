using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class SafeHandleMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private const string SafeHandleReferenceIncrementedLocalBoolNamePrefix = "___safeHandle_reference_incremented_for";

	private readonly TypeDefinition _safeHandleTypeDefinition;

	private readonly MethodDefinition _addRefMethod;

	private readonly MethodDefinition _releaseMethod;

	private readonly MethodDefinition _defaultConstructor;

	private readonly MarshaledType[] _marshaledTypes;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public SafeHandleMarshalInfoWriter(TypeReference type, TypeDefinition safeHandleTypeTypeDefinition)
		: base(type)
	{
		_safeHandleTypeDefinition = safeHandleTypeTypeDefinition;
		_addRefMethod = _safeHandleTypeDefinition.Methods.Single((MethodDefinition method) => method.Name == "DangerousAddRef");
		_releaseMethod = _safeHandleTypeDefinition.Methods.Single((MethodDefinition method) => method.Name == "DangerousRelease");
		_defaultConstructor = _typeRef.Resolve().Methods.SingleOrDefault((MethodDefinition ctor) => ctor.Name == ".ctor" && ctor.Parameters.Count == 0);
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType("void*", "void*")
		};
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} == {"NULL"}) {Emit.RaiseManagedException("il2cpp_codegen_get_argument_null_exception(\"" + (string.IsNullOrEmpty(managedVariableName) ? sourceVariable.GetNiceName(writer.Context) : managedVariableName) + "\")")};");
		EmitCallToDangerousAddRef(writer, sourceVariable.Load(writer.Context), metadataAccess);
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{destinationVariable} = reinterpret_cast<void*>({LoadHandleFieldFor(writer.Context, sourceVariable.Load(writer.Context))});");
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		if (!string.IsNullOrEmpty(managedVariableName))
		{
			writer.WriteLine($"if ({SafeHandleReferenceIncrementedLocalBoolName(writer.Context, managedVariableName)})");
			writer.BeginBlock();
			writer.WriteStatement(Emit.Call(writer.Context, metadataAccess.Method(_releaseMethod), new string[2]
			{
				managedVariableName,
				metadataAccess.HiddenMethodInfo(_releaseMethod)
			}));
			writer.EndBlock();
		}
	}

	public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
	}

	public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		TypeDefinition typeDefinition = _typeRef.Resolve();
		if (typeDefinition.IsAbstract)
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_marshal_directive_exception(\"A returned SafeHandle cannot be abstract, but this type is: '%s'.\", " + metadataAccess.Il2CppTypeFor(typeDefinition) + ")"));
		}
		CustomMarshalInfoWriter.EmitNewObject(writer, _typeRef, unmarshaledVariableName, marshaledVariableName, emitNullCheck: false, metadataAccess);
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		if (forNativeWrapperOfManagedMethod)
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal a SafeHandle from unmanaged to managed.\")"));
			return;
		}
		CustomMarshalInfoWriter.EmitCallToConstructor(writer, _typeRef.Resolve(), _defaultConstructor, destinationVariable, metadataAccess);
		string tempVariableName = destinationVariable.GetNiceName(writer.Context) + "_handle_temp";
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.TypeProvider.SystemIntPtr.CppNameForVariable} {tempVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{tempVariableName} = ({"intptr_t"}){variableName};");
		FieldReference safeHandleField = GetSafeHandleHandleField();
		writer.WriteFieldSetter(safeHandleField, "(" + destinationVariable.Load(writer.Context) + ")->" + safeHandleField.CppName, tempVariableName);
		if (safeHandleShouldEmitAddRef)
		{
			EmitCallToDangerousAddRef(writer, destinationVariable.Load(writer.Context), metadataAccess);
		}
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
	}

	private string LoadHandleFieldFor(ReadOnlyContext context, string sourceVariable)
	{
		return "(" + sourceVariable + ")->" + GetSafeHandleHandleField().CppName;
	}

	private FieldReference GetSafeHandleHandleField()
	{
		return _safeHandleTypeDefinition.Fields.Single((FieldDefinition f) => f.Name == "handle");
	}

	private string SafeHandleReferenceIncrementedLocalBoolName(ReadOnlyContext context, string variableName)
	{
		return "___safeHandle_reference_incremented_for_" + CleanVariableName(context, variableName);
	}

	private void EmitCallToDangerousAddRef(ICodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess)
	{
		string boolName = SafeHandleReferenceIncrementedLocalBoolName(writer.Context, variableName);
		ICodeWriter codeWriter = writer;
		codeWriter.WriteLine($"bool {boolName} = false;");
		codeWriter = writer;
		codeWriter.WriteLine($"{Emit.Call(writer.Context, metadataAccess.Method(_addRefMethod), new string[3]
		{
			variableName,
			Emit.AddressOf(boolName),
			metadataAccess.HiddenMethodInfo(_addRefMethod)
		})};");
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}
}
