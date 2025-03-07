namespace Unity.IL2CPP.DataModel;

public interface IConstantProvider
{
	bool HasConstant { get; }

	object Constant { get; }
}
