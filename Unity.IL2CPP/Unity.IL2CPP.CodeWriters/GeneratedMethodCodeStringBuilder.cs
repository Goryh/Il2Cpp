using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public class GeneratedMethodCodeStringBuilder : GeneratedCodeStringBuilder, IGeneratedMethodCodeWriter, IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IDirectDeclarationAccessForGeneratedMethodCodeWriter
{
	private readonly Dictionary<string, MethodMetadataUsage> _methodMetadataUsages = new Dictionary<string, MethodMetadataUsage>();

	public ReadOnlyDictionary<string, MethodMetadataUsage> MethodMetadataUsages => _methodMetadataUsages.AsReadOnly();

	public bool ErrorOccurred { get; set; }

	public GeneratedMethodCodeStringBuilder(SourceWritingContext context, StringBuilder builder)
		: base(context, builder)
	{
	}

	public void AddMetadataUsage(string identifier, MethodMetadataUsage usage)
	{
		_methodMetadataUsages.Add(identifier, usage);
	}

	public void Write(IGeneratedMethodCodeStream other)
	{
		throw new NotImplementedException();
	}

	void IDirectDeclarationAccessForGeneratedMethodCodeWriter.AddVirtualMethodDeclarationData(VirtualMethodDeclarationData data)
	{
		_cppDeclarations._virtualMethods.Add(data);
	}

	bool IDirectDeclarationAccessForGeneratedMethodCodeWriter.AddMethodDeclaration(MethodReference method)
	{
		return _cppDeclarations._methods.Add(method);
	}

	bool IDirectDeclarationAccessForGeneratedMethodCodeWriter.AddSharedMethodDeclaration(MethodReference method)
	{
		return _cppDeclarations._sharedMethods.Add(method);
	}

	void IDirectDeclarationAccessForGeneratedMethodCodeWriter.TryAddInternalPInvokeMethodDeclarationsForForcedInternalPInvoke(string methodName, string pinvokeDeclaration)
	{
		if (!_cppDeclarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.ContainsKey(methodName))
		{
			_cppDeclarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Add(methodName, pinvokeDeclaration);
		}
	}

	void IDirectDeclarationAccessForGeneratedMethodCodeWriter.TryAddInternalPInvokeMethodDeclarations(string methodName, string pinvokeDeclaration)
	{
		if (!_cppDeclarations._internalPInvokeMethodDeclarations.ContainsKey(methodName))
		{
			_cppDeclarations._internalPInvokeMethodDeclarations.Add(methodName, pinvokeDeclaration);
		}
	}
}
