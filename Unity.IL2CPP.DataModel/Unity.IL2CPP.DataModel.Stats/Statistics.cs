using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NiceIO;

namespace Unity.IL2CPP.DataModel.Stats;

internal class Statistics : IDisposable
{
	private class SectionStatistics
	{
		public readonly string Name;

		public int LockHits;

		public readonly List<TypeReference> TypesCreated = new List<TypeReference>();

		public readonly List<FieldReference> FieldsCreated = new List<FieldReference>();

		public readonly List<MethodReference> MethodsCreated = new List<MethodReference>();

		public readonly List<TypeReference> TypesPerThreadCacheHit = new List<TypeReference>();

		public readonly List<FieldReference> FieldsPerThreadCacheHit = new List<FieldReference>();

		public readonly List<MethodReference> MethodsPerThreadCacheHit = new List<MethodReference>();

		public readonly List<TypeReference> TypesPerThreadCacheMiss = new List<TypeReference>();

		public readonly List<FieldReference> FieldsPerThreadCacheMiss = new List<FieldReference>();

		public readonly List<MethodReference> MethodsPerThreadCacheMiss = new List<MethodReference>();

		public readonly List<TypeReference> TypesUniqueName = new List<TypeReference>();

		public readonly List<MethodReference> MethodsUniqueName = new List<MethodReference>();

		public readonly List<TypeReference> TypesFullName = new List<TypeReference>();

		public readonly List<IMethodSignature> MethodsFullName = new List<IMethodSignature>();

		public readonly List<TypeReference> TypesCppName = new List<TypeReference>();

		public readonly List<MethodReference> MethodsCppName = new List<MethodReference>();

		public SectionStatistics(string name)
		{
			Name = name;
		}

		public void RecordLockHit()
		{
			LockHits++;
		}
	}

