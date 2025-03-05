using System;
using NiceIO;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public static class ContextSourceCodeWriterExtensions
{
	public static IGeneratedMethodCodeStream CreateManagedSourceWriter(this SourceWritingContext context, FileCategory category, NPath filename)
	{
		return context.CreateManagedSourceWriter(category, filename, createProfilerSection: false);
	}

	public static IGeneratedMethodCodeStream CreateProfiledManagedSourceWriter(this SourceWritingContext context, FileCategory category, NPath filename)
	{
		return context.CreateManagedSourceWriter(category, filename, createProfilerSection: true);
	}

	public static IGeneratedMethodCodeStream CreateProfiledManagedSourceWriterInOutputDirectory(this SourceWritingContext context, FileCategory category, string filename)
	{
		return context.CreateManagedSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
	}

	public static IGeneratedMethodCodeStream CreateManagedSourceWriterInOutputDirectory(this SourceWritingContext context, FileCategory category, string filename)
	{
		return context.CreateManagedSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
	}

	public static ICppCodeStream CreateProfiledSourceWriterInOutputDirectory(this SourceWritingContext context, FileCategory category, string filename)
	{
		return context.AsMinimal().CreateSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
	}

	public static ICppCodeStream CreateSourceWriterInOutputDirectory(this SourceWritingContext context, FileCategory category, string filename)
	{
		return context.AsMinimal().CreateSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
	}

	public static IGeneratedCodeStream CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(this SourceWritingContext context, FileCategory category, string filename)
	{
		return context.CreateGeneratedCodeSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
	}

	public static IGeneratedCodeStream CreateProfiledGeneratedCodeSourceWriter(this SourceWritingContext context, FileCategory category, NPath filename)
	{
		return context.CreateGeneratedCodeSourceWriter(category, filename, createProfilerSection: true);
	}

	public static ICppCodeStream CreateProfiledSourceWriterInOutputDirectory(this MinimalContext context, FileCategory category, string filename)
	{
		return context.CreateSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
	}

	public static ICppCodeStream CreateSourceWriterInOutputDirectory(this MinimalContext context, FileCategory category, string filename)
	{
		return context.CreateSourceWriter(category, context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
	}

	private static IGeneratedMethodCodeStream CreateManagedSourceWriter(this SourceWritingContext context, FileCategory category, NPath filename, bool createProfilerSection)
	{
		NPath finalFilePath = context.Global.Services.PathFactory.GetFilePath(category, filename);
		object obj;
		if (!createProfilerSection)
		{
			obj = null;
		}
		else
		{
			IDisposable disposable = context.Global.Services.TinyProfiler.Section(finalFilePath.FileName);
			obj = disposable;
		}
		IDisposable profilerSection = (IDisposable)obj;
		return new ManagedSourceCodeWriter(context, finalFilePath, profilerSection);
	}

	private static ICppCodeStream CreateSourceWriter(this MinimalContext context, FileCategory category, NPath filename, bool createProfilerSection)
	{
		NPath finalFilePath = context.Global.Services.PathFactory.GetFilePath(category, filename);
		object obj;
		if (!createProfilerSection)
		{
			obj = null;
		}
		else
		{
			IDisposable disposable = context.Global.Services.TinyProfiler.Section(finalFilePath.FileName);
			obj = disposable;
		}
		IDisposable profilerSection = (IDisposable)obj;
		return new SourceCodeWriter(context, finalFilePath, profilerSection);
	}

	private static IGeneratedCodeStream CreateGeneratedCodeSourceWriter(this SourceWritingContext context, FileCategory category, NPath filename, bool createProfilerSection)
	{
		NPath finalFilePath = context.Global.Services.PathFactory.GetFilePath(category, filename);
		object obj;
		if (!createProfilerSection)
		{
			obj = null;
		}
		else
		{
			IDisposable disposable = context.Global.Services.TinyProfiler.Section(finalFilePath.FileName);
			obj = disposable;
		}
		IDisposable profilerSection = (IDisposable)obj;
		return new GeneratedCodeSourceCodeWriter(context, finalFilePath, profilerSection);
	}
}
