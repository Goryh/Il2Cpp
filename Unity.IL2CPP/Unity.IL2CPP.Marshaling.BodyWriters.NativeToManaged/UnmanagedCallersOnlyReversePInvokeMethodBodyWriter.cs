using System;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

public class UnmanagedCallersOnlyReversePInvokeMethodBodyWriter : IReversePInvokeMethodBodyWriter
{
	private readonly MethodReference _method;

	public UnmanagedCallersOnlyReversePInvokeMethodBodyWriter(MethodReference method)
	{
		_method = method;
	}

	public void WriteMethodDeclaration(IGeneratedCodeWriter writer)
	{
		foreach (ParameterDefinition parameter in _method.Parameters)
		{
			writer.AddIncludesForTypeReference(writer.Context, parameter.ParameterType);
		}
		writer.AddIncludesForTypeReference(writer.Context, _method.ReturnType);
		MethodSignatureWriter.WriteMethodSignatureRaw(writer.Context, writer, _method);
		writer.WriteLine(";");
	}

	public void WriteMethodDefinition(IGeneratedMethodCodeWriter writer)
	{
		throw new InvalidOperationException("No reverse P/Invoke method needed for [UnmanagedCallersOnly] methods");
	}
}
