using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Debugger;

public class SequencePointCollector : ISequencePointCollector, ISequencePointProvider
{
	public class SourceFileData : ISequencePointSourceFileData
	{
		public string File { get; }

		public byte[] Hash { get; }

		public SourceFileData(string file, byte[] hash)
		{
			File = file;
			Hash = hash;
		}
	}

	private static readonly SequencePointInfoComparer s_seqPointComparer = new SequencePointInfoComparer();

	private readonly Dictionary<MethodDefinition, HashSet<SequencePointInfo>> sequencePointsByMethod = new Dictionary<MethodDefinition, HashSet<SequencePointInfo>>();

	private readonly List<SequencePointInfo> allSequencePoints = new List<SequencePointInfo>();

	private readonly Dictionary<MethodDefinition, HashSet<int>> pausePointsByMethod = new Dictionary<MethodDefinition, HashSet<int>>();

	private readonly Dictionary<SequencePointInfo, int> seqPointIndexes = new Dictionary<SequencePointInfo, int>(s_seqPointComparer);

	private readonly Dictionary<string, int> sourceFileIndexes = new Dictionary<string, int>();

	private readonly List<ISequencePointSourceFileData> allSourceFiles = new List<ISequencePointSourceFileData>();

	private readonly Dictionary<string, int> _variableNames = new Dictionary<string, int>();

	private readonly List<VariableData> _variables = new List<VariableData>();

	private readonly Dictionary<MethodDefinition, Range> _variableMap = new Dictionary<MethodDefinition, Range>();

	private readonly List<Range> _scopes = new List<Range>();

	private readonly Dictionary<MethodDefinition, Range> _scopeMap = new Dictionary<MethodDefinition, Range>();

	private bool isComplete;

	public int NumSeqPoints => allSequencePoints.Count;

	public void AddSequencePoint(SequencePointInfo seqPoint)
	{
		if (isComplete)
		{
			throw new InvalidOperationException("Cannot add new sequence points after collection is complete.");
		}
		SequencePointInfo adjustedSeqPoint = GetAdjustedSequencePoint(seqPoint);
		AddSequencePointToMethod(adjustedSeqPoint);
		if (!seqPointIndexes.ContainsKey(seqPoint))
		{
			AddAndIndexSequencePoint(seqPoint, adjustedSeqPoint);
		}
	}

	public void AddPausePoint(MethodDefinition method, int offset)
	{
		if (isComplete)
		{
			throw new InvalidOperationException("Cannot add new pause points after collection is complete.");
		}
		if (pausePointsByMethod.TryGetValue(method, out var offsets))
		{
			offsets.Add(offset);
			return;
		}
		offsets = new HashSet<int>();
		offsets.Add(offset);
		pausePointsByMethod.Add(method, offsets);
	}

	public void Complete()
	{
		isComplete = true;
	}

