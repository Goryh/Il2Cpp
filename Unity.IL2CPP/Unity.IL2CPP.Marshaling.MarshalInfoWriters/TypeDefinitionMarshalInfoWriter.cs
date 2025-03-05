using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class TypeDefinitionMarshalInfoWriter : CustomMarshalInfoWriter
{
	public override int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		int sum = 0;
		DefaultMarshalInfoWriter[] fieldMarshalInfoWriters = GetFieldMarshalInfoWriters(context);
		foreach (DefaultMarshalInfoWriter marshalInfoWriter in fieldMarshalInfoWriters)
		{
			sum += marshalInfoWriter.GetNativeSizeWithoutPointers(context);
		}
		return sum;
	}

	public TypeDefinitionMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
		: base(type, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper)
	{
	}

	protected override void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_ToNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalToNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < GetFields(writer.Context).Length; i++)
			{
				GetFieldMarshalInfoWriters(writer.Context)[i].WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue("unmarshaled", GetFields(writer.Context)[i]), GetFieldMarshalInfoWriters(writer.Context)[i].UndecorateVariable(writer.Context, "marshaled." + GetFields(writer.Context)[i].CppName), null, metadataAccess);
			}
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_FromNativeMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalFromNativeFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < GetFields(writer.Context).Length; i++)
			{
				FieldDefinition fieldDefinition = GetFields(writer.Context)[i];
				ManagedMarshalValue destinationVariable = new ManagedMarshalValue("unmarshaled", fieldDefinition);
				if (!fieldDefinition.FieldType.IsValueType)
				{
					GetFieldMarshalInfoWriters(writer.Context)[i].WriteMarshalVariableFromNative(bodyWriter, GetFieldMarshalInfoWriters(writer.Context)[i].UndecorateVariable(writer.Context, "marshaled." + fieldDefinition.CppName), destinationVariable, null, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
				}
				else
				{
					string text = destinationVariable.GetNiceName(writer.Context) + "_temp_" + i;
					bodyWriter.WriteVariable(writer.Context, fieldDefinition.FieldType, text);
					GetFieldMarshalInfoWriters(writer.Context)[i].WriteMarshalVariableFromNative(bodyWriter, "marshaled." + fieldDefinition.CppName, new ManagedMarshalValue(text), null, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
					destinationVariable.WriteStore(bodyWriter, text);
				}
			}
		}, metadataAccessID, null);
	}

	protected override void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer)
	{
		string metadataAccessID = writer.Context.Global.Services.Naming.ForType(_type) + "_" + MarshalingUtils.MarshalTypeToString(_marshalType) + "_MarshalCleanupMethodDefinition";
		writer.WriteMethodWithMetadataInitialization(MarshalCleanupFunctionDeclaration(), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < GetFields(writer.Context).Length; i++)
			{
				GetFieldMarshalInfoWriters(writer.Context)[i].WriteMarshalCleanupVariable(bodyWriter, GetFieldMarshalInfoWriters(writer.Context)[i].UndecorateVariable(writer.Context, "marshaled." + GetFields(writer.Context)[i].CppName), metadataAccess);
			}
		}, metadataAccessID, null);
	}
}
