using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Analytics;

internal static class AnalyticsTableBuilder
{
	public static Il2CppDataTable Complete(GlobalReadOnlyContext context)
	{
		Il2CppDataTable il2CppDataTable = new Il2CppDataTable();
		ReadOnlyDictionary<AssemblyDefinition, AssemblyAnalyticsData> primaryCollectionAnalytics = context.Results.PrimaryCollection.Analytics;
		il2CppDataTable.attribute_total_count_set_option = primaryCollectionAnalytics.Values.Sum((AssemblyAnalyticsData d) => d.SetOptionAttributeCount);
		il2CppDataTable.attribute_total_count_eager_static_constructor = primaryCollectionAnalytics.Values.Sum((AssemblyAnalyticsData d) => d.EagerStaticConstructorAttributeCount);
		il2CppDataTable.attribute_total_count_generate_into_own_cpp_file = primaryCollectionAnalytics.Values.Sum((AssemblyAnalyticsData d) => d.GenerateIntoOwnCppFileAttributeCount);
		il2CppDataTable.attribute_total_count_ignore_by_deep_profiler = primaryCollectionAnalytics.Values.Sum((AssemblyAnalyticsData d) => d.IgnoredByDeepProfilerAttributeCount);
		il2CppDataTable.extra_types_total_count = context.Results.PrimaryCollection.Generics.ExtraTypes.Count;
		return il2CppDataTable;
	}
}
