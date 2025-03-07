using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

internal static class Formatter
{
	public static string FormatInstruction(Instruction instruction)
	{
		StringWriter stringWriter = new StringWriter();
		WriteInstruction(stringWriter, instruction);
		return stringWriter.ToString();
	}

	public static void WriteInstruction(TextWriter writer, Instruction instruction)
	{
		writer.Write(FormatLabel(instruction.Offset));
		writer.Write(": ");
		writer.Write(instruction.OpCode.Name);
		if (instruction.Operand != null)
		{
			writer.Write(' ');
			WriteOperand(writer, instruction.Operand);
		}
	}

	private static string FormatLabel(int offset)
	{
		string label = "000" + offset.ToString("x");
		return "IL_" + label.Substring(label.Length - 4);
	}

	private static void WriteOperand(TextWriter writer, object operand)
	{
		if (operand == null)
		{
			throw new ArgumentNullException("operand");
		}
		if (operand is Instruction targetInstruction)
		{
			writer.Write(FormatLabel(targetInstruction.Offset));
			return;
		}
		if (operand is Instruction[] targetInstructions)
		{
			WriteLabelList(writer, targetInstructions);
			return;
		}
		if (operand is VariableDefinition variableRef)
		{
			writer.Write(variableRef.Index.ToString());
			return;
		}
		if (operand is MethodReference methodRef)
		{
			WriteMethodReference(writer, methodRef);
			return;
		}
		if (operand is string s)
		{
			writer.Write("\"" + s + "\"");
			return;
		}
		string s2 = ToInvariantCultureString(operand);
		writer.Write(s2);
	}

	private static void WriteLabelList(TextWriter writer, Instruction[] instructions)
	{
		writer.Write("(");
		for (int i = 0; i < instructions.Length; i++)
		{
			if (i != 0)
			{
				writer.Write(", ");
			}
			writer.Write(FormatLabel(instructions[i].Offset));
		}
		writer.Write(")");
	}

	public static string ToInvariantCultureString(object value)
	{
		if (!(value is IConvertible convertible))
		{
			return value.ToString();
		}
		return convertible.ToString(CultureInfo.InvariantCulture);
	}

	private static void WriteMethodReference(TextWriter writer, MethodReference method)
	{
		writer.Write(FormatTypeReference(method.ReturnType));
		writer.Write(' ');
		writer.Write(FormatTypeReference(method.DeclaringType));
		writer.Write("::");
		writer.Write(method.Name);
		writer.Write("(");
		ReadOnlyCollection<ParameterDefinition> parameters = method.Parameters;
		for (int i = 0; i < parameters.Count; i++)
		{
			if (i > 0)
			{
				writer.Write(", ");
			}
			writer.Write(FormatTypeReference(parameters[i].ParameterType));
		}
		writer.Write(")");
	}

	public static string FormatTypeReference(TypeReference type)
	{
		string typeName = type.FullName;
		return typeName switch
		{
			"System.Void" => "void", 
			"System.String" => "string", 
			"System.Int32" => "int32", 
			"System.Long" => "int64", 
			"System.Boolean" => "bool", 
			"System.Single" => "float32", 
			"System.Double" => "float64", 
			_ => typeName, 
		};
	}
}
