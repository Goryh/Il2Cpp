using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP;

public static class EmitWriteExtensions
{
	public static void WriteAssignStatement(this ICodeWriter writer, string left, string right)
	{
		writer.WriteLine($"{left} = {right};");
	}

	public static string WriteCall(this ICodeWriter writer, string method)
	{
		writer.Write(method);
		writer.Write('(');
		writer.Write(')');
		return string.Empty;
	}

	public static string WriteCall(this ICodeWriter writer, string method, params string[] arguments)
	{
		writer.Write(method);
		writer.Write('(');
		writer.WriteWithComma(arguments);
		writer.Write(')');
		return string.Empty;
	}

	public static void WriteWithComma(this ICodeWriter writer, string[] elements)
	{
		if (elements.Length != 0)
		{
			int lengthMinusOne = elements.Length - 1;
			for (int index = 0; index < lengthMinusOne; index++)
			{
				string item = elements[index];
				writer.Write(item);
				writer.Write(", ");
			}
			writer.Write(elements[lengthMinusOne]);
		}
	}

	public static string WriteMemset(this ICodeWriter writer, string address, int value, string size)
	{
		return writer.WriteCall("memset", address, value.ToString(), size);
	}
}
