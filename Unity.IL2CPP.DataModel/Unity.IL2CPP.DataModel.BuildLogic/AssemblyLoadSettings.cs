using System;
using System.Diagnostics;
using NiceIO;

namespace Unity.IL2CPP.DataModel.BuildLogic;

[DebuggerDisplay("{Path}")]
public class AssemblyLoadSettings : IEquatable<AssemblyLoadSettings>
{
	public readonly NPath Path;

	public readonly bool LoadSymbols;

	public readonly bool ExportsOnly;

	public AssemblyLoadSettings(NPath path, bool loadSymbols, bool exportsOnly)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		Path = path;
		LoadSymbols = loadSymbols;
		ExportsOnly = exportsOnly;
	}

	public bool Equals(AssemblyLoadSettings other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (object.Equals(Path, other.Path) && LoadSymbols == other.LoadSymbols)
		{
			return ExportsOnly == other.ExportsOnly;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((AssemblyLoadSettings)obj);
	}

	public override int GetHashCode()
	{
		return (((((Path != null) ? Path.GetHashCode() : 0) * 397) ^ LoadSymbols.GetHashCode()) * 397) ^ ExportsOnly.GetHashCode();
	}
}
