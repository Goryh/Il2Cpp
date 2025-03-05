using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Diagnostics;

internal class CollectorStateDumper
{
	private readonly Dictionary<Type, int> _dumpCounters;

	private readonly Dictionary<Type, string> _lastValue;

	public CollectorStateDumper()
	{
		_dumpCounters = new Dictionary<Type, int>();
		_lastValue = new Dictionary<Type, string>();
	}

	public CollectorStateDumper(CollectorStateDumper other)
	{
		_dumpCounters = new Dictionary<Type, int>(other._dumpCounters);
		_lastValue = new Dictionary<Type, string>(other._lastValue);
	}

	public void FastForwardDumpCounters(CollectorStateDumper other)
	{
		foreach (KeyValuePair<Type, int> pair in other._dumpCounters)
		{
			_dumpCounters[pair.Key] = pair.Value;
		}
	}

	public void DumpAll(ReadOnlyContext context, IUnrestrictedContextCollectorProvider collectors, IUnrestrictedContextStatefulServicesProvider statefulServices, IUnrestrictedContextServicesProvider services, string phaseName, NPath outputDirectory)
	{
		foreach (IDumpableState obj in GetDumpableObjectsFromContext(collectors, statefulServices, services))
		{
			DumpCollector(context.Global.Services.PathFactory, obj, phaseName, outputDirectory);
		}
	}

	private IEnumerable<IDumpableState> GetDumpableObjectsFromContext(IUnrestrictedContextCollectorProvider collectors, IUnrestrictedContextStatefulServicesProvider statefulServices, IUnrestrictedContextServicesProvider services)
	{
		return GetDumpableObjectsFromType(typeof(IUnrestrictedContextCollectorProvider), collectors).Concat(GetDumpableObjectsFromType(typeof(IUnrestrictedContextStatefulServicesProvider), statefulServices)).Concat(GetDumpableObjectsFromType(typeof(IUnrestrictedContextServicesProvider), services));
	}

	private IEnumerable<IDumpableState> GetDumpableObjectsFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, object instance)
	{
		PropertyInfo[] properties = type.GetProperties();
		for (int i = 0; i < properties.Length; i++)
		{
			object value = properties[i].GetMethod.Invoke(instance, new object[0]);
			if (value != null && value is IDumpableState obj)
			{
				yield return obj;
			}
		}
	}

	private void DumpCollector(IPathFactoryService pathFactoryService, IDumpableState obj, string phaseName, NPath outputDirectory)
	{
		Type componentType = obj.GetType();
		if (!_dumpCounters.TryGetValue(componentType, out var dumpNumber))
		{
			dumpNumber = (_dumpCounters[componentType] = 0);
		}
		NPath filePath = pathFactoryService.GetFilePath(FileCategory.Other, outputDirectory.Combine($"{obj.GetType().Name}_{dumpNumber.ToString("D2")}_{phaseName}.log"));
		string newValue = null;
		using (StreamWriter writer = new StreamWriter(filePath.ToString()))
		{
			StringBuilder sb = new StringBuilder();
			obj.DumpState(sb);
			newValue = sb.ToString();
			bool changedFromPrevious = dumpNumber != 0 && _lastValue[componentType] != newValue;
			writer.WriteLine("---------Meta-----------");
			writer.WriteLine($"Changed From Previous = {changedFromPrevious}");
			writer.WriteLine("------------------------");
			writer.WriteLine(newValue);
		}
		_dumpCounters[componentType] += 1;
		_lastValue[componentType] = newValue;
	}

	internal static void AppendTable<TKey, TValue>(StringBuilder builder, string tableName, IEnumerable<KeyValuePair<TKey, TValue>> table, Func<TKey, string> keyToString = null, Func<TValue, string> valueToString = null)
	{
		builder.AppendLine("--------------------");
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, builder);
		handler.AppendLiteral("Table : ");
		handler.AppendFormatted(tableName);
		builder.AppendLine(ref handler);
		builder.AppendLine("--------------------");
		foreach (KeyValuePair<TKey, TValue> item in table)
		{
			builder.AppendLine((keyToString == null) ? item.Key.ToString() : keyToString(item.Key));
			builder.Append("  Value: ");
			builder.AppendLine((valueToString == null) ? item.Value.ToString() : valueToString(item.Value));
		}
		builder.AppendLine("--------------------");
	}

	internal static void AppendCollection<T>(StringBuilder builder, string tableName, IEnumerable<T> collection, Func<T, string> toString = null)
	{
		builder.AppendLine("--------------------");
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, builder);
		handler.AppendLiteral("Collection : ");
		handler.AppendFormatted(tableName);
		builder.AppendLine(ref handler);
		builder.AppendLine("--------------------");
		foreach (T item in collection)
		{
			builder.AppendLine((toString == null) ? item.ToString() : toString(item));
		}
		builder.AppendLine("--------------------");
	}

	internal static void AppendValue(StringBuilder builder, string name, object value)
	{
		string valueString = ((value == null) ? "null" : value.ToString());
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, builder);
		handler.AppendFormatted(name);
		handler.AppendLiteral(" = ");
		handler.AppendFormatted(valueString);
		builder.AppendLine(ref handler);
	}
}
