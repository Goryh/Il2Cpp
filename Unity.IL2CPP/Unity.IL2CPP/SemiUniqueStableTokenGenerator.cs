using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Unity.IL2CPP;

internal static class SemiUniqueStableTokenGenerator
{
	private unsafe static string GenerateForString(string str)
	{
		using SHA1 sha1 = SHA1.Create();
		byte[] dest = new byte[str.Length * 2];
		fixed (char* source = str)
		{
			Marshal.Copy((nint)source, dest, 0, dest.Length);
		}
		byte[] array = sha1.ComputeHash(dest);
		StringBuilder sb = new StringBuilder(array.Length * 2);
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			sb.Append(b.ToString("X2"));
		}
		return sb.ToString();
	}

	internal static string GenerateFor(string literal)
	{
		return GenerateForString(literal);
	}
}
