using System;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

internal class ICommandProjectedMethodBodyWriter
{
	private readonly MinimalContext _context;

	private readonly TypeDefinition _winrtICommand;

	private readonly TypeDefinition _systemFunc2;

	private readonly MethodDefinition _systemFunc2Ctor;

	private readonly TypeDefinition _systemAction1;

	private readonly MethodDefinition _systemAction1Ctor;

	private readonly GenericInstanceType _systemEventHandler1Instance;

	private readonly TypeDefinition _eventRegistrationToken;

	private readonly MethodDefinition _icommandEventHelperAddDelegateConverter;

	private readonly MethodDefinition _icommandEventHelperRemoveDelegateConverter;

	private readonly MethodDefinition _windowsRuntimeMarshalAddEventHandler;

	private readonly MethodDefinition _windowsRuntimeMarshalRemoveEventHandler;

	public ICommandProjectedMethodBodyWriter(MinimalContext context, TypeContext typeContext, TypeDefinition winrtICommand)
	{
		_context = context;
		_winrtICommand = winrtICommand;
		_systemFunc2 = context.Global.Services.TypeProvider.GetSystemType(SystemType.Func_2);
		if (_systemFunc2 == null)
		{
			ThrowCannotImplementMethodException("System.Func`2");
		}
		_systemFunc2Ctor = _systemFunc2.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
		if (_systemFunc2Ctor == null)
		{
			ThrowCannotImplementMethodException("System.Func`2 constructor");
		}
		_systemAction1 = context.Global.Services.TypeProvider.GetSystemType(SystemType.Action_1);
		if (_systemAction1 == null)
		{
			ThrowCannotImplementMethodException("System.Action`1");
		}
		_systemAction1Ctor = _systemAction1.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
		if (_systemAction1Ctor == null)
		{
			ThrowCannotImplementMethodException("System.Action`1 constructor");
		}
		TypeDefinition systemEventHandler1 = context.Global.Services.TypeProvider.GetSystemType(SystemType.EventHandler_1);
		if (systemEventHandler1 == null)
		{
			ThrowCannotImplementMethodException("System.EventHandler`1");
		}
		_systemEventHandler1Instance = context.Global.Services.TypeFactory.CreateGenericInstanceType(systemEventHandler1, systemEventHandler1.DeclaringType, context.Global.Services.TypeProvider.SystemObject);
		_eventRegistrationToken = typeContext.SystemAssembly.ThisIsSlowFindType("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken")?.Resolve();
		if (_eventRegistrationToken == null)
		{
			ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken");
		}
		TypeDefinition icommandEventHelper = typeContext.ThisIsSlowFindType("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Input", "ICommandEventHelper");
		if (icommandEventHelper == null)
		{
			ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper");
		}
		_icommandEventHelperAddDelegateConverter = icommandEventHelper.Methods.Single((MethodDefinition m) => m.Name == "GetGenericEventHandlerForNonGenericEventHandlerForSubscribing");
		if (_icommandEventHelperAddDelegateConverter == null)
		{
			ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.GetGenericEventHandlerForNonGenericEventHandlerForSubscribing");
		}
		_icommandEventHelperRemoveDelegateConverter = icommandEventHelper.Methods.Single((MethodDefinition m) => m.Name == "GetGenericEventHandlerForNonGenericEventHandlerForUnsubscribing");
		if (_icommandEventHelperRemoveDelegateConverter == null)
		{
			ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.GetGenericEventHandlerForNonGenericEventHandlerForUnsubscribing");
		}
		TypeDefinition windowsRuntimeMarshal = typeContext.SystemAssembly.ThisIsSlowFindType("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeMarshal")?.Resolve();
		if (windowsRuntimeMarshal == null)
		{
			ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal");
		}
		_windowsRuntimeMarshalAddEventHandler = windowsRuntimeMarshal.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "AddEventHandler");
		if (_windowsRuntimeMarshalAddEventHandler == null)
		{
			ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler");
		}
		_windowsRuntimeMarshalRemoveEventHandler = windowsRuntimeMarshal.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "RemoveEventHandler");
		if (_windowsRuntimeMarshalRemoveEventHandler == null)
		{
			ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler");
		}
	}

	private void ThrowCannotImplementMethodException(string missingThing)
	{
		throw new InvalidProgramException("Unable to implement System.Windows.Input.ICommand projection to Windows.UI.Xaml.Input.ICommand because " + missingThing + " was stripped. This indicates a bug in UnityLinker.");
	}

	public void WriteAddCanExecuteChanged(MethodDefinition method)
	{
		IDataModelService factory = _context.Global.Services.TypeFactory;
		GenericInstanceType systemFunc2Instance = factory.CreateGenericInstanceTypeFromDefinition(_systemFunc2, _systemEventHandler1Instance, _eventRegistrationToken);
		MethodReference systemFunc2CtorInstance = _context.Global.Services.TypeFactory.ResolverFor(systemFunc2Instance).Resolve(_systemFunc2Ctor);
		GenericInstanceType systemAction1Instance = factory.CreateGenericInstanceTypeFromDefinition(_systemAction1, _eventRegistrationToken);
		MethodReference systemAction1CtorInstance = _context.Global.Services.TypeFactory.ResolverFor(systemAction1Instance).Resolve(_systemAction1Ctor);
		MethodDefinition winrtAddCanExecuteChanged = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "add_CanExecuteChanged");
		MethodDefinition winrtRemoveCanExecuteChanged = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "remove_CanExecuteChanged");
		GenericInstanceMethod windowsRuntimeMarshalAddEventHandlerInstance = factory.CreateGenericInstanceMethodFromDefinition(_windowsRuntimeMarshalAddEventHandler, _systemEventHandler1Instance);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Dup);
		iLProcessor.Emit(OpCodes.Ldvirtftn, winrtAddCanExecuteChanged);
		iLProcessor.Emit(OpCodes.Newobj, systemFunc2CtorInstance);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Dup);
		iLProcessor.Emit(OpCodes.Ldvirtftn, winrtRemoveCanExecuteChanged);
		iLProcessor.Emit(OpCodes.Newobj, systemAction1CtorInstance);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Call, _icommandEventHelperAddDelegateConverter);
		iLProcessor.Emit(OpCodes.Call, windowsRuntimeMarshalAddEventHandlerInstance);
		iLProcessor.Emit(OpCodes.Ret);
	}

	public void WriteRemoveCanExecuteChanged(MethodDefinition method)
	{
		IDataModelService factory = _context.Global.Services.TypeFactory;
		GenericInstanceType systemAction1Instance = factory.CreateGenericInstanceTypeFromDefinition(_systemAction1, _eventRegistrationToken);
		MethodReference systemAction1CtorInstance = _context.Global.Services.TypeFactory.ResolverFor(systemAction1Instance).Resolve(_systemAction1Ctor);
		MethodDefinition winrtRemoveCanExecuteChanged = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "remove_CanExecuteChanged");
		GenericInstanceMethod windowsRuntimeMarshalRemoveEventHandlerInstance = factory.CreateGenericInstanceMethodFromDefinition(_windowsRuntimeMarshalRemoveEventHandler, _systemEventHandler1Instance);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Dup);
		iLProcessor.Emit(OpCodes.Ldvirtftn, winrtRemoveCanExecuteChanged);
		iLProcessor.Emit(OpCodes.Newobj, systemAction1CtorInstance);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Call, _icommandEventHelperRemoveDelegateConverter);
		iLProcessor.Emit(OpCodes.Call, windowsRuntimeMarshalRemoveEventHandlerInstance);
		iLProcessor.Emit(OpCodes.Ret);
	}

	public void WriteCanExecute(MethodDefinition method)
	{
		ForwardToMethodWithOneArg(method, "CanExecute");
	}

	public void WriteExecute(MethodDefinition method)
	{
		ForwardToMethodWithOneArg(method, "Execute");
	}

	private void ForwardToMethodWithOneArg(MethodDefinition method, string methodName)
	{
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Callvirt, _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == methodName));
		iLProcessor.Emit(OpCodes.Ret);
	}
}
