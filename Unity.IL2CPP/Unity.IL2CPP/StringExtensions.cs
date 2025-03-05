using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal static class StringExtensions
{
	public static string AggregateWithComma(this IEnumerable<string> elements, ReadOnlyContext context)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		return elements.AggregateWithComma(builderContext.Value);
	}
}
