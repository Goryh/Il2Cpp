using System.Text;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class ByReferenceType : TypeSpecification
{
	private string _fullName;

	public override bool IsValueType => false;

	public override bool IsByReference => true;

	public override MetadataType MetadataType => MetadataType.ByReference;

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

	internal ByReferenceType(TypeReference elementType, TypeContext context)
		: base(elementType, context)
	{
		using Returnable<StringBuilder> builderContext = context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		builder.Append(elementType.Name);
		builder.Append('&');
		InitializeName(builder.ToString());
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return RuntimeStorageKind.Pointer;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return base.ElementType.GetRuntimeFieldLayout(typeFactory);
	}
}
