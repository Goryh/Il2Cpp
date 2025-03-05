using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal class DateTimeOffsetMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly MarshaledType[] _marshaledTypes;

	private readonly TypeDefinition _windowsFoundationDateTime;

	private readonly TypeDefinition _systemDateTime;

	private readonly FieldDefinition _universalTimeField;

	private readonly FieldDefinition _dateTimeOffsetDateTimeField;

	private readonly FieldDefinition _dateTimeOffsetMinutesField;

	private readonly FieldDefinition _dateTimeDateDataField;

	private const long kTicksBetweenDotNetAndWindowsRuntimeTime = 504911232000000000L;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public DateTimeOffsetMarshalInfoWriter(ReadOnlyContext context, TypeDefinition dateTimeOffset)
		: base(dateTimeOffset)
	{
		_dateTimeOffsetDateTimeField = dateTimeOffset.Fields.Single((FieldDefinition f) => f.Name == "_dateTime");
		_dateTimeOffsetMinutesField = dateTimeOffset.Fields.Single((FieldDefinition f) => f.Name == "_offsetMinutes");
		_systemDateTime = _dateTimeOffsetDateTimeField.FieldType.Resolve();
		_dateTimeDateDataField = _systemDateTime.Fields.Single((FieldDefinition f) => f.Name == "_dateData");
		_windowsFoundationDateTime = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(dateTimeOffset);
		_universalTimeField = _windowsFoundationDateTime.Fields.Single((FieldDefinition f) => f.Name == "UniversalTime");
		string windowsFoundationDateTimeTypeName = _windowsFoundationDateTime.CppNameForVariable;
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(windowsFoundationDateTimeTypeName, windowsFoundationDateTimeTypeName)
		};
	}

	public override void WriteIncludesForFieldDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _windowsFoundationDateTime);
	}

	public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
		writer.AddIncludeForTypeDefinition(writer.Context, _windowsFoundationDateTime);
		writer.AddIncludeForTypeDefinition(writer.Context, _systemDateTime);
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		string universalTimeSetter = _universalTimeField.CppName;
		string dateTimeFieldGetter = _dateTimeOffsetDateTimeField.CppName;
		string dateTimeDataFieldGetter = _dateTimeDateDataField.CppName;
		writer.WriteFieldSetter(_universalTimeField, "(" + destinationVariable + ")." + universalTimeSetter, $"({sourceVariable.Load(writer.Context)}.{dateTimeFieldGetter}.{dateTimeDataFieldGetter} & 0x3FFFFFFFFFFFFFFF) - {504911232000000000L}");
	}

	public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		string universalTimeGetter = _universalTimeField.CppName;
		string dateTimeFieldSetter = _dateTimeOffsetDateTimeField.CppName;
		string minutesFieldSetter = _dateTimeOffsetMinutesField.CppName;
		string dateTimeDataFieldSetter = _dateTimeDateDataField.CppName;
		MethodDefinition toLocalTimeConverter = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToLocalTime" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.Boolean);
		TypeDefinition argumentOutOfRangeException = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentOutOfRangeException);
		MethodDefinition argumentOutOfRangeExceptionConstructor = argumentOutOfRangeException.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
		string stagingDateTimeOffsetVariableName = destinationVariable.GetNiceName(writer.Context) + "Staging";
		string dateTimeOffsetDateTimeVariableName = destinationVariable.GetNiceName(writer.Context) + "DateTime";
		string universalTicksExpression = "(" + variableName + ")." + universalTimeGetter;
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({universalTicksExpression} < {DateTime.MinValue.Ticks - 504911232000000000L} || {universalTicksExpression} > {DateTime.MaxValue.Ticks - 504911232000000000L})");
		using (new BlockWriter(writer))
		{
			string ticksParameterName = metadataAccess.StringLiteral("ticks");
			string message = metadataAccess.StringLiteral("Ticks must be between DateTime.MinValue.Ticks and DateTime.MaxValue.Ticks.");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{argumentOutOfRangeException.CppNameForVariable} exception = {Emit.NewObj(writer.Context, argumentOutOfRangeException, metadataAccess)};");
			writer.WriteMethodCallStatement(metadataAccess, "exception", null, argumentOutOfRangeExceptionConstructor, MethodCallType.Normal, ticksParameterName, message);
			writer.WriteStatement(Emit.RaiseManagedException("exception"));
		}
		writer.WriteLine();
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{_typeRef.CppNameForVariable} {stagingDateTimeOffsetVariableName};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{_systemDateTime.CppNameForVariable} {dateTimeOffsetDateTimeVariableName};");
		writer.WriteFieldSetter(_dateTimeDateDataField, dateTimeOffsetDateTimeVariableName + "." + dateTimeDataFieldSetter, $"{universalTicksExpression} + {504911232000000000L}");
		writer.WriteFieldSetter(_dateTimeDateDataField, stagingDateTimeOffsetVariableName + "." + dateTimeFieldSetter, dateTimeOffsetDateTimeVariableName);
		writer.WriteFieldSetter(_dateTimeDateDataField, stagingDateTimeOffsetVariableName + "." + minutesFieldSetter, "0");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{toLocalTimeConverter.ReturnType.CppNameForVariable} result;");
		writer.WriteMethodCallWithResultStatement(metadataAccess, Emit.AddressOf(stagingDateTimeOffsetVariableName), null, toLocalTimeConverter, MethodCallType.Normal, "result", "true");
		destinationVariable.WriteStore(writer, "result");
	}
}
