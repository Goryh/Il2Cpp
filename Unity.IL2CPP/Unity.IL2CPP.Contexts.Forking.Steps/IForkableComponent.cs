namespace Unity.IL2CPP.Contexts.Forking.Steps;

public interface IForkableComponent<TWrite, TRead, TFull>
{
	void ForkForPrimaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForPrimaryWrite(TFull forked);

	void ForkForPrimaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForPrimaryCollection(TFull forked);

	void ForkForSecondaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForSecondaryWrite(TFull forked);

	void ForkForSecondaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForSecondaryCollection(TFull forked);

	void ForkForPartialPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForPartialPerAssembly(TFull forked);

	void ForkForFullPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	void MergeForFullPerAssembly(TFull forked);
}
