using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

internal sealed class IEnumerableMethodBodyWriter
{
	private readonly TypeDefinition _iteratorToEnumeratorAdapter;

	private readonly TypeDefinition _iiterableType;

	private readonly EditContext _typeEditContext;

	private readonly ReadOnlyContext _context;

	public IEnumerableMethodBodyWriter(ReadOnlyContext context, EditContext typeEditContext, TypeDefinition iteratorToEnumeratorAdapter, TypeDefinition iiterableType)
	{
		_typeEditContext = typeEditContext;
		_iteratorToEnumeratorAdapter = iteratorToEnumeratorAdapter;
		_iiterableType = iiterableType;
		_context = context;
	}

	public void WriteGetEnumerator(MethodDefinition method)
	{
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		IDataModelService typeFactory = _context.Global.Services.TypeFactory;
		TypeReference ienumerableType = method.Overrides.First().DeclaringType;
		TypeResolver typeResolver;
		if (_iiterableType.HasGenericParameters && !ienumerableType.Resolve().HasGenericParameters)
		{
			GenericInstanceType iiterableInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iiterableType, method.DeclaringType.GenericParameters[0]);
			typeResolver = typeFactory.ResolverFor(iiterableInstance);
		}
		else
		{
			typeResolver = typeFactory.ResolverFor(ienumerableType);
		}
		MethodReference firstMethod = typeResolver.Resolve(_iiterableType.Methods.First((MethodDefinition m) => m.Name == "First"));
		MethodReference adapterConstructor = typeResolver.Resolve(_iteratorToEnumeratorAdapter.Methods.First((MethodDefinition m) => m.IsConstructor));
		_typeEditContext.AddVariableToMethod(method, typeResolver.Resolve(firstMethod.ReturnType));
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Callvirt, firstMethod);
		ilProcessor.Emit(OpCodes.Dup);
		ilProcessor.Emit(OpCodes.Stloc_0);
		Instruction ldLoc0Instruction = Instruction.Create(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Brtrue, ldLoc0Instruction);
		ilProcessor.Emit(OpCodes.Ldnull);
		ilProcessor.Emit(OpCodes.Ret);
		ilProcessor.Append(ldLoc0Instruction);
		ilProcessor.Emit(OpCodes.Newobj, adapterConstructor);
		ilProcessor.Emit(OpCodes.Ret);
	}
}
