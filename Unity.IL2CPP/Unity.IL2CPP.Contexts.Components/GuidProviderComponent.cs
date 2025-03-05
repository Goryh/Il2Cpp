using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Components;

public class GuidProviderComponent : ReusedServiceComponentBase<IGuidProvider, GuidProviderComponent>, IGuidProvider
{
	private static readonly ReadOnlyCollection<byte> kParameterizedNamespaceGuid = new byte[16]
	{
		17, 244, 122, 213, 123, 115, 66, 192, 171, 174,
		135, 139, 30, 22, 173, 238
	}.AsReadOnly();

	public Guid GuidFor(ReadOnlyContext context, TypeReference type)
	{
		if (type is GenericInstanceType genericInstanceType)
		{
			return ParameterizedGuidFromTypeIdentifier(IdentifierFor(context, genericInstanceType));
		}
		if (type is TypeSpecification || type is GenericParameter)
		{
			throw new InvalidOperationException("Cannot retrieve GUID for " + type.FullName);
		}
		TypeDefinition typeDef = type.Resolve();
		TypeDefinition guidAttribute = context.Global.Services.TypeProvider.GetSystemType(SystemType.GuidAttribute);
		CustomAttribute attribute = typeDef.CustomAttributes.SingleOrDefault((CustomAttribute a) => a.AttributeType == guidAttribute);
		if (attribute != null)
		{
			return new Guid((string)attribute.ConstructorArguments[0].Value);
		}
		attribute = typeDef.CustomAttributes.SingleOrDefault((CustomAttribute a) => a.AttributeType.Name == "GuidAttribute" && a.AttributeType.Namespace == "Windows.Foundation.Metadata");
		if (attribute != null)
		{
			return new Guid((uint)attribute.ConstructorArguments[0].Value, (ushort)attribute.ConstructorArguments[1].Value, (ushort)attribute.ConstructorArguments[2].Value, (byte)attribute.ConstructorArguments[3].Value, (byte)attribute.ConstructorArguments[4].Value, (byte)attribute.ConstructorArguments[5].Value, (byte)attribute.ConstructorArguments[6].Value, (byte)attribute.ConstructorArguments[7].Value, (byte)attribute.ConstructorArguments[8].Value, (byte)attribute.ConstructorArguments[9].Value, (byte)attribute.ConstructorArguments[10].Value);
		}
		throw new InvalidOperationException("'" + type.FullName + "' doesn't have a GUID.");
	}

	private static Guid ParameterizedGuidFromTypeIdentifier(string typeIdentifier)
	{
		List<byte> bytesToHash = new List<byte>();
		bytesToHash.AddRange(kParameterizedNamespaceGuid);
		bytesToHash.AddRange(Encoding.UTF8.GetBytes(typeIdentifier));
		byte[] sha1Hash;
		using (SHA1 sha1 = SHA1.Create())
		{
			sha1Hash = sha1.ComputeHash(bytesToHash.ToArray());
		}
		int resultPartA = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(sha1Hash, 0));
		short resultPartB = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(sha1Hash, 4));
		short resultPartC = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(sha1Hash, 6));
		byte[] resultPartD = sha1Hash.Skip(8).Take(8).ToArray();
		resultPartC &= 0xFFF;
		resultPartC |= 0x5000;
		resultPartD[0] &= 63;
		resultPartD[0] |= 128;
		return new Guid(resultPartA, resultPartB, resultPartC, resultPartD);
	}

	public string IdentifierFor(ReadOnlyContext context, IEnumerable<TypeReference> nameElements)
	{
		return nameElements.Select((TypeReference element) => IdentifierFor(context, element)).AggregateWith(";");
	}

	private string IdentifierFor(ReadOnlyContext context, TypeReference type)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
			return "b1";
		case MetadataType.Char:
			return "c2";
		case MetadataType.Byte:
			return "u1";
		case MetadataType.Int16:
			return "i2";
		case MetadataType.UInt16:
			return "u2";
		case MetadataType.Int32:
			return "i4";
		case MetadataType.UInt32:
			return "u4";
		case MetadataType.Int64:
			return "i8";
		case MetadataType.UInt64:
			return "u8";
		case MetadataType.Single:
			return "f4";
		case MetadataType.Double:
			return "f8";
		case MetadataType.String:
			return "string";
		case MetadataType.Object:
			return "cinterface(IInspectable)";
		case MetadataType.ValueType:
			if (type.FullName == "System.Guid")
			{
				return "g16";
			}
			break;
		}
		TypeDefinition typeDef = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type.Resolve());
		if (type.MetadataType != MetadataType.Class && type.MetadataType != MetadataType.ValueType && type.MetadataType != MetadataType.GenericInstance)
		{
			throw new InvalidOperationException($"Cannot compute type identifier for {type.FullName}, as its metadata type is not supported: {type.MetadataType}.");
		}
		if (!typeDef.IsExposedToWindowsRuntime())
		{
			throw new InvalidOperationException("Cannot compute type identifier for " + type.FullName + ", as it is not a Windows Runtime type.");
		}
		if (type is GenericInstanceType genericInstance)
		{
			return $"pinterface({{{GuidFor(context, typeDef).ToString()}}};{IdentifierFor(context, genericInstance.GenericArguments)})";
		}
		if (typeDef.MetadataType == MetadataType.ValueType)
		{
			if (!typeDef.IsEnum)
			{
				IEnumerable<TypeReference> fieldTypes = from f in typeDef.Fields
					where !f.IsStatic
					select f.FieldType;
				return $"struct({typeDef.FullName};{IdentifierFor(context, fieldTypes)})";
			}
			return $"enum({typeDef.FullName};{IdentifierFor(context, typeDef.GetUnderlyingEnumType())})";
		}
		if (typeDef.IsInterface)
		{
			return "{" + GuidFor(context, typeDef).ToString() + "}";
		}
		if (typeDef.IsDelegate)
		{
			return "delegate({" + GuidFor(context, typeDef).ToString() + "})";
		}
		TypeReference defaultInterface = typeDef.ExtractDefaultInterface();
		if (defaultInterface is GenericInstanceType genericDefaultInterfaceInstance)
		{
			return $"rc({typeDef.FullName};{IdentifierFor(context, genericDefaultInterfaceInstance)})";
		}
		return $"rc({typeDef.FullName};{{{GuidFor(context, defaultInterface).ToString()}}})";
	}

	protected override GuidProviderComponent ThisAsFull()
	{
		return this;
	}

	protected override IGuidProvider ThisAsRead()
	{
		return this;
	}
}
