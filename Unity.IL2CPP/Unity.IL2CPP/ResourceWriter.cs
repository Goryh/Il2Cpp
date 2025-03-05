using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class ResourceWriter
{
	private class ResourceRecord
	{
		private readonly byte[] name;

		private readonly byte[] data;

		private readonly int size;

		public ResourceRecord(string name, int size, byte[] data)
		{
			this.name = Encoding.UTF8.GetBytes(name);
			this.size = size;
			this.data = data;
		}

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(size);
			writer.Write(name.Length);
			writer.Write(name);
		}

		public void WriteData(BinaryWriter writer)
		{
			writer.Write(data);
		}

		public int GetRecordSize()
		{
			return 4 + name.Length + 4;
		}
	}

	public static void WriteEmbeddedResources(AssemblyDefinition assembly, Stream stream)
	{
		WriteResourceInformation(stream, GenerateResourceInfomation(assembly));
	}

	private static List<ResourceRecord> GenerateResourceInfomation(AssemblyDefinition assembly)
	{
		List<ResourceRecord> resourceRecords = new List<ResourceRecord>();
		foreach (EmbeddedResource resource in assembly.MainModule.Resources.OfType<EmbeddedResource>())
		{
			byte[] dataBytes = resource.GetResourceData();
			resourceRecords.Add(new ResourceRecord(resource.Name, dataBytes.Length, dataBytes));
		}
		return resourceRecords;
	}

	private static void WriteResourceInformation(Stream stream, List<ResourceRecord> resourceRecords)
	{
		int sizeOfAllResourceRecords = GetSumOfAllRecordSizes(resourceRecords) + GetSizeOfNumberOfRecords();
		BinaryWriter writer = new BinaryWriter(stream);
		writer.Write(sizeOfAllResourceRecords);
		writer.Write(resourceRecords.Count);
		foreach (ResourceRecord resourceRecord in resourceRecords)
		{
			resourceRecord.WriteRecord(writer);
		}
		foreach (ResourceRecord resourceRecord2 in resourceRecords)
		{
			resourceRecord2.WriteData(writer);
		}
	}

	private static int GetSumOfAllRecordSizes(IEnumerable<ResourceRecord> resourceRecords)
	{
		return resourceRecords.Sum((ResourceRecord resourceRecord) => resourceRecord.GetRecordSize());
	}

	private static int GetSizeOfNumberOfRecords()
	{
		return 4;
	}
}
