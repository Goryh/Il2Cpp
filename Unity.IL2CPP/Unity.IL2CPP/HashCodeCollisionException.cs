using System;
using System.Text;

namespace Unity.IL2CPP;

public class HashCodeCollisionException : Exception
{
	public HashCodeCollisionException(string message)
		: base(message)
	{
	}

	public HashCodeCollisionException(string hashValue, string existingItem, string collidingItem)
		: this(FormatMessage(hashValue, existingItem, collidingItem))
	{
	}

	private static string FormatMessage(string hashValue, string existingItem, string collidingItem)
	{
		StringBuilder stringBuilder;
		StringBuilder stringBuilder2 = (stringBuilder = new StringBuilder());
		StringBuilder stringBuilder3 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 1, stringBuilder);
		handler.AppendLiteral("Hash code collision on value `");
		handler.AppendFormatted(hashValue);
		handler.AppendLiteral("`");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder = stringBuilder2;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder);
		handler.AppendLiteral("Existing Item was : `");
		handler.AppendFormatted(existingItem);
		handler.AppendLiteral("`");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder = stringBuilder2;
		StringBuilder stringBuilder5 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder);
		handler.AppendLiteral("Colliding Item was : `");
		handler.AppendFormatted(collidingItem);
		handler.AppendLiteral("`");
		stringBuilder5.AppendLine(ref handler);
		return stringBuilder2.ToString();
	}
}
