using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Attributes;

public static class AttributeCollection
{
	public static ReadOnlyCollection<AttributeClassCollectionData> BuildAttributeCollection(GlobalPrimaryCollectionContext context, AssemblyDefinition assemblyDefinition)
	{
		List<AttributeClassCollectionData> attributeTypeRanges = new List<AttributeClassCollectionData>();
		foreach (ICustomAttributeProvider customAttributeProvider in AttributeProviderIterator.Iterate(assemblyDefinition))
		{
			List<CustomAttributeCollectionData> attributeData = new List<CustomAttributeCollectionData>(customAttributeProvider.CustomAttributes.Count);
			foreach (CustomAttribute customAttribute in customAttributeProvider.GetConstructibleCustomAttributes())
			{
				MethodDefinition constructor = customAttribute.Constructor;
				ReadOnlyCollection<CustomAttributeArgumentData> args = ReadOnlyCollectionCache<CustomAttributeArgumentData>.Empty;
				ReadOnlyCollection<CustomAttributeNamedArgumentData> fields = ReadOnlyCollectionCache<CustomAttributeNamedArgumentData>.Empty;
				ReadOnlyCollection<CustomAttributeNamedArgumentData> properties = ReadOnlyCollectionCache<CustomAttributeNamedArgumentData>.Empty;
				TypeDefinition attrType = customAttribute.AttributeType.Resolve();
				if (customAttribute.HasConstructorArguments)
				{
					List<CustomAttributeArgumentData> constructorArgsList = new List<CustomAttributeArgumentData>(customAttribute.ConstructorArguments.Count);
					foreach (CustomAttributeArgument arg in customAttribute.ConstructorArguments)
					{
						constructorArgsList.Add(CustomAttributeArgumentData.Create(context, arg, CreateAttributeDataItem(context, arg, string.Empty)));
					}
					args = constructorArgsList.AsReadOnly();
				}
				if (customAttribute.HasFields)
				{
					List<CustomAttributeNamedArgumentData> fieldArgsList = new List<CustomAttributeNamedArgumentData>(customAttribute.Fields.Count);
					foreach (CustomAttributeNamedArgument arg2 in customAttribute.Fields)
					{
						FieldDefinition field = attrType.FindFieldDefinition(arg2.Name);
						if (field != null)
						{
							fieldArgsList.Add(CustomAttributeNamedArgumentData.Create(context, arg2, CreateAttributeDataItem(context, arg2.Argument, arg2.Name), field));
						}
					}
					fields = fieldArgsList.AsReadOnly();
				}
				if (customAttribute.HasProperties)
				{
					List<CustomAttributeNamedArgumentData> propertyArgsList = new List<CustomAttributeNamedArgumentData>(customAttribute.Properties.Count);
					foreach (CustomAttributeNamedArgument arg3 in customAttribute.Properties)
					{
						PropertyDefinition property = attrType.FindPropertyDefinition(arg3.Name);
						if (property != null)
						{
							propertyArgsList.Add(CustomAttributeNamedArgumentData.Create(context, arg3, CreateAttributeDataItem(context, arg3.Argument, arg3.Name), property));
						}
					}
					properties = propertyArgsList.AsReadOnly();
				}
				attributeData.Add(new CustomAttributeCollectionData(constructor, args, fields, properties));
			}
			attributeTypeRanges.Add(new AttributeClassCollectionData(customAttributeProvider.MetadataToken, attributeData.AsReadOnly()));
		}
		return attributeTypeRanges.ToSortedCollectionBy((AttributeClassCollectionData d) => d.MetadataToken);
	}

	private static AttributeDataItem CreateAttributeDataItem(GlobalPrimaryCollectionContext context, CustomAttributeArgument arg, string name)
	{
		object value = arg.Value;
		if (!(value is TypeReference type))
		{
			if (!(value is TypeReference[] typeArray))
			{
				if (!(value is CustomAttributeArgument innerArg))
				{
					if (value is CustomAttributeArgument[] arr)
					{
						ArrayType arrayType = (ArrayType)arg.Type;
						if (arrayType.ElementType.IsSystemObject)
						{
							return new AttributeDataItem(arr.Select((CustomAttributeArgument t) => CustomAttributeArgumentData.Create(context, t, CreateAttributeDataItem(context, t, name))).ToArray());
						}
						if (arrayType.ElementType.IsSystemType)
						{
							return new AttributeDataItem(AttributeDataItemType.TypeArray, arr.Select((CustomAttributeArgument t) => CreateAttributeDataItem(context, t, name)).ToArray());
						}
						if (arrayType.ElementType.IsEnum)
						{
							return new AttributeDataItem(AttributeDataItemType.EnumArray, arr.Select((CustomAttributeArgument t) => CreateAttributeDataItem(context, t, name)).ToArray());
						}
						return new AttributeDataItem(MetadataUtils.ConstantDataFor(arr.Select((CustomAttributeArgument a) => a.Value).ToArray(), arg.Type, name));
					}
					if (arg.Value == null)
					{
						return default(AttributeDataItem);
					}
					return new AttributeDataItem(MetadataUtils.ConstantDataFor(arg.Value, arg.Type, name));
				}
				return CreateAttributeDataItem(context, innerArg, name);
			}
			return new AttributeDataItem(AttributeDataItemType.TypeArray, typeArray.Select((TypeReference t) => new AttributeDataItem(context.Collectors.Types.Add(t))).ToArray());
		}
		return new AttributeDataItem(context.Collectors.Types.Add(type));
	}
}
