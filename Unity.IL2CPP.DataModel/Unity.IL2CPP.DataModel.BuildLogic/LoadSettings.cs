using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public class LoadSettings : IEquatable<LoadSettings>
{
	public readonly ReadOnlyCollection<AssemblyLoadSettings> AssemblySettings;

	public readonly LoadParameters Parameters;

	public LoadSettings(ReadOnlyCollection<AssemblyLoadSettings> assemblySettings, LoadParameters parameters)
	{
		AssemblySettings = assemblySettings;
		Parameters = parameters;
	}

	public bool Equals(LoadSettings other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (AssemblySettings.OrderBy((AssemblyLoadSettings a) => a.Path.ToString()).SequenceEqual(other.AssemblySettings.OrderBy((AssemblyLoadSettings a) => a.Path.ToString())))
		{
			return object.Equals(Parameters, other.Parameters);
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
		return Equals((LoadSettings)obj);
	}

	public override int GetHashCode()
	{
		return (AssemblySettings.Aggregate(0, (int hash, AssemblyLoadSettings a) => hash ^ (a.GetHashCode() * 397)) * 397) ^ Parameters.GetHashCode();
	}
}
