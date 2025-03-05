using System;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;

namespace Unity.IL2CPP.Marshaling;

public struct ManagedMarshalValue
{
	private readonly string _objectVariableName;

	private readonly FieldReference _field;

	private readonly string _indexVariableName;

	public ManagedMarshalValue Dereferenced => new ManagedMarshalValue(Emit.Dereference(_objectVariableName), _field, _indexVariableName);

	public ManagedMarshalValue(string objectVariableName)
	{
		_objectVariableName = objectVariableName;
		_field = null;
		_indexVariableName = null;
	}

	public ManagedMarshalValue(string objectVariableName, FieldReference field)
	{
		_objectVariableName = objectVariableName;
		_field = field;
		_indexVariableName = null;
	}

	public ManagedMarshalValue(ManagedMarshalValue arrayValue, string indexVariableName)
	{
		_objectVariableName = arrayValue._objectVariableName;
		_field = arrayValue._field;
		_indexVariableName = indexVariableName;
	}

	public ManagedMarshalValue(string objectVariableName, FieldReference field, string indexVariableName)
	{
		_objectVariableName = objectVariableName;
		_field = field;
		_indexVariableName = indexVariableName;
	}

	public string Load(ReadOnlyContext context)
	{
		if (_indexVariableName != null)
		{
			string managedArray = ((_field == null) ? _objectVariableName : (_objectVariableName + "." + _field.CppName));
			return Emit.LoadArrayElement(managedArray, _indexVariableName, useArrayBoundsCheck: false);
		}
		if (_field != null)
		{
			return _objectVariableName + "." + _field.CppName;
		}
		return _objectVariableName;
	}

	public string LoadAddress(ReadOnlyContext context)
	{
		if (_indexVariableName != null)
		{
			throw new NotSupportedException();
		}
		if (_field != null)
		{
			return "&" + _objectVariableName + "." + _field.CppName;
		}
		return Emit.AddressOf(_objectVariableName);
	}

	public void WriteStore(ICodeWriter writer, string value)
	{
		if (_indexVariableName != null)
		{
			string managedArray = ((_field == null) ? _objectVariableName : (_objectVariableName + "." + _field.CppName));
			writer.WriteStatement(Emit.StoreArrayElement(managedArray, _indexVariableName, value, useArrayBoundsCheck: false));
			return;
		}
		if (_field != null)
		{
			writer.WriteFieldSetter(_field, _objectVariableName + "." + _field.CppName, value);
			return;
		}
		writer.WriteLine($"{_objectVariableName} = {value};");
	}

	public void WriteStore(ICodeWriter writer, string format, params object[] args)
	{
		WriteStore(writer, string.Format(format, args));
	}

	public string GetNiceName(ReadOnlyContext context)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		string objectVariableName = _objectVariableName;
		foreach (char c in objectVariableName)
		{
			if (c != '*')
			{
				builder.AppendClean(c);
			}
		}
		if (_field != null)
		{
			builder.AppendClean(_field.Name);
		}
		if (_indexVariableName != null)
		{
			builder.Append("_item");
		}
		return builder.ToString();
	}

	public override string ToString()
	{
		throw new NotSupportedException();
	}
}