	private readonly ThreadLocal<List<TypeReference>> _fullyConstructedTypeInflations = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<MethodReference>> _fullyConstructedMethodInflations = new ThreadLocal<List<MethodReference>>(() => new List<MethodReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<FieldReference>> _fullyConstructedFieldInflations = new ThreadLocal<List<FieldReference>>(() => new List<FieldReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<TypeReference>> _threadCachedTypeInflationHits = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<MethodReference>> _threadCachedMethodInflationsHits = new ThreadLocal<List<MethodReference>>(() => new List<MethodReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<FieldReference>> _threadCachedFieldInflationsHits = new ThreadLocal<List<FieldReference>>(() => new List<FieldReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<TypeReference>> _threadCachedTypeInflationMisses = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<MethodReference>> _threadCachedMethodInflationsMisses = new ThreadLocal<List<MethodReference>>(() => new List<MethodReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<FieldReference>> _threadCachedFieldInflationsMisses = new ThreadLocal<List<FieldReference>>(() => new List<FieldReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<TypeReference>> _uniqueNameBuiltTypes = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<MethodReference>> _uniqueNameBuiltMethods = new ThreadLocal<List<MethodReference>>(() => new List<MethodReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<TypeReference>> _fullNameBuiltTypes = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<IMethodSignature>> _fullNameBuiltMethods = new ThreadLocal<List<IMethodSignature>>(() => new List<IMethodSignature>(), trackAllValues: true);

	private readonly ThreadLocal<List<TypeReference>> _cppNameBuiltTypes = new ThreadLocal<List<TypeReference>>(() => new List<TypeReference>(), trackAllValues: true);

	private readonly ThreadLocal<List<MethodReference>> _cppNameBuiltMethods = new ThreadLocal<List<MethodReference>>(() => new List<MethodReference>(), trackAllValues: true);

	private readonly List<TypeReference> _fullyConstructedTypeCreate = new List<TypeReference>();

	private readonly List<MethodReference> _fullyConstructedMethodCreate = new List<MethodReference>();

	private readonly List<FieldReference> _fullyConstructedFieldCreate = new List<FieldReference>();

	private readonly ThreadLocal<Stack<SectionStatistics>> _activeSections = new ThreadLocal<Stack<SectionStatistics>>(() => new Stack<SectionStatistics>());

	private readonly ThreadLocal<List<SectionStatistics>> _completedSections = new ThreadLocal<List<SectionStatistics>>(() => new List<SectionStatistics>(), trackAllValues: true);

	private readonly TypeContext _context;

	private readonly ThreadLocal<int> _largestStringBuilder = new ThreadLocal<int>(() => 0, trackAllValues: true);

	private readonly ThreadLocal<int> _mostStringBuildersInPool = new ThreadLocal<int>(() => 0, trackAllValues: true);

	public Statistics(TypeContext context)
	{
		_context = context;
	}

	[Conditional("DEBUG")]
	internal void PushSection(string name)
	{
		_activeSections.Value.Push(new SectionStatistics(name));
	}

	[Conditional("DEBUG")]
	internal void PopSection()
	{
		_completedSections.Value.Add(_activeSections.Value.Pop());
	}

	[Conditional("DEBUG")]
	public void WriteStatistics(TextWriter consoleWriter, NPath statsOutputDirectory)
	{
		TypeReference[] allKnownTypes = _context.AllKnownNonDefinitionTypesUnordered().ToArray();
		MethodReference[] allKnownMethods = _context.AllKnownNonDefinitionMethodsUnordered().ToArray();
		FieldReference[] allKnownFields = _context.AllKnownNonDefinitionFieldsUnordered().ToArray();
		TypeReference[] allTypeInflations = _fullyConstructedTypeInflations.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		MethodReference[] allMethodInflations = _fullyConstructedMethodInflations.Values.SelectMany((List<MethodReference> t) => t).ToArray();
		FieldReference[] allFieldInflations = _fullyConstructedFieldInflations.Values.SelectMany((List<FieldReference> t) => t).ToArray();
		TypeReference[] allTypeInflationsThreadCachedHits = _threadCachedTypeInflationHits.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		MethodReference[] allMethodInflationsThreadCachedHits = _threadCachedMethodInflationsHits.Values.SelectMany((List<MethodReference> t) => t).ToArray();
		FieldReference[] allFieldInflationsThreadCachedHits = _threadCachedFieldInflationsHits.Values.SelectMany((List<FieldReference> t) => t).ToArray();
		TypeReference[] allTypeInflationsThreadCachedMisses = _threadCachedTypeInflationMisses.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		MethodReference[] allMethodInflationsThreadCachedMisses = _threadCachedMethodInflationsMisses.Values.SelectMany((List<MethodReference> t) => t).ToArray();
		FieldReference[] allFieldInflationsThreadCachedMisses = _threadCachedFieldInflationsMisses.Values.SelectMany((List<FieldReference> t) => t).ToArray();
		TypeReference[] allTypeUniqueNames = _uniqueNameBuiltTypes.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		MethodReference[] allMethodUniqueNames = _uniqueNameBuiltMethods.Values.SelectMany((List<MethodReference> t) => t).ToArray();
		TypeReference[] allTypeFullNames = _fullNameBuiltTypes.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		IMethodSignature[] allMethodFullNames = _fullNameBuiltMethods.Values.SelectMany((List<IMethodSignature> t) => t).ToArray();
		TypeReference[] allTypeCppName = _cppNameBuiltTypes.Values.SelectMany((List<TypeReference> t) => t).ToArray();
		MethodReference[] allMethodCppName = _cppNameBuiltMethods.Values.SelectMany((List<MethodReference> t) => t).ToArray();
		int largestStringBuilder = _largestStringBuilder.Values.Max();
		int[] largestStringBuilderForEachThread = _largestStringBuilder.Values.OrderBy((int i) => i).ToArray();
		int averagePerThreadLargestStringBuilders = (int)_largestStringBuilder.Values.Average();
		int mostStringBuildersInPool = _mostStringBuildersInPool.Values.Max();
		int[] mostStringBuildersInPoolForEachThread = _mostStringBuildersInPool.Values.OrderBy((int i) => i).ToArray();
		HashSet<TypeReference> fullyConstructedTypeInflationsHashSet = new HashSet<TypeReference>(allTypeInflations);
		HashSet<MethodReference> fullyConstructedMethodInflationsHashSet = new HashSet<MethodReference>(allMethodInflations);
		HashSet<FieldReference> fullyConstructedFieldInflationsHashSet = new HashSet<FieldReference>(allFieldInflations);
		List<TypeReference> neverLookedUpTypes = new List<TypeReference>();
		List<MethodReference> neverLookedUpMethods = new List<MethodReference>();
		List<FieldReference> neverLookedUpFields = new List<FieldReference>();
		TypeReference[] array = allKnownTypes;
		foreach (TypeReference type in array)
		{
			if (!fullyConstructedTypeInflationsHashSet.Contains(type))
			{
				neverLookedUpTypes.Add(type);
			}
		}
		MethodReference[] array2 = allKnownMethods;
		foreach (MethodReference method in array2)
		{
			if (!fullyConstructedMethodInflationsHashSet.Contains(method))
			{
				neverLookedUpMethods.Add(method);
			}
		}
		FieldReference[] array3 = allKnownFields;
		foreach (FieldReference field in array3)
		{
			if (!fullyConstructedFieldInflationsHashSet.Contains(field))
			{
				neverLookedUpFields.Add(field);
			}
		}
		TypeDefinition[] allTypeDefinitions = _context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.GetAllTypes()).ToArray();
		MethodDefinition[] allMethodDefinitions = _context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.AllMethods()).ToArray();
		FieldDefinition[] allFieldDefinitions = _context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition asm) => asm.AllFields()).ToArray();
		StreamWriter fileWriter = ((statsOutputDirectory == null) ? null : new StreamWriter(statsOutputDirectory.Combine("statistics-data-model.txt")));
		TextWriter[] writers = ((fileWriter != null) ? new TextWriter[2] { consoleWriter, fileWriter } : new TextWriter[1] { consoleWriter });
		try
		{
			TextWriter[] array4 = writers;
			foreach (TextWriter writer in array4)
			{
				writer.WriteLine("-----Data Model Statistics-----");
				WriteHeader(writer, "Overview");
				writer.WriteLine("Types");
				writer.WriteLine($"  Total Non-Definition : {allKnownTypes.Length}");
				writer.WriteLine($"  Total Definitions : {allTypeDefinitions.Length}");
				writer.WriteLine();
				writer.WriteLine("Methods");
				writer.WriteLine($"  Total Non-Definition : {allKnownMethods.Length}");
				writer.WriteLine($"  Total Definitions : {allMethodDefinitions.Length}");
				writer.WriteLine($"  Total Definitions w/ Bodies: {allMethodDefinitions.Count((MethodDefinition m) => m.HasBody)}");
				writer.WriteLine();
				writer.WriteLine("Fields");
				writer.WriteLine($"  Total Non-Definition : {allKnownFields.Length}");
				writer.WriteLine($"  Total Definitions : {allFieldDefinitions.Length}");
				writer.WriteLine();
				WriteHeader(writer, "Global Factory (Most Costly)");
				writer.WriteLine("Types");
				writer.WriteLine($"  Total : {allTypeInflations.Length + _fullyConstructedTypeCreate.Count}");
				writer.WriteLine($"  Creations : {_fullyConstructedTypeCreate.Count}");
				writer.WriteLine($"  Lookups : {allTypeInflations.Length}");
				writer.WriteLine($"  Never Looked Up : {neverLookedUpTypes.Count}");
				writer.WriteLine();
				writer.WriteLine("Methods");
				writer.WriteLine($"  Total : {allMethodInflations.Length + _fullyConstructedMethodCreate.Count}");
				writer.WriteLine($"  Creations : {_fullyConstructedMethodCreate.Count}");
				writer.WriteLine($"  Lookups : {allMethodInflations.Length}");
				writer.WriteLine($"  Never Looked Up : {neverLookedUpMethods.Count}");
				writer.WriteLine();
				writer.WriteLine("Fields");
				writer.WriteLine($"  Total : {allFieldInflations.Length + _fullyConstructedFieldCreate.Count}");
				writer.WriteLine($"  Creations : {_fullyConstructedFieldCreate.Count}");
				writer.WriteLine($"  Lookups : {allFieldInflations.Length}");
				writer.WriteLine($"  Never Looked Up : {neverLookedUpFields.Count}");
				writer.WriteLine();
				WriteHeader(writer, "Per Thread Cache Factory (Less Costly than Global)");
				writer.WriteLine("Types");
				writer.WriteLine($"  Total : {allTypeInflationsThreadCachedHits.Length + allTypeInflationsThreadCachedMisses.Length}");
				writer.WriteLine($"  Cache Hits : {allTypeInflationsThreadCachedHits.Length}");
				writer.WriteLine($"  Cache Misses : {allTypeInflationsThreadCachedMisses.Length}");
				writer.WriteLine();
				writer.WriteLine("Methods");
				writer.WriteLine($"  Total : {allMethodInflationsThreadCachedHits.Length + allMethodInflationsThreadCachedMisses.Length}");
				writer.WriteLine($"  Cache Hits : {allMethodInflationsThreadCachedHits.Length}");
				writer.WriteLine($"  Cache Misses : {allMethodInflationsThreadCachedMisses.Length}");
				writer.WriteLine();
				writer.WriteLine("Fields");
				writer.WriteLine($"  Total : {allFieldInflationsThreadCachedHits.Length + allFieldInflationsThreadCachedMisses.Length}");
				writer.WriteLine($"  Cache Hits : {allFieldInflationsThreadCachedHits.Length}");
				writer.WriteLine($"  Cache Misses : {allFieldInflationsThreadCachedMisses.Length}");
				writer.WriteLine();
				WriteHeader(writer, "Naming");
				writer.WriteLine("Types");
				writer.WriteLine($"  FullName : {allTypeFullNames.Length}");
				writer.WriteLine($"  UniqueName : {allTypeUniqueNames.Length}");
				writer.WriteLine($"  CppName : {allTypeCppName.Length}");
				writer.WriteLine();
				writer.WriteLine("Methods");
				writer.WriteLine($"  FullName : {allMethodFullNames.Length}");
				writer.WriteLine($"  UniqueName : {allMethodUniqueNames.Length}");
				writer.WriteLine($"  CppName : {allMethodCppName.Length}");
				writer.WriteLine();
				WriteHeader(writer, "Factories");
				writer.WriteLine($"  StringBuilder Pool - Most in Single Pool : {mostStringBuildersInPool}");
				writer.WriteLine("  StringBuilder Pool - Pool Size per Thread : (" + mostStringBuildersInPoolForEachThread.AggregateWithComma() + ")");
				writer.WriteLine();
				writer.WriteLine($"  StringBuilder - Total per Thread Pools : {largestStringBuilderForEachThread.Length}");
				writer.WriteLine($"  StringBuilder.Capacity - Initial : {8000}");
				writer.WriteLine($"  StringBuilder.Capacity - Largest : {largestStringBuilder}");
				writer.WriteLine($"  StringBuilder.Capacity - Average Largest per Thread : {averagePerThreadLargestStringBuilders}");
				writer.WriteLine("  StringBuilder.Capacity - Largest per Thread : (" + largestStringBuilderForEachThread.AggregateWithComma() + ")");
				writer.WriteLine();
				WriteSectionStatistics(writer);
			}
		}
		finally
		{
			fileWriter?.Dispose();
		}
	}

	private void WriteHeader(TextWriter writer, string name)
	{
		writer.WriteLine("-------------------------------");
		writer.WriteLine(name);
		writer.WriteLine("-------------------------------");
	}

	private void WriteSectionStatistics(TextWriter writer)
	{
		WriteHeader(writer, "Section Statistics");
		Dictionary<string, List<SectionStatistics>> mergedScopes = new Dictionary<string, List<SectionStatistics>>();
		foreach (SectionStatistics scope in _completedSections.Values.SelectMany((List<SectionStatistics> v) => v))
		{
			if (!mergedScopes.TryGetValue(scope.Name, out var all))
			{
				all = (mergedScopes[scope.Name] = new List<SectionStatistics>());
			}
			all.Add(scope);
		}
		foreach (KeyValuePair<string, List<SectionStatistics>> pair2 in mergedScopes.OrderBy((KeyValuePair<string, List<SectionStatistics>> pair) => pair.Key))
		{
			List<SectionStatistics> scopes = pair2.Value;
			writer.WriteLine("Name : " + pair2.Key);
			writer.WriteLine($"Number of Scopes : {scopes.Count}");
			writer.WriteLine($"Lock Hits : {scopes.Sum((SectionStatistics s) => s.LockHits)}");
			writer.WriteLine($"Types Created : {scopes.Sum((SectionStatistics s) => s.TypesCreated.Count)}");
			writer.WriteLine($"Methods Created : {scopes.Sum((SectionStatistics s) => s.MethodsCreated.Count)}");
			writer.WriteLine($"Fields Created : {scopes.Sum((SectionStatistics s) => s.FieldsCreated.Count)}");
			writer.WriteLine("Per Thread Cache");
			writer.WriteLine("  Hits");
			writer.WriteLine($"    Types : {scopes.Sum((SectionStatistics s) => s.TypesPerThreadCacheHit.Count)}");
			writer.WriteLine($"    Methods : {scopes.Sum((SectionStatistics s) => s.MethodsPerThreadCacheHit.Count)}");
			writer.WriteLine($"    Fields : {scopes.Sum((SectionStatistics s) => s.FieldsPerThreadCacheHit.Count)}");
			writer.WriteLine("  Misses");
			writer.WriteLine($"    Types : {scopes.Sum((SectionStatistics s) => s.TypesPerThreadCacheMiss.Count)}");
			writer.WriteLine($"    Methods : {scopes.Sum((SectionStatistics s) => s.MethodsPerThreadCacheMiss.Count)}");
			writer.WriteLine($"    Fields : {scopes.Sum((SectionStatistics s) => s.FieldsPerThreadCacheMiss.Count)}");
			writer.WriteLine("Naming");
			writer.WriteLine("  FullName");
			writer.WriteLine($"    Types : {scopes.Sum((SectionStatistics s) => s.TypesFullName.Count)}");
			writer.WriteLine($"    Methods : {scopes.Sum((SectionStatistics s) => s.MethodsFullName.Count)}");
			writer.WriteLine("  UniqueName");
			writer.WriteLine($"    Types : {scopes.Sum((SectionStatistics s) => s.TypesUniqueName.Count)}");
			writer.WriteLine($"    Methods : {scopes.Sum((SectionStatistics s) => s.MethodsUniqueName.Count)}");
			writer.WriteLine("  CppName");
			writer.WriteLine($"    Types : {scopes.Sum((SectionStatistics s) => s.TypesCppName.Count)}");
			writer.WriteLine($"    Methods : {scopes.Sum((SectionStatistics s) => s.MethodsCppName.Count)}");
			writer.WriteLine("-------------------------------");
		}
	}

	[Conditional("DEBUG")]
	public void RecordUniqueNameBuilt(object obj)
	{
		if (!(obj is TypeReference type))
		{
			if (obj is FieldReference)
			{
				throw new NotImplementedException();
			}
			if (!(obj is MethodReference method))
			{
				throw new ArgumentException($"Unhandled type {obj.GetType()}");
			}
			_uniqueNameBuiltMethods.Value.Add(method);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().MethodsUniqueName.Add(method);
			}
		}
		else
		{
			_uniqueNameBuiltTypes.Value.Add(type);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().TypesUniqueName.Add(type);
			}
		}
	}

	[Conditional("DEBUG")]
	public void RecordFullNameBuilt(object obj)
	{
		if (!(obj is TypeReference type))
		{
			if (obj is FieldReference)
			{
				throw new NotImplementedException();
			}
			if (!(obj is IMethodSignature method))
			{
				throw new ArgumentException($"Unhandled type {obj.GetType()}");
			}
			_fullNameBuiltMethods.Value.Add(method);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().MethodsFullName.Add(method);
			}
		}
		else
		{
			_fullNameBuiltTypes.Value.Add(type);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().TypesFullName.Add(type);
			}
		}
	}

	[Conditional("DEBUG")]
	public void RecordCppNameBuilt(object obj)
	{
		if (!(obj is TypeReference type))
		{
			if (obj is FieldReference)
			{
				throw new NotImplementedException();
			}
			if (!(obj is MethodReference method))
			{
				throw new ArgumentException($"Unhandled type {obj.GetType()}");
			}
			_cppNameBuiltMethods.Value.Add(method);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().MethodsCppName.Add(method);
			}
		}
		else
		{
			_cppNameBuiltTypes.Value.Add(type);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().TypesCppName.Add(type);
			}
		}
	}

	[Conditional("DEBUG")]
	public void RecordThreadCachedHit(object obj)
	{
		if (!(obj is TypeReference type))
		{
			if (!(obj is FieldReference field))
			{
				if (!(obj is MethodReference method))
				{
					throw new ArgumentException($"Unhandled type {obj.GetType()}");
				}
				_threadCachedMethodInflationsHits.Value.Add(method);
				if (_activeSections.Value.Count > 0)
				{
					_activeSections.Value.Peek().MethodsPerThreadCacheHit.Add(method);
				}
			}
			else
			{
				_threadCachedFieldInflationsHits.Value.Add(field);
				if (_activeSections.Value.Count > 0)
				{
					_activeSections.Value.Peek().FieldsPerThreadCacheHit.Add(field);
				}
			}
		}
		else
		{
			_threadCachedTypeInflationHits.Value.Add(type);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().TypesPerThreadCacheHit.Add(type);
			}
		}
	}

	[Conditional("DEBUG")]
	public void RecordThreadCachedMiss(object obj)
	{
		if (!(obj is TypeReference type))
		{
			if (!(obj is FieldReference field))
			{
				if (!(obj is MethodReference method))
				{
					throw new ArgumentException($"Unhandled type {obj.GetType()}");
				}
				_threadCachedMethodInflationsMisses.Value.Add(method);
				if (_activeSections.Value.Count > 0)
				{
					_activeSections.Value.Peek().MethodsPerThreadCacheMiss.Add(method);
				}
			}
			else
			{
				_threadCachedFieldInflationsMisses.Value.Add(field);
				if (_activeSections.Value.Count > 0)
				{
					_activeSections.Value.Peek().FieldsPerThreadCacheMiss.Add(field);
				}
			}
		}
		else
		{
			_threadCachedTypeInflationMisses.Value.Add(type);
			if (_activeSections.Value.Count > 0)
			{
				_activeSections.Value.Peek().TypesPerThreadCacheMiss.Add(type);
			}
		}
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedInflation(TypeReference type)
	{
		_fullyConstructedTypeInflations.Value.Add(type);
		RecordSectionLockHit();
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedCreate(TypeReference type)
	{
		_fullyConstructedTypeCreate.Add(type);
		if (_activeSections.Value.Count > 0)
		{
			_activeSections.Value.Peek().TypesCreated.Add(type);
		}
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedInflation(MethodReference method)
	{
		_fullyConstructedMethodInflations.Value.Add(method);
		RecordSectionLockHit();
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedCreate(MethodReference method)
	{
		_fullyConstructedMethodCreate.Add(method);
		if (_activeSections.Value.Count > 0)
		{
			_activeSections.Value.Peek().MethodsCreated.Add(method);
		}
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedInflation(FieldReference field)
	{
		_fullyConstructedFieldInflations.Value.Add(field);
		RecordSectionLockHit();
	}

	[Conditional("DEBUG")]
	public void RecordFullyConstructedCreate(FieldReference field)
	{
		_fullyConstructedFieldCreate.Add(field);
		if (_activeSections.Value.Count > 0)
		{
			_activeSections.Value.Peek().FieldsCreated.Add(field);
		}
	}

	[Conditional("DEBUG")]
	public void RecordReturnedStringBuilder(StringBuilder stringBuilder)
	{
		int currentMax = _largestStringBuilder.Value;
		if (stringBuilder.Capacity > currentMax)
		{
			_largestStringBuilder.Value = stringBuilder.Capacity;
		}
	}

	[Conditional("DEBUG")]
	public void RecordStringBuilderPool(Stack<StringBuilder> pool)
	{
		int currentMax = _mostStringBuildersInPool.Value;
		if (pool.Count > currentMax)
		{
			_mostStringBuildersInPool.Value = pool.Count;
		}
	}

	private void RecordSectionLockHit()
	{
		if (_activeSections.Value.Count > 0)
		{
			_activeSections.Value.Peek().RecordLockHit();
		}
	}

	public void Dispose()
	{
		_fullyConstructedTypeInflations.Dispose();
		_fullyConstructedMethodInflations.Dispose();
		_fullyConstructedFieldInflations.Dispose();
		_threadCachedTypeInflationHits.Dispose();
		_threadCachedMethodInflationsHits.Dispose();
		_threadCachedFieldInflationsHits.Dispose();
		_threadCachedTypeInflationMisses.Dispose();
		_threadCachedMethodInflationsMisses.Dispose();
		_threadCachedFieldInflationsMisses.Dispose();
		_uniqueNameBuiltTypes.Dispose();
		_uniqueNameBuiltMethods.Dispose();
		_fullNameBuiltTypes.Dispose();
		_fullNameBuiltMethods.Dispose();
		_cppNameBuiltTypes.Dispose();
		_cppNameBuiltMethods.Dispose();
		_activeSections.Dispose();
		_completedSections.Dispose();
		_largestStringBuilder.Dispose();
		_mostStringBuildersInPool.Dispose();
	}
}
