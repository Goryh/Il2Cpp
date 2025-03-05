using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public abstract class DefaultMarshalInfoWriter
{
	protected readonly TypeReference _typeRef;

	public virtual string MarshalToNativeFunctionName => "NULL";

	public virtual string MarshalFromNativeFunctionName => "NULL";

	public virtual string MarshalCleanupFunctionName => "NULL";

	public virtual bool HasNativeStructDefinition => false;

	public virtual int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		return 0;
	}

	public virtual string GetNativeSize(ReadOnlyContext context)
	{
		return ComputeNativeSize(GetMarshaledTypes(context));
	}

	protected static string ComputeNativeSize(MarshaledType[] marshalTypes)
	{
		if (marshalTypes.Length == 1)
		{
			return "sizeof(" + marshalTypes[0].Name + ")";
		}
		return marshalTypes.Select((MarshaledType t) => "sizeof(" + t.Name + ")").Aggregate((string x, string y) => x + " + " + y);
	}

	public abstract MarshaledType[] GetMarshaledTypes(ReadOnlyContext context);

	public DefaultMarshalInfoWriter(TypeReference type)
	{
		_typeRef = type;
	}

	public virtual void WriteNativeStructDefinition(IReadOnlyContextGeneratedCodeWriter writer)
	{
	}

	public virtual void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
	{
	}

	public virtual void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
	{
	}

	public virtual bool WillWriteMarshalFunctionDefinitions()
	{
		return false;
	}

	public virtual void WriteFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
	{
		MarshaledType[] marshaledTypes = GetMarshaledTypes(writer.Context);
		foreach (MarshaledType type in marshaledTypes)
		{
			string fieldName = field.CppName + type.VariableName + fieldNameSuffix;
			writer.WriteLine($"{type.DecoratedName} {fieldName};");
		}
	}

	public virtual void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		if (TreatAsValueType())
		{
			writer.AddIncludesForTypeReference(writer.Context, _typeRef, requiresCompleteType: true);
		}
		else
		{
			WriteMarshaledTypeForwardDeclaration(writer);
		}
	}

	public virtual void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		if (!_typeRef.IsEnum && !_typeRef.IsSystemObject)
		{
			MarshaledType[] marshaledTypes = GetMarshaledTypes(writer.Context);
			foreach (MarshaledType type in marshaledTypes)
			{
				writer.AddForwardDeclaration("struct " + type.Name);
			}
		}
	}

	public virtual void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
	}

	public virtual void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine($"{destinationVariable} = {sourceVariable.Load(writer.Context)};");
	}

	public virtual string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		return variableName.Load(writer.Context);
	}

	public virtual void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		destinationVariable.WriteStore(writer, variableName);
	}

	public virtual void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
	{
		WriteMarshalVariableFromNative(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
	}

	public virtual string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		return sourceVariable.Load(writer.Context);
	}

	public virtual string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
		string emptyVariableName = "_" + CleanVariableName(writer.Context, variableName) + "_empty";
		writer.WriteVariable(writer.Context, _typeRef, emptyVariableName);
		return emptyVariableName;
	}

	public virtual void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
	{
	}

	public virtual string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
	{
		return WriteMarshalVariableToNative(writer, sourceVariable, null, metadataAccess);
	}

	public virtual string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		return variableName;
	}

	public virtual void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
	}

	public virtual void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		WriteMarshalCleanupVariable(writer, variableName, metadataAccess, managedVariableName);
	}

	public virtual void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteVariable(writer.Context, _typeRef, unmarshaledVariableName);
	}

	public virtual string DecorateVariable(ReadOnlyContext context, string unmarshaledParameterName, string marshaledVariableName)
	{
		return marshaledVariableName;
	}

	public virtual string UndecorateVariable(ReadOnlyContext context, string variableName)
	{
		return variableName;
	}

	public virtual bool CanMarshalTypeToNative(ReadOnlyContext context)
	{
		return true;
	}

	public virtual bool CanMarshalTypeFromNative(ReadOnlyContext context)
	{
		return CanMarshalTypeToNative(context);
	}

	public virtual bool CanMarshalTypeFromNativeAsReturnValue(ReadOnlyContext context)
	{
		return CanMarshalTypeFromNative(context);
	}

	public virtual string GetMarshalingException(ReadOnlyContext context, IRuntimeMetadataAccess metadataAccess)
	{
		throw new NotSupportedException($"Cannot retrieve marshaling exception for type ({_typeRef}) that can be marshaled.");
	}

	public virtual void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
	{
		MarshaledType[] marshaledTypes = GetMarshaledTypes(writer.Context);
		foreach (MarshaledType type in marshaledTypes)
		{
			string defaultValue = ((!type.Name.EndsWith("*") && !(type.Name == "Il2CppHString")) ? ((_typeRef.MetadataType == MetadataType.Class && !_typeRef.DerivesFromObject(writer.Context)) ? (type.Name + "()") : ((!_typeRef.MetadataType.IsPrimitiveType()) ? ((!type.Name.IsPrimitiveCppType()) ? "{}" : GeneratedCodeWriterExtensions.InitializerStringForPrimitiveCppType(type.Name)) : GeneratedCodeWriterExtensions.InitializerStringForPrimitiveType(_typeRef.MetadataType))) : "NULL");
			CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 3, writer);
			left.AppendFormatted(type.Name);
			left.AppendLiteral(" ");
			left.AppendFormatted(variableName);
			left.AppendFormatted(type.VariableName);
			writer.WriteAssignStatement(ref left, defaultValue);
		}
	}

	public virtual bool TreatAsValueType()
	{
		return _typeRef.IsValueType;
	}

	protected string CleanVariableName(ReadOnlyContext context, string variableName)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		foreach (char c in variableName)
		{
			switch (c)
			{
			case '.':
			case '[':
			case ']':
				builder.Append('_');
				break;
			default:
				builder.AppendClean(c);
				break;
			case '(':
			case ')':
			case '*':
				break;
			}
		}
		return builder.ToString();
	}
}
