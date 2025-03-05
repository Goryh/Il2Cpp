using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public class InMemoryGeneratedMethodCodeWriter : InMemoryCodeWriter, IGeneratedMethodCodeStream, IGeneratedCodeStream, IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IReadOnlyContextGeneratedCodeStream, ICppCodeStream, ICodeStream, IDisposable, IStream, IGeneratedMethodCodeWriter, IDirectDeclarationAccessForGeneratedMethodCodeWriter
{
	private readonly Dictionary<string, MethodMetadataUsage> _methodMetadataUsages = new Dictionary<string, MethodMetadataUsage>();

	public ReadOnlyDictionary<string, MethodMetadataUsage> MethodMetadataUsages => _methodMetadataUsages.AsReadOnly();

	public bool ErrorOccurred { get; set; }

	public virtual NPath FileName => null;

	public InMemoryGeneratedMethodCodeWriter(SourceWritingContext context)
		: base(context)
	{
	}

	public void AddMetadataUsage(string identifier, MethodMetadataUsage usage)
	{
		_methodMetadataUsages.Add(identifier, usage);
	}

	public void Write(IGeneratedMethodCodeStream other)
	{
		foreach (KeyValuePair<string, MethodMetadataUsage> usage in other.MethodMetadataUsages)
		{
			_methodMetadataUsages.Add(usage.Key, usage.Value);
		}
		_cppDeclarations.Add(other.Declarations);
		base.Writer.Flush();
		other.Writer.Flush();
		Stream baseStream = other.Writer.BaseStream;
		long originalPosition = baseStream.Position;
		baseStream.Seek(0L, SeekOrigin.Begin);
		baseStream.CopyTo(base.Writer.BaseStream);
		baseStream.Seek(originalPosition, SeekOrigin.Begin);
		base.Writer.Flush();
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
