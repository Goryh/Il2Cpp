using System;
using System.Text;

namespace Unity.IL2CPP.DataModel.BuildLogic.Naming;

public static class NamingUtils
{
	private const int MaxLengthBeforeHashing = 1500;

	internal static void AppendArraySuffix(ArrayType type, StringBuilder builder)
	{
		AppendArraySuffix(type.IsVector, type.Rank, builder);
	}

	internal static void AppendArraySuffix(bool isVector, int rank, StringBuilder builder)
	{
		if (isVector)
		{
			builder.Append("[]");
			return;
		}
		if (rank == 1)
		{
			builder.Append("[...]");
			return;
		}
		builder.Append('[');
		builder.Append(',', rank - 1);
		builder.Append(']');
	}

	public static void AppendClean(this StringBuilder sb, string name)
	{
		sb.AppendClean(name, skipFirstCharacterSafeCheck: false);
	}

	internal static void AppendClean(this StringBuilder sb, string name, bool skipFirstCharacterSafeCheck)
	{
		char[] chars = name.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			char c = chars[i];
			if (IsSafeCharacter(c) || (IsAsciiDigit(c) && (i != 0 || skipFirstCharacterSafeCheck)))
			{
				sb.Append(c);
			}
			else
			{
				sb.AppendUncleanCharacter(c);
			}
		}
	}

	public static void AppendClean(this StringBuilder sb, char c)
	{
		if (IsSafeCharacter(c) || IsAsciiDigit(c))
		{
			sb.Append(c);
		}
		else
		{
			sb.AppendUncleanCharacter(c);
		}
	}

	private static void AppendUncleanCharacter(this StringBuilder sb, char c)
	{
		ushort unicode = Convert.ToUInt16(c);
		if (unicode < 255)
		{
			if (unicode == 46 || unicode == 47 || unicode == 96 || unicode == 95)
			{
				sb.Append('_');
			}
			else
			{
				sb.AppendFormat("U{0:X2}", unicode);
			}
		}
		else if (unicode < 4095)
		{
			sb.AppendFormat("U{0:X3}", unicode);
		}
		else
		{
			sb.AppendFormat("U{0:X4}", unicode);
		}
	}

	private static bool IsSafeCharacter(char c)
	{
		if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
		{
			return c == '_';
		}
		return true;
	}

	private static bool IsAsciiDigit(char c)
	{
		if (c >= '0')
		{
			return c <= '9';
		}
		return false;
	}

	public static string ValueOrHashIfTooLong(string str, string prefixIfHashingNeeded)
	{
		return ValueOrHashIfTooLong(str, prefixIfHashingNeeded, 1500);
	}

	public static string ValueOrHashIfTooLong(string str, string prefixIfHashingNeeded, int maxLengthBeforeHashing)
	{
		if (str.Length > maxLengthBeforeHashing)
		{
			return prefixIfHashingNeeded + GenerateHashForString(str);
		}
		return str;
	}

	public static string ValueOrHashIfTooLong(StringBuilder builder, string prefixIfHashingNeeded)
	{
		return ValueOrHashIfTooLong(builder, prefixIfHashingNeeded, 1500);
	}

	public static string ValueOrHashIfTooLong(StringBuilder builder, string prefixIfHashingNeeded, int maxLengthBeforeHashing)
	{
		if (builder.Length > maxLengthBeforeHashing)
		{
			return prefixIfHashingNeeded + GenerateHashForString(builder.ToString());
		}
		return builder.ToString();
	}

	public static string GenerateHashForString(string str)
	{
		return CppNamePopulator.GenerateForString(str);
	}
}
