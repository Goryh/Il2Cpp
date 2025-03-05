namespace Unity.IL2CPP.Metadata;

public class RgctxEntryName
{
	public readonly string Name;

	public readonly RGCTXEntry Entry;

	public RgctxEntryName(string name, RGCTXEntry entry)
	{
		Name = name;
		Entry = entry;
	}
}
