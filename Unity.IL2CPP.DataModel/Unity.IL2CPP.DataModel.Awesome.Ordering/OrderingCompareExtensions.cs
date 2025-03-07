using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public static class OrderingCompareExtensions
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct SignatureComparisonMode : IDisposable
	{
		public SignatureComparisonMode(bool unused)
		{
			IsSignatureComparisonMode = true;
		}

		public void Dispose()
		{
			IsSignatureComparisonMode = false;
		}
	}

	[ThreadStatic]
	private static bool IsSignatureComparisonMode;

	public static int Compare(this AssemblyDefinition x, AssemblyDefinition y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		return string.Compare(x.Name.Name, y.Name.Name, StringComparison.Ordinal);
	}

	public static int Compare(this TypeReference[] x, TypeReference[] y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		if (x.Length < y.Length)
		{
			return 1;
		}
		if (x.Length > y.Length)
		{
			return -1;
		}
		if (x.Length == 0 && y.Length == 0)
		{
			return 0;
		}
		for (int i = 0; i < x.Length; i++)
		{
			int tmp = x[i].Compare(y[i]);
			if (tmp != 0)
			{
				return tmp;
			}
		}
		return 0;
	}

	public static int Compare(this TypeDefinition x, TypeDefinition y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.DeclaringType.Compare(y.DeclaringType);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Namespace, y.Namespace, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Module.Name, y.Module.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this GenericParameter x, GenericParameter y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.Position - y.Position;
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode)
		{
			return x.Type - y.Type;
		}
		MethodReference xOwnerAsMethodReference = null;
		MethodReference yOwnerAsMethodReference = null;
		if (x.Owner is MethodReference xTmp)
		{
			xOwnerAsMethodReference = xTmp;
		}
		if (y.Owner is MethodReference yTmp)
		{
			yOwnerAsMethodReference = yTmp;
		}
		if (xOwnerAsMethodReference != null && yOwnerAsMethodReference != null)
		{
			return xOwnerAsMethodReference.Compare(yOwnerAsMethodReference);
		}
		if (xOwnerAsMethodReference != null)
		{
			return -1;
		}
		if (yOwnerAsMethodReference != null)
		{
			return 1;
		}
		if (TryCompareAsTypeReference(x.Owner, y.Owner, out result))
		{
			return result;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this TypeSpecification x, TypeSpecification y)
	{
		if (TryCompareAsGenericInstance(x, y, out var result))
		{
			return result;
		}
		if (TryCompareAsArrayType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsPointerType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsFunctionPointerType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsByReferenceType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsOptionalModifierType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsRequiredModifierType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsPinnedType(x, y, out result))
		{
			return result;
		}
		if (TryCompareAsSentinelType(x, y, out result))
		{
			return result;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this TypeReference x, TypeReference y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		if (TryCompareAsGenericParameter(x, y, out var result))
		{
			return result;
		}
		if (TryCompareAsTypeSpecification(x, y, out result))
		{
			return result;
		}
		result = x.DeclaringType.Compare(y.DeclaringType);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Namespace, y.Namespace, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = CompareAssembliesOfTypeReferences(x, y);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this ArrayType x, ArrayType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.Rank - y.Rank;
		if (result != 0)
		{
			return result;
		}
		if (x.IsVector && !y.IsVector)
		{
			return -1;
		}
		if (!x.IsVector && y.IsVector)
		{
			return 1;
		}
		result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this ByReferenceType x, ByReferenceType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this PointerType x, PointerType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this PinnedType x, PinnedType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this SentinelType x, SentinelType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this OptionalModifierType x, OptionalModifierType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		result = x.ModifierType.Compare(y.ModifierType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this RequiredModifierType x, RequiredModifierType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		result = x.ModifierType.Compare(y.ModifierType);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this GenericInstanceType x, GenericInstanceType y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Namespace, y.Namespace, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = x.GenericArguments.Count - y.GenericArguments.Count;
		if (result != 0)
		{
			return result;
		}
		for (int i = 0; i < x.GenericArguments.Count; i++)
		{
			result = x.GenericArguments[i].Compare(y.GenericArguments[i]);
			if (result != 0)
			{
				return result;
			}
		}
		result = x.ElementType.Compare(y.ElementType);
		if (result != 0)
		{
			return result;
		}
		result = CompareAssembliesOfTypeReferences(x, y);
		if (result != 0)
		{
			return result;
		}
		if (IsSignatureComparisonMode || x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this GenericInstanceMethod x, GenericInstanceMethod y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int typeCompare = x.DeclaringType.Compare(y.DeclaringType);
		if (typeCompare != 0)
		{
			return typeCompare;
		}
		int nameCompare = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (nameCompare != 0)
		{
			return nameCompare;
		}
		int paramDiff = x.Parameters.Count - y.Parameters.Count;
		if (paramDiff != 0)
		{
			return paramDiff;
		}
		paramDiff = x.GenericArguments.Count - y.GenericArguments.Count;
		if (paramDiff != 0)
		{
			return paramDiff;
		}
		for (int i = 0; i < x.GenericArguments.Count; i++)
		{
			nameCompare = x.GenericArguments[i].Compare(y.GenericArguments[i]);
			if (nameCompare != 0)
			{
				return nameCompare;
			}
		}
		using (new SignatureComparisonMode(unused: true))
		{
			MethodDefinition yForSigComparision = x.Resolve();
			MethodDefinition xForSigComparision = y.Resolve();
			nameCompare = yForSigComparision.ReturnType.Compare(xForSigComparision.ReturnType);
			if (nameCompare != 0)
			{
				return nameCompare;
			}
			for (int j = 0; j < yForSigComparision.Parameters.Count; j++)
			{
				nameCompare = yForSigComparision.Parameters[j].ParameterType.Compare(xForSigComparision.Parameters[j].ParameterType);
				if (nameCompare != 0)
				{
					return nameCompare;
				}
			}
		}
		if (x == y)
		{
			return 0;
		}
		return x.Resolve().Compare(y.Resolve());
	}

	public static int Compare(this MethodReference x, MethodReference y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.DeclaringType.Compare(y.DeclaringType);
		if (result != 0)
		{
			return result;
		}
		if (TryCompareAsGenericInstance(x, y, out result))
		{
			return result;
		}
		result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = x.Parameters.Count - y.Parameters.Count;
		if (result != 0)
		{
			return result;
		}
		result = x.GenericParameters.Count - y.GenericParameters.Count;
		if (result != 0)
		{
			return result;
		}
		using (new SignatureComparisonMode(unused: true))
		{
			MethodReference yForSigComparision = x.Resolve() ?? x;
			MethodReference xForSigComparision = y.Resolve() ?? y;
			result = yForSigComparision.ReturnType.Compare(xForSigComparision.ReturnType);
			if (result != 0)
			{
				return result;
			}
			for (int i = 0; i < yForSigComparision.Parameters.Count; i++)
			{
				result = yForSigComparision.Parameters[i].ParameterType.Compare(xForSigComparision.Parameters[i].ParameterType);
				if (result != 0)
				{
					return result;
				}
			}
		}
		if (x.IsWindowsRuntimeProjection && !y.IsWindowsRuntimeProjection)
		{
			return -1;
		}
		if (!x.IsWindowsRuntimeProjection && y.IsWindowsRuntimeProjection)
		{
			return 1;
		}
		return x.Resolve().Compare(y.Resolve());
	}

	public static int Compare(this MethodDefinition x, MethodDefinition y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.DeclaringType.Compare(y.DeclaringType);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		if (x.IsCompilerControlled && !y.IsCompilerControlled)
		{
			return -1;
		}
		if (!x.IsCompilerControlled && y.IsCompilerControlled)
		{
			return 1;
		}
		if (x.IsCompilerControlled && y.IsCompilerControlled)
		{
			if (x.MetadataToken.RID > y.MetadataToken.RID)
			{
				return 1;
			}
			if (x.MetadataToken.RID < y.MetadataToken.RID)
			{
				return -1;
			}
			if (x.MetadataToken.RID == y.MetadataToken.RID)
			{
				return 0;
			}
		}
		result = x.Parameters.Count - y.Parameters.Count;
		if (result != 0)
		{
			return result;
		}
		using (new SignatureComparisonMode(unused: true))
		{
			result = x.ReturnType.Compare(y.ReturnType);
			if (result != 0)
			{
				return result;
			}
			for (int i = 0; i < x.Parameters.Count; i++)
			{
				result = x.Parameters[i].ParameterType.Compare(y.Parameters[i].ParameterType);
				if (result != 0)
				{
					return result;
				}
			}
		}
		result = x.GenericParameters.Count - y.GenericParameters.Count;
		if (result != 0)
		{
			return result;
		}
		if (x.IsWindowsRuntimeProjection && !y.IsWindowsRuntimeProjection)
		{
			return -1;
		}
		if (!x.IsWindowsRuntimeProjection && y.IsWindowsRuntimeProjection)
		{
			return 1;
		}
		result = x.Attributes.CompareTo(y.Attributes);
		if (result != 0)
		{
			return result;
		}
		if (x == y)
		{
			return 0;
		}
		return ThrowFailureException(x.FullName, y.FullName, x.UniqueName, y.UniqueName);
	}

	public static int Compare(this AssemblyNameReference x, AssemblyNameReference y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		if (x.Version != null && y.Version == null)
		{
			return -1;
		}
		if (x.Version == null && y.Version != null)
		{
			return 1;
		}
		if (x.Version != null && y.Version != null)
		{
			result = x.Version.CompareTo(y.Version);
			if (result != 0)
			{
				return result;
			}
		}
		if (x.PublicKeyToken != null && y.PublicKeyToken == null)
		{
			return -1;
		}
		if (x.PublicKeyToken == null && y.PublicKeyToken != null)
		{
			return 1;
		}
		if (x.PublicKeyToken != null && y.PublicKeyToken != null)
		{
			result = x.PublicKeyToken.Length - y.PublicKeyToken.Length;
			if (result != 0)
			{
				return result;
			}
			for (int i = 0; i < x.PublicKeyToken.Length; i++)
			{
				result = x.PublicKeyToken[i] - y.PublicKeyToken[i];
				if (result != 0)
				{
					return result;
				}
			}
		}
		if (x.Culture != null && y.Culture == null)
		{
			return -1;
		}
		if (x.Culture == null && y.Culture != null)
		{
			return 1;
		}
		if (x.Culture != null && y.Culture != null)
		{
			result = string.Compare(x.Culture, y.Culture, StringComparison.Ordinal);
			if (result != 0)
			{
				return result;
			}
		}
		if (x.IsRetargetable && !y.IsRetargetable)
		{
			return -1;
		}
		if (!x.IsRetargetable && y.IsRetargetable)
		{
			return 1;
		}
		return 0;
	}

	private static int ThrowFailureException(string xToString, string yToString, string xAssemblyQualifiedName, string yAssemblyQualifiedName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Unhandled compare for");
		stringBuilder.AppendLine(xToString);
		stringBuilder.AppendLine("and");
		stringBuilder.AppendLine(yToString);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Assembly Qualified Names were");
		stringBuilder.AppendLine(xAssemblyQualifiedName);
		stringBuilder.AppendLine("and");
		stringBuilder.AppendLine(yAssemblyQualifiedName);
		throw new ArgumentException(stringBuilder.ToString());
	}

	private static int CompareAssembliesOfTypeReferences(TypeReference x, TypeReference y)
	{
		return x.GetAssemblyNameReference().Compare(y.GetAssemblyNameReference());
	}

	private static bool TryCompareAsTypeReference(IGenericParameterProvider x, IGenericParameterProvider y, out int value)
	{
		TypeReference xReference = null;
		TypeReference yReference = null;
		if (x is TypeReference xTmp)
		{
			xReference = xTmp;
		}
		if (y is TypeReference yTmp)
		{
			yReference = yTmp;
		}
		if (xReference != null && yReference != null)
		{
			value = xReference.Compare(yReference);
			return true;
		}
		if (xReference != null)
		{
			value = -1;
			return true;
		}
		if (yReference != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsGenericParameter(TypeReference x, TypeReference y, out int value)
	{
		GenericParameter xParameter = null;
		GenericParameter yParameter = null;
		if (x is GenericParameter xTmp)
		{
			xParameter = xTmp;
		}
		if (y is GenericParameter yTmp)
		{
			yParameter = yTmp;
		}
		if (xParameter != null && yParameter != null)
		{
			value = xParameter.Compare(yParameter);
			return true;
		}
		if (xParameter != null)
		{
			value = -1;
			return true;
		}
		if (yParameter != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsDefinitions(TypeReference x, TypeReference y, out int value)
	{
		TypeDefinition xAsDefinition = null;
		TypeDefinition yAsDefinition = null;
		if (x is TypeDefinition xTmp)
		{
			xAsDefinition = xTmp;
		}
		if (y is TypeDefinition yTmp)
		{
			yAsDefinition = yTmp;
		}
		if (xAsDefinition != null && yAsDefinition != null)
		{
			value = xAsDefinition.Compare(yAsDefinition);
			return true;
		}
		if (xAsDefinition != null)
		{
			value = -1;
			return true;
		}
		if (yAsDefinition != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsTypeSpecification(TypeReference x, TypeReference y, out int value)
	{
		TypeSpecification xAsOther = null;
		TypeSpecification yAsOther = null;
		if (x is TypeSpecification xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is TypeSpecification yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = -1;
			return true;
		}
		if (yAsOther != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsArrayType(TypeReference x, TypeReference y, out int value)
	{
		ArrayType xAsOther = null;
		ArrayType yAsOther = null;
		if (x is ArrayType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is ArrayType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsPointerType(TypeReference x, TypeReference y, out int value)
	{
		PointerType xAsOther = null;
		PointerType yAsOther = null;
		if (x is PointerType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is PointerType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsFunctionPointerType(TypeReference x, TypeReference y, out int value)
	{
		FunctionPointerType xAsOther = null;
		FunctionPointerType yAsOther = null;
		if (x is FunctionPointerType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is FunctionPointerType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = CompareAsMethodSignature(xAsOther, yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static int CompareAsMethodSignature(IMethodSignature x, IMethodSignature y)
	{
		if (x == y)
		{
			return 0;
		}
		if (y == null)
		{
			return 1;
		}
		if (x == null)
		{
			return -1;
		}
		int hasThisComparison = x.HasThis.CompareTo(y.HasThis);
		if (hasThisComparison != 0)
		{
			return hasThisComparison;
		}
		int explicitThisComparison = x.ExplicitThis.CompareTo(y.ExplicitThis);
		if (explicitThisComparison != 0)
		{
			return explicitThisComparison;
		}
		int callingConventionComparison = x.CallingConvention.CompareTo(y.CallingConvention);
		if (callingConventionComparison != 0)
		{
			return callingConventionComparison;
		}
		int hasParametersComparision = x.HasParameters.CompareTo(y.HasParameters);
		if (hasParametersComparision != 0)
		{
			return hasParametersComparision;
		}
		int result = x.Parameters.Count;
		int parameterCountComparision = result.CompareTo(y.Parameters.Count);
		if (parameterCountComparision != 0)
		{
			return parameterCountComparision;
		}
		using (new SignatureComparisonMode(unused: true))
		{
			int result2 = x.ReturnType.Compare(y.ReturnType);
			if (result2 != 0)
			{
				result = result2;
				goto IL_014f;
			}
			for (int i = 0; i < x.Parameters.Count; i++)
			{
				result2 = y.Parameters[i].ParameterType.Compare(x.Parameters[i].ParameterType);
				if (result2 != 0)
				{
					result = result2;
					goto IL_014f;
				}
			}
		}
		return ThrowFailureException(x.ToString(), y.ToString(), "", "");
		IL_014f:
		return result;
	}

	private static bool TryCompareAsPinnedType(TypeReference x, TypeReference y, out int value)
	{
		PinnedType xAsOther = null;
		PinnedType yAsOther = null;
		if (x is PinnedType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is PinnedType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsSentinelType(TypeReference x, TypeReference y, out int value)
	{
		SentinelType xAsOther = null;
		SentinelType yAsOther = null;
		if (x is SentinelType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is SentinelType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsByReferenceType(TypeReference x, TypeReference y, out int value)
	{
		ByReferenceType xAsOther = null;
		ByReferenceType yAsOther = null;
		if (x is ByReferenceType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is ByReferenceType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsOptionalModifierType(TypeReference x, TypeReference y, out int value)
	{
		OptionalModifierType xAsOther = null;
		OptionalModifierType yAsOther = null;
		if (x is OptionalModifierType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is OptionalModifierType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsRequiredModifierType(TypeReference x, TypeReference y, out int value)
	{
		RequiredModifierType xAsOther = null;
		RequiredModifierType yAsOther = null;
		if (x is RequiredModifierType xTmp)
		{
			xAsOther = xTmp;
		}
		if (y is RequiredModifierType yTmp)
		{
			yAsOther = yTmp;
		}
		if (xAsOther != null && yAsOther != null)
		{
			value = xAsOther.Compare(yAsOther);
			return true;
		}
		if (xAsOther != null)
		{
			value = 1;
			return true;
		}
		if (yAsOther != null)
		{
			value = -1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsGenericInstance(TypeReference x, TypeReference y, out int value)
	{
		GenericInstanceType xAsGenericInstance = null;
		GenericInstanceType yAsGenericInstance = null;
		if (x is GenericInstanceType xTmp)
		{
			xAsGenericInstance = xTmp;
		}
		if (y is GenericInstanceType yTmp)
		{
			yAsGenericInstance = yTmp;
		}
		if (xAsGenericInstance != null && yAsGenericInstance != null)
		{
			value = xAsGenericInstance.Compare(yAsGenericInstance);
			return true;
		}
		if (xAsGenericInstance != null)
		{
			value = -1;
			return true;
		}
		if (yAsGenericInstance != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryCompareAsGenericInstance(MethodReference x, MethodReference y, out int value)
	{
		GenericInstanceMethod xAsGenericInstance = null;
		GenericInstanceMethod yAsGenericInstance = null;
		if (x is GenericInstanceMethod xTmp)
		{
			xAsGenericInstance = xTmp;
		}
		if (y is GenericInstanceMethod yTmp)
		{
			yAsGenericInstance = yTmp;
		}
		if (xAsGenericInstance != null && yAsGenericInstance != null)
		{
			value = xAsGenericInstance.Compare(yAsGenericInstance);
			return true;
		}
		if (xAsGenericInstance != null)
		{
			value = -1;
			return true;
		}
		if (yAsGenericInstance != null)
		{
			value = 1;
			return true;
		}
		value = 0;
		return false;
	}

	public static int Compare(this FieldReference x, FieldReference y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x != null && y == null)
		{
			return 1;
		}
		if (x == null && y != null)
		{
			return -1;
		}
		int result = x.DeclaringType.Compare(y.DeclaringType);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = x.FieldType.Compare(y.FieldType);
		if (result != 0)
		{
			return result;
		}
		return 0;
	}
}
