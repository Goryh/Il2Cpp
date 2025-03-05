using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components;

public class ICallMappingComponent : ReusedServiceComponentBase<IICallMappingService, ICallMappingComponent>, IICallMappingService
{
	private struct ICallMapValue
	{
		public string Function;

		public string Header;
	}

	private Dictionary<string, ICallMapValue> Map;

	private void ReadMap(string path)
	{
		string[] array = File.ReadAllLines(path);
		string headerFile = "";
		string[] array2 = array;
		foreach (string line in array2)
		{
			if (line.StartsWith(">"))
			{
				headerFile = line.Substring(1);
			}
			if (!line.StartsWith(";") && !line.StartsWith("#") && !line.StartsWith("//") && !line.StartsWith(">"))
			{
				string[] parts = line.Split(new char[1] { ' ' });
				if (parts.Length == 2)
				{
					ICallMapValue current = new ICallMapValue
					{
						Function = parts[1],
						Header = headerFile
					};
					Map[parts[0]] = current;
				}
			}
		}
	}

	public ICallMappingComponent()
	{
		Map = new Dictionary<string, ICallMapValue>();
	}

	public void Initialize(AssemblyConversionContext context)
	{
		ReadMap(context.InputData.DistributionDirectory.Combine("libil2cpp/libil2cpp.icalls").ToString());
	}

	public string ResolveICallFunction(string icall)
	{
		if (Map.ContainsKey(icall))
		{
			return Map[icall].Function;
		}
		return null;
	}

	public string ResolveICallHeader(string icall)
	{
		if (Map.ContainsKey(icall))
		{
			if (!(Map[icall].Header == "null"))
			{
				return Map[icall].Header;
			}
			return null;
		}
		return null;
	}

	protected override ICallMappingComponent ThisAsFull()
	{
		return this;
	}

	protected override IICallMappingService ThisAsRead()
	{
		return this;
	}
}
