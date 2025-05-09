using System;
using System.Text;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components;

public sealed class ErrorInformationComponent : StatefulComponentBase<IErrorInformationService, object, ErrorInformationComponent>, IErrorInformationService
{
	private TypeDefinition _type;

	private MethodDefinition _method;

	private FieldDefinition _field;

	private PropertyDefinition _property;

	private EventDefinition _event;

	public TypeDefinition CurrentType
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				ClearAllTypeChildren();
				_type = value;
			}
		}
	}

	public MethodDefinition CurrentMethod
	{
		get
		{
			return _method;
		}
		set
		{
			if (_method != value)
			{
				ClearAllTypeChildren();
				_method = value;
				_type = _method.DeclaringType;
			}
		}
	}

	public FieldDefinition CurrentField
	{
		get
		{
			return _field;
		}
		set
		{
			if (_field != value)
			{
				ClearAllTypeChildren();
				_field = value;
				_type = _field.DeclaringType;
			}
		}
	}

	public PropertyDefinition CurrentProperty
	{
		get
		{
			return _property;
		}
		set
		{
			if (_property != value)
			{
				ClearAllTypeChildren();
				_property = value;
				_type = _property.DeclaringType;
			}
		}
	}

	public EventDefinition CurrentEvent
	{
		get
		{
			return _event;
		}
		set
		{
			if (_event != value)
			{
				ClearAllTypeChildren();
				_event = value;
				_type = _event.DeclaringType;
			}
		}
	}

	public Instruction CurrentInstruction { get; set; }

	private void ClearAllTypeChildren()
	{
		_method = null;
		_field = null;
		_property = null;
		_event = null;
		CurrentInstruction = null;
	}

	protected override void DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendValue(builder, "_type", _type);
		CollectorStateDumper.AppendValue(builder, "_method", _method);
		CollectorStateDumper.AppendValue(builder, "_field", _field);
		CollectorStateDumper.AppendValue(builder, "_event", _event);
		CollectorStateDumper.AppendValue(builder, "_property", _property);
	}

	protected override void HandleMergeForAdd(ErrorInformationComponent forked)
	{
	}

	protected override void HandleMergeForMergeValues(ErrorInformationComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotSupportedException();
	}

	protected override void SyncPooledInstanceWithParent(ErrorInformationComponent parent)
	{
		throw new NotSupportedException();
	}

	protected override ErrorInformationComponent CreateEmptyInstance()
	{
		return new ErrorInformationComponent();
	}

	protected override ErrorInformationComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override ErrorInformationComponent CreatePooledInstance()
	{
		throw new NotSupportedException();
	}

	protected override ErrorInformationComponent ThisAsFull()
	{
		return this;
	}

	protected override object ThisAsRead()
	{
		throw new NotSupportedException();
	}

	protected override IErrorInformationService GetNotAvailableWrite()
	{
		throw new NotSupportedException();
	}

	protected override object GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IErrorInformationService writer, out object reader, out ErrorInformationComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IErrorInformationService writer, out object reader, out ErrorInformationComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IErrorInformationService writer, out object reader, out ErrorInformationComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IErrorInformationService writer, out object reader, out ErrorInformationComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}
}
