using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;

public class AnalyticsCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, AssemblyAnalyticsData>
{
	protected override string Name => "Analytics";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EnableAnalytics;
	}

	protected override AssemblyAnalyticsData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		int eagerStaticConstructorAttributeCount = 0;
		int setOptionAttributeCount = 0;
		int ownCppFileAttributeCount = 0;
		int ignoredByDeepProfilerAttributeCount = 0;
		foreach (CustomAttribute item2 in CompilerServicesSupport.SetOptionAttributes(item))
		{
			_ = item2;
			setOptionAttributeCount++;
		}
		foreach (TypeDefinition type in item.GetAllTypes())
		{
			if (CompilerServicesSupport.HasEagerStaticClassConstructionEnabled(type))
			{
				eagerStaticConstructorAttributeCount++;
			}
			if (CompilerServicesSupport.HasGenerateIntoOwnCppFile(type))
			{
				ownCppFileAttributeCount++;
			}
			foreach (CustomAttribute item3 in CompilerServicesSupport.SetOptionAttributes(type))
			{
				_ = item3;
				setOptionAttributeCount++;
			}
			foreach (MethodDefinition method in type.Methods)
			{
				if (CompilerServicesSupport.HasGenerateIntoOwnCppFile(method))
				{
					ownCppFileAttributeCount++;
				}
				if (CompilerServicesSupport.HasIgnoredByDeepProfilerAttribute(method))
				{
					ignoredByDeepProfilerAttributeCount++;
				}
				foreach (CustomAttribute customAttribute in method.CustomAttributes)
				{
					if (CompilerServicesSupport.IsSetOptionAttribute(customAttribute))
					{
						setOptionAttributeCount++;
					}
				}
			}
			foreach (PropertyDefinition property in type.Properties)
			{
				foreach (CustomAttribute item4 in CompilerServicesSupport.SetOptionAttributes(property))
				{
					_ = item4;
					setOptionAttributeCount++;
				}
			}
		}
		return new AssemblyAnalyticsData
		{
			EagerStaticConstructorAttributeCount = eagerStaticConstructorAttributeCount,
			SetOptionAttributeCount = setOptionAttributeCount,
			GenerateIntoOwnCppFileAttributeCount = ownCppFileAttributeCount,
			IgnoredByDeepProfilerAttributeCount = ignoredByDeepProfilerAttributeCount
		};
	}
}
