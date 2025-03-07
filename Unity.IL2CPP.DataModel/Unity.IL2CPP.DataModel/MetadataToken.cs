using System;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public readonly struct MetadataToken : IEquatable<MetadataToken>
{
	private readonly uint _token;

	public static readonly MetadataToken TypeDefZero = new MetadataToken(TokenType.TypeDef, 0u);

	public static readonly MetadataToken TypeSpecZero = new MetadataToken(TokenType.TypeSpec, 0u);

	public static readonly MetadataToken MethodSpecZero = new MetadataToken(TokenType.MethodSpec, 0u);

	public static readonly MetadataToken MethodDefZero = new MetadataToken(TokenType.Method, 0u);

	public static readonly MetadataToken FieldDefZero = new MetadataToken(TokenType.Field, 0u);

	public static readonly MetadataToken PropertyDefZero = new MetadataToken(TokenType.Property, 0u);

	public static readonly MetadataToken EventDefZero = new MetadataToken(TokenType.Event, 0u);

	public static readonly MetadataToken MemberRefZero = new MetadataToken(TokenType.MemberRef, 0u);

	public static readonly MetadataToken ParamZero = new MetadataToken(TokenType.Param, 0u);

	public static readonly MetadataToken AssemblyZero = new MetadataToken(TokenType.Assembly, 0u);

	public static readonly MetadataToken ModuleZero = new MetadataToken(TokenType.Module, 0u);

	public static readonly MetadataToken InterfaceImplementationZero = new MetadataToken(TokenType.InterfaceImpl, 0u);

	public static readonly MetadataToken GenericParameterConstraintZero = new MetadataToken(TokenType.GenericParamConstraint, 0u);

	public uint RID => _token & 0xFFFFFF;

	public TokenType TokenType => (TokenType)(_token & 0xFF000000u);

	internal static MetadataToken FromCecil(Mono.Cecil.IMetadataTokenProvider tokenProvider)
	{
		return new MetadataToken(tokenProvider.MetadataToken.ToUInt32());
	}

	private MetadataToken(uint token)
	{
		_token = token;
	}

	internal MetadataToken(TokenType tokenType, uint token)
		: this((uint)tokenType | token)
	{
	}

	public int ToInt32()
	{
		return (int)_token;
	}

	public uint ToUInt32()
	{
		return _token;
	}

	public override int GetHashCode()
	{
		return (int)_token;
	}

	public bool Equals(MetadataToken other)
	{
		return other._token == _token;
	}

	public override bool Equals(object obj)
	{
		if (obj is MetadataToken other)
		{
			return other._token == _token;
		}
		return false;
	}

	public static bool operator ==(MetadataToken one, MetadataToken other)
	{
		return one._token == other._token;
	}

	public static bool operator !=(MetadataToken one, MetadataToken other)
	{
		return one._token != other._token;
	}

	public override string ToString()
	{
		return $"[{TokenType}:0x{RID:x4}]";
	}
}
