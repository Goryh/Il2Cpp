using System;
using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;

namespace Unity.IL2CPP.DataModel;

public class GeneratedTypesAssemblyDefinition : AssemblyDefinition
{
	internal const string AssemblyName = "__Generated";

	public GeneratedTypesAssemblyDefinition(TypeContext context, AssemblyDefinition systemAssembly)
		: base("__Generated", new Version(0, 0, 0, 0), context, MetadataToken.AssemblyZero)
	{
		InitializeMainModule(new ModuleDefinition(this, new TypeSystem(_context), "__Generated", null, MetadataToken.ModuleZero, ModuleKind.Dll, MetadataKind.Ecma335, hasSymbols: false, ReadOnlyCollectionCache<CustomAttribute>.Empty));
		InitializeCustomAttributes(ReadOnlyCollectionCache<CustomAttribute>.Empty);
		InitializeEntryPoint(null);
		InitializeReferences(new AssemblyDefinition[1] { systemAssembly }.AsReadOnly());
		InitializeMembers(new List<TypeDefinition>(), new List<MethodDefinition>(), ReadOnlyCollectionCache<GenericParameter>.Empty, new List<FieldDefinition>(), new List<EventDefinition>(), new List<PropertyDefinition>());
		base.MainModule.InitializeResources(ReadOnlyCollectionCache<Resource>.Empty);
		base.MainModule.InitializeAssemblyReferences(new AssemblyNameReference[1] { systemAssembly.Name }.AsReadOnly());
		base.MainModule.InitializeModuleInitializer(null);
		base.MainModule.InitializeExportedTypes(ReadOnlyCollectionCache<TypeReference>.Empty);
	}
}
