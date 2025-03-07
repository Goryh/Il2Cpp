using System.Text;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class ArrayType : TypeSpecification
{
	private string _fullName;

	public int Rank { get; }

	public bool IsVector { get; }

	public override bool IsValueType => false;

	public override bool IsArray => true;

	public override bool IsInterface => false;

	public override bool IsEnum => false;

	public override bool IsAttribute => false;

	public override bool IsComInterface => false;

	public override MetadataType MetadataType => MetadataType.Array;

	public override bool IsByRefLike => false;

	public override bool IsAbstract => false;

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

	internal ArrayType(TypeReference elementType, int rank, bool isVector, TypeContext context)
		: base(elementType, context)
	{
		Rank = rank;
		IsVector = isVector;
		using Returnable<StringBuilder> builderContext = context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		builder.Append(elementType.Name);
		NamingUtils.AppendArraySuffix(isVector, rank, builder);
		InitializeName(builder.ToString());
	}

	public string RankOnlyName()
	{
		return Name;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return RuntimeStorageKind.ReferenceType;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Fixed;
	}
}