	public SequencePointInfo GetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind)
	{
		if (!sequencePointsByMethod.TryGetValue(method, out var methodSequencePoints))
		{
			throw new KeyNotFoundException("Could not find a sequence point with specified properties");
		}
		foreach (SequencePointInfo seqPoint in methodSequencePoints)
		{
			if (seqPoint.IlOffset == ilOffset && seqPoint.Kind == kind)
			{
				return seqPoint;
			}
		}
		throw new KeyNotFoundException("Could not find a sequence point with specified properties");
	}

	public bool TryGetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind, out SequencePointInfo info)
	{
		info = null;
		if (!sequencePointsByMethod.TryGetValue(method, out var methodSequencePoints))
		{
			return false;
		}
		foreach (SequencePointInfo seqPoint in methodSequencePoints)
		{
			if (seqPoint.IlOffset == ilOffset && seqPoint.Kind == kind)
			{
				info = seqPoint;
				return true;
			}
		}
		return false;
	}

	public ReadOnlyCollection<SequencePointInfo> GetAllSequencePoints()
	{
		if (!isComplete)
		{
			throw new InvalidOperationException("Cannot retrieve sequence points before collection is complete.");
		}
		return allSequencePoints.AsReadOnly();
	}

	public int GetSeqPointIndex(SequencePointInfo seqPoint)
	{
		if (seqPointIndexes.ContainsKey(seqPoint))
		{
			return seqPointIndexes[seqPoint];
		}
		SequencePointInfo adjustedSeqPoint = GetAdjustedSequencePoint(seqPoint);
		AddSequencePointToMethod(adjustedSeqPoint);
		return AddAndIndexSequencePoint(seqPoint, adjustedSeqPoint);
	}

	public int GetSourceFileIndex(string sourceFile)
	{
		if (sourceFileIndexes.TryGetValue(sourceFile, out var index))
		{
			return index;
		}
		return -1;
	}

	public ReadOnlyCollection<ISequencePointSourceFileData> GetAllSourceFiles()
	{
		if (!isComplete)
		{
			throw new InvalidOperationException("Cannot retrieve sequence point source files before collection is complete.");
		}
		return allSourceFiles.AsReadOnly();
	}

	private SequencePointInfo GetAdjustedSequencePoint(SequencePointInfo seqPoint)
	{
		SequencePointInfo adjustedSeqPoint = seqPoint;
		if (seqPoint.StartLine == 16707566)
		{
			int startLine = allSequencePoints.Last().StartLine;
			adjustedSeqPoint = new SequencePointInfo(endLine: (seqPoint.EndLine != 16707566) ? seqPoint.EndLine : allSequencePoints.Last().EndLine, method: seqPoint.Method, kind: seqPoint.Kind, sourceFile: seqPoint.SourceFile, sourceFileHash: seqPoint.SourceFileHash, startLine: startLine, startColumn: seqPoint.StartColumn, endColumn: seqPoint.EndColumn, ilOffset: seqPoint.IlOffset);
		}
		return adjustedSeqPoint;
	}

	private void AddSequencePointToMethod(SequencePointInfo adjustedSeqPoint)
	{
		if (!sequencePointsByMethod.TryGetValue(adjustedSeqPoint.Method, out var methodSequencePoints))
		{
			methodSequencePoints = (sequencePointsByMethod[adjustedSeqPoint.Method] = new HashSet<SequencePointInfo>(s_seqPointComparer));
		}
		methodSequencePoints.Add(adjustedSeqPoint);
	}

	private int AddAndIndexSequencePoint(SequencePointInfo seqPoint, SequencePointInfo adjustedSeqPoint)
	{
		int index = allSequencePoints.Count;
		allSequencePoints.Add(adjustedSeqPoint);
		seqPointIndexes.Add(seqPoint, index);
		if (!s_seqPointComparer.Equals(seqPoint, adjustedSeqPoint))
		{
			seqPointIndexes.Add(adjustedSeqPoint, index);
		}
		if (!sourceFileIndexes.ContainsKey(seqPoint.SourceFile))
		{
			sourceFileIndexes.Add(seqPoint.SourceFile, sourceFileIndexes.Count);
			allSourceFiles.Add(new SourceFileData(seqPoint.SourceFile, seqPoint.SourceFileHash));
		}
		return index;
	}

	public ReadOnlyCollection<string> GetAllContextInfoStrings()
	{
		return _variableNames.KeysSortedByValue();
	}

	public ReadOnlyCollection<VariableData> GetVariables()
	{
		return _variables.AsReadOnly();
	}

	public ReadOnlyCollection<Range> GetScopes()
	{
		return _scopes.AsReadOnly();
	}

	public bool TryGetScopeRange(MethodDefinition method, out Range range)
	{
		return _scopeMap.TryGetValue(method, out range);
	}

	public bool MethodHasSequencePoints(MethodDefinition method)
	{
		if (!sequencePointsByMethod.TryGetValue(method, out var methodSequencePoints))
		{
			return false;
		}
		return methodSequencePoints.Count > 0;
	}

	public bool MethodHasPausePointAtOffset(MethodDefinition method, int offset)
	{
		if (!pausePointsByMethod.TryGetValue(method, out var offsets))
		{
			return false;
		}
		return offsets.Contains(offset);
	}

	public bool TryGetVariableRange(MethodDefinition method, out Range range)
	{
		return _variableMap.TryGetValue(method, out range);
	}

	public void AddVariables(PrimaryCollectionContext context, MethodDefinition method)
	{
		if (!method.Body.HasVariables)
		{
			return;
		}
		int scopeStart = _scopes.Count;
		int scopeCount = 0;
		int[] variableScopes = new int[method.Body.Variables.Count];
		foreach (ScopeDebugInfo scope in method.DebugInformation.GetScopes())
		{
			int endOffset = ((scope.End.IsEndOfMethod || scope.End.Offset == 0) ? method.Body.CodeSize : scope.End.Offset);
			int scopeIndex = _scopes.Count;
			_scopes.Add(new Range(scope.Start.Offset, endOffset));
			scopeCount++;
			foreach (VariableDebugInfo variable in scope.Variables)
			{
				variableScopes[variable.Index] = scopeIndex;
			}
		}
		int variableStart = _variables.Count;
		int variableCount = 0;
		foreach (VariableDefinition variable2 in method.Body.Variables)
		{
			if (method.DebugInformation.TryGetName(variable2, out var name))
			{
				if (!_variableNames.TryGetValue(name, out var variableNameIndex))
				{
					variableNameIndex = _variableNames.Count;
					_variableNames.Add(name, variableNameIndex);
				}
				variableCount++;
				_variables.Add(new VariableData(context.Global.Collectors.Types.Add(variable2.VariableType), variableNameIndex, variableScopes[variable2.Index]));
			}
		}
		if (variableCount != 0)
		{
			_variableMap.Add(method, new Range(variableStart, variableCount));
		}
		if (scopeCount != 0)
		{
			_scopeMap.Add(method, new Range(scopeStart, scopeCount));
		}
	}
}
