using System;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;

namespace Unity.IL2CPP.DataModel;

public class SystemImplementedArrayMethod : MethodSpec
{
	private string _fullName;

	public override bool IsStripped => false;

	public override bool IsConstructor => false;

	public override bool HasBody => false;

	public override int CodeSize => 0;

	public override bool IsStatic => false;

	public override UnmanagedCallersOnlyInfo UnmanagedCallersOnlyInfo => null;

	public override bool IsCompilerControlled => false;

	public override MethodAttributes Attributes
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override MethodImplAttributes ImplAttributes
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public new ArrayType DeclaringType => (ArrayType)base.DeclaringType;

	internal override bool RequiresRidForNameUniqueness => false;

	public override TypeReference ReturnType { get; }

	public override ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public override string FullName
	{
		get
		{
			if (_fullName == null)
			{
				Interlocked.CompareExchange(ref _fullName, LazyNameHelpers.GetFullName(this), null);
			}
			return _fullName;
		}
	}

	protected override bool IsFullNameBuilt => _fullName != null;

	internal SystemImplementedArrayMethod(string name, TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameterTypes, ArrayType declaringType)
		: base(declaringType, MethodCallingConvention.Default, hasThis: true, explicitThis: false, MetadataToken.MethodSpecZero)
	{
		InitializeName(name);
		ReturnType = returnType;
		Parameters = parameterTypes;
	}

	internal SystemImplementedArrayMethod Clone(ArrayType newArrayType)
	{
		TypeReference newReturnType = ReturnType;
		if (ShouldReplaceWithElementType(newReturnType))
		{
			newReturnType = newArrayType.ElementType;
		}
		ParameterDefinition[] newParameters = new ParameterDefinition[Parameters.Count];
		for (int i = 0; i < Parameters.Count; i++)
		{
			ParameterDefinition oldParameter = Parameters[i];
			if (ShouldReplaceWithElementType(oldParameter.ParameterType))
			{
				newParameters[i] = new ParameterDefinition(oldParameter.Name, oldParameter.Attributes, oldParameter.Index, oldParameter.CustomAttributes, oldParameter.MarshalInfo, oldParameter.HasConstant, oldParameter.Constant, MetadataToken.ParamZero);
				newParameters[i].InitializeParameterType(newArrayType.ElementType);
			}
			else
			{
				newParameters[i] = oldParameter;
			}
		}
		return new SystemImplementedArrayMethod(Name, newReturnType, newParameters.AsReadOnly(), newArrayType);
	}

	private bool ShouldReplaceWithElementType(TypeReference typeReference)
	{
		if (typeReference.ContainsGenericParameter)
		{
			return typeReference == DeclaringType.ElementType;
		}
		return false;
	}

	public override MethodDefinition Resolve()
	{
		return null;
	}
}
