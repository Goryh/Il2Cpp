using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Generics;

namespace Unity.IL2CPP.DataModel.BuildLogic.Inflation;

internal class DefinitionInflater
{
	private readonly TypeContext _context;

	private readonly ITypeFactory _typeFactory;

	public DefinitionInflater(TypeContext context, ITypeFactory typeFactory)
	{
		_context = context;
		_typeFactory = typeFactory;
	}

	public void Process()
	{
		TypeDefinition[] allTypesDefinitions = _context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition a) => a.GetAllTypes()).ToArray();
		TypeDefinition[] array = allTypesDefinitions;
		foreach (TypeDefinition type in array)
		{
			PopulateTypeDefinitionInflatedProperties(_context, _typeFactory, type);
		}
		array = allTypesDefinitions;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (MethodDefinition method in array[i].Methods)
			{
				PopulateMethodDefinitionInflatedProperties(_context, _typeFactory, method);
			}
		}
	}

	internal static void PopulateTypeDefinitionInflatedProperties(TypeContext context, ITypeFactory typeFactory, TypeDefinition typeDef)
	{
		bool hasFullySharableGenericParameters = GenericSharingAnalysis.TypeHasFullySharableGenericParameters(context, typeDef);
		typeDef.InitializeGenericSharingProperties(hasFullySharableGenericParameters, typeDef.HasGenericParameters ? GenericSharingAnalysis.GetFullySharedType(context, typeFactory, typeDef) : null);
		typeDef.InitializeTypeReferenceFieldTypes(typeDef.HasFields ? typeDef.Fields.Select((FieldDefinition f) => new InflatedFieldType(f, f.FieldType)).ToArray().AsReadOnly() : ReadOnlyCollectionCache<InflatedFieldType>.Empty);
	}

	internal static void PopulateMethodDefinitionInflatedProperties(TypeContext context, ITypeFactory typeFactory, MethodDefinition method)
	{
		bool methodAndTypeHaveFullySharableGenericParameters = GenericSharingAnalysis.MethodAndTypeHaveFullySharableGenericParameters(context, method);
		bool hasFullySharableGenericParameters = GenericSharingAnalysis.MethodHasFullySharableGenericParameters(context, method);
		method.InitializeGenericSharingProperties(hasFullySharableGenericParameters, methodAndTypeHaveFullySharableGenericParameters, method.HasFullySharedMethod ? GenericSharingAnalysis.GetFullySharedMethod(context, typeFactory, method) : method);
	}
}
