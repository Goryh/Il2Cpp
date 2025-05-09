using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.Metadata;

public class GenericContextCollector
{
	private readonly Dictionary<IGenericParameterProvider, int> _rgctxEntriesStart = new Dictionary<IGenericParameterProvider, int>();

	private readonly Dictionary<IGenericParameterProvider, int> _rgctxEntriesCount = new Dictionary<IGenericParameterProvider, int>();

	private readonly List<RGCTXEntry> _rgctxEntries = new List<RGCTXEntry>();

	private readonly GlobalWriteContext _context;

	private GenericContextCollector(GlobalWriteContext context)
	{
		_context = context;
	}

	public static GenericContextCollection Collect(GlobalWriteContext context, AssemblyDefinition assembly, GenericSharingAnalysisResults genericSharingAnalysis)
	{
		return new GenericContextCollector(context).CollectInternal(assembly, genericSharingAnalysis);
	}

	private GenericContextCollection CollectInternal(AssemblyDefinition assembly, GenericSharingAnalysisResults genericSharingAnalysis)
	{
		foreach (TypeDefinition type in assembly.GetAllTypes())
		{
			if (type.HasGenericParameters)
			{
				ReadOnlyCollection<RuntimeGenericData> runtimeGenericDatas = genericSharingAnalysis.RuntimeGenericContextFor(type).RuntimeGenericDatas;
				if (runtimeGenericDatas.Count > 0)
				{
					_rgctxEntriesCount.Add(type, runtimeGenericDatas.Count);
					_rgctxEntriesStart.Add(type, _rgctxEntries.Count);
					_rgctxEntries.AddRange(runtimeGenericDatas.Select(CreateRGCTXEntry));
				}
			}
			foreach (MethodDefinition method in type.Methods)
			{
				if (method.HasGenericParameters)
				{
					ReadOnlyCollection<RuntimeGenericData> runtimeGenericDatas2 = genericSharingAnalysis.RuntimeGenericContextFor(method).RuntimeGenericDatas;
					if (runtimeGenericDatas2.Count > 0)
					{
						_rgctxEntriesCount.Add(method, runtimeGenericDatas2.Count);
						_rgctxEntriesStart.Add(method, _rgctxEntries.Count);
						_rgctxEntries.AddRange(runtimeGenericDatas2.Select(CreateRGCTXEntry));
					}
				}
			}
		}
		return new GenericContextCollection(_rgctxEntries.AsReadOnly(), _rgctxEntriesStart.AsReadOnly(), _rgctxEntriesCount.AsReadOnly());
	}

	private RGCTXEntry CreateRGCTXEntry(RuntimeGenericData data)
	{
		RuntimeGenericTypeData typeData = data as RuntimeGenericTypeData;
		RuntimeGenericMethodData methodData = data as RuntimeGenericMethodData;
		RuntimeGenericConstrainedCallData constrained = data as RuntimeGenericConstrainedCallData;
		switch (data.InfoType)
		{
		case RuntimeGenericContextInfo.Class:
		case RuntimeGenericContextInfo.Static:
			return RGCTXClassEntryFor(typeData);
		case RuntimeGenericContextInfo.Type:
			return RGCTXTypeEntryFor(typeData);
		case RuntimeGenericContextInfo.Array:
			return RGCTXArrayEntryFor(typeData);
		case RuntimeGenericContextInfo.Method:
			return RGCTXMethodEntryFor(methodData.GenericMethod);
		case RuntimeGenericContextInfo.Constrained:
			return RGCTXConstrainedEntryFor(constrained.ConstrainedType, constrained.ConstrainedMethod);
		default:
			throw new ArgumentOutOfRangeException("Invalid type of runtime generic data found - this should not happen.");
		}
	}

	private RGCTXEntry RGCTXClassEntryFor(RuntimeGenericTypeData typeData)
	{
		return new RGCTXEntry(RGCTXType.Class, _context.Collectors.Types.Add(typeData.GenericType));
	}

	private RGCTXEntry RGCTXTypeEntryFor(RuntimeGenericTypeData typeData)
	{
		return new RGCTXEntry(RGCTXType.Type, _context.Collectors.Types.Add(typeData.GenericType));
	}

	private RGCTXEntry RGCTXArrayEntryFor(RuntimeGenericTypeData typeData)
	{
		return RGCTXClassEntryFor(new RuntimeGenericTypeData(RuntimeGenericContextInfo.Class, _context.Services.TypeFactory.CreateArrayType(typeData.GenericType, 1)));
	}

	private RGCTXEntry RGCTXMethodEntryFor(MethodReference method)
	{
		return new RGCTXEntry(RGCTXType.Method, method);
	}

	private RGCTXEntry RGCTXConstrainedEntryFor(TypeReference constrainedType, MethodReference constrainedMethod)
	{
		return new RGCTXEntry(RGCTXType.Constrained, _context.Collectors.Types.Add(constrainedType), constrainedMethod);
	}
}
