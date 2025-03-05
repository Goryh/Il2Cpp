using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components;

public class PathFactoryComponent : CompletableStatefulComponentBase<IFileResults, IPathFactoryService, PathFactoryComponent>, IPathFactoryService
{
	private class NotAvailable : IPathFactoryService
	{
		public string GetFileNameForAssembly(AssemblyDefinition assembly, string fileName)
		{
			throw new NotSupportedException();
		}

		public NPath GetFilePath(FileCategory category, NPath filePath)
		{
			throw new NotSupportedException();
		}
	}

	private class Results : IFileResults
	{
		public ReadOnlyCollection<NPath> PerAssembly { get; init; }

		public ReadOnlyCollection<NPath> Generics { get; init; }

		public ReadOnlyCollection<NPath> Metadata { get; init; }

		public ReadOnlyCollection<NPath> Debugger { get; init; }

		public ReadOnlyCollection<NPath> Other { get; init; }
	}

	private const string PrefixAndNameSeparator = "_";

	private readonly string _perAssemblyFileNamePrefix;

	private readonly List<NPath> _perAssembly = new List<NPath>();

	private readonly List<NPath> _generics = new List<NPath>();

	private readonly List<NPath> _metadata = new List<NPath>();

	private readonly List<NPath> _other = new List<NPath>();

	private readonly List<NPath> _debugger = new List<NPath>();

	public PathFactoryComponent()
	{
		_perAssemblyFileNamePrefix = null;
	}

	public PathFactoryComponent(string perAssemblyFileNamePrefix)
	{
		_perAssemblyFileNamePrefix = perAssemblyFileNamePrefix;
	}

	public static string GenerateFileNamePrefixForAssembly(AssemblyDefinition assembly)
	{
		return Path.GetFileNameWithoutExtension(assembly.MainModule.GetModuleFileName());
	}

	protected override void HandleMergeForAdd(PathFactoryComponent forked)
	{
		_perAssembly.AddRange(forked._perAssembly);
		_generics.AddRange(forked._generics);
		_metadata.AddRange(forked._metadata);
		_other.AddRange(forked._other);
		_debugger.AddRange(forked._debugger);
	}

	protected override void HandleMergeForMergeValues(PathFactoryComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotSupportedException();
	}

	protected override void SyncPooledInstanceWithParent(PathFactoryComponent parent)
	{
	}

	protected override PathFactoryComponent CreateEmptyInstance()
	{
		return new PathFactoryComponent(_perAssemblyFileNamePrefix);
	}

	protected override PathFactoryComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override PathFactoryComponent CreatePooledInstance()
	{
		throw new NotSupportedException();
	}

	protected override PathFactoryComponent ThisAsFull()
	{
		return this;
	}

	protected override IFileResults GetResults()
	{
		return new Results
		{
			PerAssembly = _perAssembly.AsReadOnly(),
			Generics = _generics.AsReadOnly(),
			Metadata = _metadata.AsReadOnly(),
			Debugger = _debugger.AsReadOnly(),
			Other = _other.AsReadOnly()
		};
	}

	protected override IPathFactoryService ThisAsRead()
	{
		return this;
	}

	protected override IPathFactoryService GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override IPathFactoryService GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IPathFactoryService writer, out object reader, out PathFactoryComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IPathFactoryService writer, out object reader, out PathFactoryComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IPathFactoryService writer, out object reader, out PathFactoryComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IPathFactoryService writer, out object reader, out PathFactoryComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	public string GetFileNameForAssembly(AssemblyDefinition assembly, string fileName)
	{
		return GenerateFileNameWithPreFix(GenerateFileNamePrefixForAssembly(assembly), fileName);
	}

	public NPath GetFilePath(FileCategory category, NPath filePath)
	{
		NPath path = GetFilePath(filePath);
		switch (category)
		{
		case FileCategory.Generics:
			_generics.Add(path);
			break;
		case FileCategory.PerAssembly:
			_perAssembly.Add(path);
			break;
		case FileCategory.Metadata:
			_metadata.Add(path);
			break;
		case FileCategory.Other:
			_other.Add(path);
			break;
		case FileCategory.Debugger:
			_debugger.Add(path);
			break;
		default:
			throw new ArgumentException($"Unhandled {"FileCategory"} {category}");
		}
		return path;
	}

	private NPath GetFilePath(NPath filePath)
	{
		if (_perAssemblyFileNamePrefix == null)
		{
			return filePath;
		}
		if (filePath.FileName.StartsWith(_perAssemblyFileNamePrefix))
		{
			return filePath;
		}
		return filePath.Parent.Combine(GenerateFileNameWithPreFix(_perAssemblyFileNamePrefix, filePath.FileName));
	}

	private static string GenerateFileNameWithPreFix(string prefix, string fileName)
	{
		if (string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(fileName)))
		{
			return prefix + fileName;
		}
		return prefix + "_" + fileName;
	}

	protected override void DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendCollection(builder, "_perAssembly", _perAssembly.ToSortedCollection());
		CollectorStateDumper.AppendCollection(builder, "_generics", _generics.ToSortedCollection());
		CollectorStateDumper.AppendCollection(builder, "_metadata", _metadata.ToSortedCollection());
		CollectorStateDumper.AppendCollection(builder, "_debugger", _debugger.ToSortedCollection());
		CollectorStateDumper.AppendCollection(builder, "_other", _other.ToSortedCollection());
	}
}
