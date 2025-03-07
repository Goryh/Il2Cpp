using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil.Cil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel;

public class MethodDebugInfo
{
	private readonly ReadOnlyCollection<ScopeDebugInfo> _scopes;

	private ReadOnlyCollection<SequencePoint> _sequencePoints;

	public MethodDefinition Method { get; }

	public bool HasSequencePoints => SequencePoints.Count > 0;

	public ReadOnlyCollection<SequencePoint> SequencePoints
	{
		get
		{
			if (_sequencePoints == null)
			{
				throw new UninitializedDataAccessException($"[{GetType()}] {this}.{"SequencePoints"} has not been initialized yet.");
			}
			return _sequencePoints;
		}
	}

	internal static MethodDebugInfo FromCecil(MethodDefinition method, MethodDebugInformation methodDebugInformation)
	{
		return new MethodDebugInfo(method, (from s in methodDebugInformation.GetScopes()
			select new ScopeDebugInfo(s.Variables.Select((VariableDebugInformation v) => new VariableDebugInfo(v.Name, v.Index)).ToArray().AsReadOnly(), BuildInstructionOffset(s.Start), BuildInstructionOffset(s.End))).ToArray().AsReadOnly());
	}

	private static InstructionOffset BuildInstructionOffset(Mono.Cecil.Cil.InstructionOffset instructionOffset)
	{
		if (!instructionOffset.IsEndOfMethod)
		{
			return new InstructionOffset(instructionOffset.Offset);
		}
		return new InstructionOffset(null);
	}

	public MethodDebugInfo(MethodDefinition method, ReadOnlyCollection<ScopeDebugInfo> scopes)
	{
		_scopes = scopes;
		Method = method;
	}

	public SequencePoint GetSequencePoint(Instruction instruction)
	{
		return instruction?.SequencePoint;
	}

	public ReadOnlyCollection<ScopeDebugInfo> GetScopes()
	{
		return _scopes;
	}

	public bool TryGetName(VariableDefinition variable, out string name)
	{
		name = variable.DebugName;
		return variable.HasDebugName;
	}

	internal void InitializeDebugInformation(ReadOnlyCollection<SequencePoint> sequencePoints)
	{
		_sequencePoints = sequencePoints;
	}
}
