using System;
using System.Linq;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class AssemblyNameReference
{
	public string Name { get; }

	public string FullName { get; }

	public string Culture { get; }

	public Version Version { get; }

	public AssemblyHashAlgorithm HashAlgorithm { get; }

	public byte[] Hash { get; }

	public byte[] PublicKey { get; }

	public byte[] PublicKeyToken { get; }

	public AssemblyAttributes Attributes { get; }

	public bool HasPublicKey => Attributes.HasFlag(AssemblyAttributes.PublicKey);

	public bool IsRetargetable => Attributes.HasFlag(AssemblyAttributes.Retargetable);

	public bool IsWindowsRuntime { get; }

	public AssemblyNameReference(string name)
		: this(name, new Version())
	{
	}

	public AssemblyNameReference(string name, Version version)
		: this(name, version, isWindowsRuntime: false)
	{
	}

	public AssemblyNameReference(string name, Version version, bool isWindowsRuntime)
	{
		Name = name;
		Culture = string.Empty;
		PublicKey = Array.Empty<byte>();
		PublicKeyToken = Array.Empty<byte>();
		Hash = Array.Empty<byte>();
		HashAlgorithm = AssemblyHashAlgorithm.None;
		Version = version;
		IsWindowsRuntime = isWindowsRuntime;
	}

	public AssemblyNameReference(string name, string culture, Version version, AssemblyHashAlgorithm hashAlgorithm, byte[] hash, byte[] publicKey, byte[] publicKeyToken, AssemblyAttributes attributes)
	{
		Name = name;
		Culture = culture;
		Version = version;
		HashAlgorithm = hashAlgorithm;
		Hash = hash;
		PublicKey = publicKey;
		PublicKeyToken = publicKeyToken;
		Attributes = attributes;
		IsWindowsRuntime = Attributes.HasFlag(AssemblyAttributes.WindowsRuntime);
		string cultureString = (string.IsNullOrEmpty(culture) ? "neutral" : culture);
		string publicKeyTokenString = (HasPublicKey ? string.Join("", publicKeyToken.Select((byte c) => c.ToString("x2"))) : "null");
		string retargetableString = (IsRetargetable ? ", Retargetable=Yes" : "");
		FullName = $"{Name}, Version={Version}, Culture={cultureString}, PublicKeyToken={publicKeyTokenString}{retargetableString}";
	}

	internal AssemblyNameReference(Mono.Cecil.AssemblyNameReference source)
		: this(source.Name, source.Culture, source.Version, (AssemblyHashAlgorithm)source.HashAlgorithm, source.Hash, source.PublicKey ?? Array.Empty<byte>(), source.PublicKeyToken ?? Array.Empty<byte>(), (AssemblyAttributes)source.Attributes)
	{
	}

	public override string ToString()
	{
		return FullName;
	}
}
