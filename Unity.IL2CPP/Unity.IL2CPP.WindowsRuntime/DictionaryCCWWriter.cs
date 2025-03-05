using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class DictionaryCCWWriter : IProjectedComCallableWrapperMethodWriter
{
	private class GetViewMethodBodyWriter : ProjectedMethodBodyWriter
	{
		public GetViewMethodBodyWriter(ReadOnlyContext context, MethodReference getViewMethod)
			: base(context, getViewMethod, getViewMethod)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference iReadOnlyDictionary = _typeResolver.Resolve(InteropMethod.ReturnType);
			TypeDefinition readOnlyDictionary = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.ReadOnlyDictionary);
			TypeReference readOnlyDictionaryInstance = _typeResolver.Resolve(readOnlyDictionary);
			MethodReference readOnlyDictionaryCtor = _typeResolver.Resolve(readOnlyDictionary.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Resolve().FullName == "System.Collections.Generic.IDictionary`2"));
			writer.AddIncludeForTypeDefinition(writer.Context, readOnlyDictionaryInstance);
			string thisLocalVariableName = "__thisValue";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{InteropMethod.DeclaringType.CppNameForVariable} {thisLocalVariableName} = {ManagedObjectExpression};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IsInst({thisLocalVariableName}, {metadataAccess.TypeInfoFor(iReadOnlyDictionary)}))");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = {thisLocalVariableName};");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{readOnlyDictionaryInstance.CppNameForVariable} readOnlyDictionary = {Emit.NewObj(writer.Context, _typeResolver.Resolve(readOnlyDictionaryInstance), metadataAccess)};");
				WriteMethodCallStatement(metadataAccess, "readOnlyDictionary", readOnlyDictionaryCtor, MethodCallType.Normal, writer, thisLocalVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = readOnlyDictionary;");
			}
		}
	}

	private class InsertMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodReference _setItemMethod;

		private readonly MethodReference _containsKeyMethod;

		public InsertMethodBodyWriter(ReadOnlyContext context, MethodReference setItemMethod, MethodReference containsKeyMethod, MethodReference insertMethod)
			: base(context, insertMethod, insertMethod)
		{
			_setItemMethod = setItemMethod;
			_containsKeyMethod = containsKeyMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _containsKeyMethod, MethodCallType.Virtual, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue(), localVariableNames[0]);
			WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, _setItemMethod, MethodCallType.Virtual, writer, localVariableNames[0], localVariableNames[1]);
		}
	}

	private class LookupMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodReference _tryGetValueMethod;

		public LookupMethodBodyWriter(ReadOnlyContext context, MethodReference tryGetValueMethod, MethodReference lookupMethod)
			: base(context, lookupMethod, lookupMethod)
		{
			_tryGetValueMethod = tryGetValueMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("bool keyFound;");
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _tryGetValueMethod, MethodCallType.Virtual, writer, "keyFound", localVariableNames[0], Emit.AddressOf(writer.Context.Global.Services.Naming.ForInteropReturnValue()));
			writer.WriteLine();
			writer.WriteLine("if (!keyFound)");
			using (new BlockWriter(writer))
			{
				WriteThrowKeyNotFoundExceptionWithEBoundsHResult(writer, _managedMethod, metadataAccess);
			}
		}
	}

	private class RemoveMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodReference _removeMethod;

		public RemoveMethodBodyWriter(ReadOnlyContext context, MethodReference removeMethod, MethodReference method)
			: base(context, method, method)
		{
			_removeMethod = removeMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("bool removed;");
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _removeMethod, MethodCallType.Virtual, writer, "removed", localVariableNames[0]);
			writer.WriteLine();
			writer.WriteLine("if (!removed)");
			using (new BlockWriter(writer))
			{
				WriteThrowKeyNotFoundExceptionWithEBoundsHResult(writer, _managedMethod, metadataAccess);
			}
		}
	}

	private class SplitMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly DictionaryCCWWriter _parent;

		private readonly TypeResolver _iDictionaryTypeResolver;

		public SplitMethodBodyWriter(ReadOnlyContext context, DictionaryCCWWriter parent, TypeResolver iDictionaryTypeResolver, MethodReference method)
			: base(context, method, method)
		{
			_parent = parent;
			_iDictionaryTypeResolver = iDictionaryTypeResolver;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			if (writer.Context.Global.Services.TypeProvider.ConstantSplittableMapType == null)
			{
				string exceptionMessage = "Cannot call method '" + InteropMethod.FullName + "' from native code. It requires type System.Runtime.InteropServices.WindowsRuntime.ConstantSplittableMap`2<K, V> to be present. Was it incorrectly stripped?";
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_not_supported_exception(\"" + exceptionMessage + "\")"));
				return;
			}
			if (!writer.Context.Global.Services.TypeProvider.ConstantSplittableMapType.IsSealed)
			{
				throw new InvalidProgramException("System.Runtime.InteropServices.WindowsRuntime.ConstantSplittableMap`2 was not sealed. Was System.Runtime.WindowsRuntime.dll modified unexpectedly?");
			}
			TypeReference constantSplittableMapInstance = _iDictionaryTypeResolver.Resolve(writer.Context.Global.Services.TypeProvider.ConstantSplittableMapType);
			TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(constantSplittableMapInstance);
			MethodDefinition constantSplittableMapCtorDef = writer.Context.Global.Services.TypeProvider.ConstantSplittableMapType.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1);
			MethodReference constantSplittableMapCtorInstance = typeResolver.Resolve(constantSplittableMapCtorDef);
			MethodDefinition splitMethodDef = writer.Context.Global.Services.TypeProvider.ConstantSplittableMapType.Methods.Single((MethodDefinition m) => m.HasThis && m.Name == "Split");
			MethodReference splitMethodInstance = typeResolver.Resolve(splitMethodDef);
			MethodReference getCountMethod = _context.Global.Services.TypeFactory.ResolverFor(_iDictionaryTypeResolver.Resolve(_parent._iCollectionTypeRef)).Resolve(_parent._getCountMethodDef);
			writer.AddIncludeForTypeDefinition(writer.Context, constantSplittableMapInstance);
			writer.AddIncludeForMethodDeclaration(constantSplittableMapCtorInstance);
			writer.AddIncludeForMethodDeclaration(splitMethodInstance);
			writer.WriteLine("int32_t itemsInCollection;");
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, getCountMethod, MethodCallType.Virtual, writer, "itemsInCollection");
			writer.WriteLine("if (itemsInCollection > 1)");
			using (new BlockWriter(writer))
			{
				string constantSplittableMapVariableType = constantSplittableMapInstance.CppNameForVariable;
				string isInstCall = $"IsInstSealed({ManagedObjectExpression}, {metadataAccess.TypeInfoFor(constantSplittableMapInstance)})";
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{constantSplittableMapVariableType} splittableMap = {Emit.Cast(constantSplittableMapVariableType, isInstCall)};");
				writer.WriteLine();
				writer.WriteLine("if (splittableMap == NULL)");
				using (new BlockWriter(writer))
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"splittableMap = {Emit.NewObj(writer.Context, constantSplittableMapInstance, metadataAccess)};");
					WriteMethodCallStatement(metadataAccess, "splittableMap", constantSplittableMapCtorInstance, MethodCallType.Normal, writer, ManagedObjectExpression);
				}
				writer.WriteLine();
				WriteMethodCallStatement(metadataAccess, "splittableMap", splitMethodInstance, MethodCallType.Normal, writer, localVariableNames[0], localVariableNames[1]);
			}
		}
	}

	private readonly TypeReference _iCollectionTypeRef;

	private readonly MethodDefinition _clearMethodDef;

	private readonly MethodDefinition _containsKeyMethodDef;

	private readonly MethodDefinition _getCountMethodDef;

	private readonly MethodDefinition _removeMethodDef;

	private readonly MethodDefinition _setItemMethodDef;

	private readonly MethodDefinition _tryGetValueMethodDef;

	public DictionaryCCWWriter(TypeDefinition iDictionary)
	{
		_iCollectionTypeRef = iDictionary.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection`1")?.InterfaceType;
		if (_iCollectionTypeRef == null)
		{
			_iCollectionTypeRef = iDictionary.Interfaces.Single((InterfaceImplementation t) => t.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType;
		}
		TypeDefinition iCollection = _iCollectionTypeRef.Resolve();
		_clearMethodDef = iCollection.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
		_containsKeyMethodDef = iDictionary.Methods.Single((MethodDefinition m) => m.Name == "ContainsKey");
		_getCountMethodDef = iCollection.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
		_removeMethodDef = iDictionary.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Remove");
		_setItemMethodDef = iDictionary.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "set_Item");
		_tryGetValueMethodDef = iDictionary.Methods.Single((MethodDefinition m) => m.Name == "TryGetValue");
	}

	public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
	}

	public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
	{
		TypeReference iMap = method.DeclaringType;
		TypeResolver iDictionaryTypeResolver = context.Global.Services.TypeFactory.ResolverFor(context.Global.Services.WindowsRuntime.ProjectToCLR(iMap));
		TypeResolver iCollectionTypeResolver = context.Global.Services.TypeFactory.ResolverFor(iDictionaryTypeResolver.Resolve(_iCollectionTypeRef));
		switch (method.Name)
		{
		case "Clear":
		{
			MethodReference clearMethod = iCollectionTypeResolver.Resolve(_clearMethodDef);
			return new ProjectedMethodBodyWriter(context, clearMethod, method);
		}
		case "get_Size":
		{
			MethodReference getCountMethod = iCollectionTypeResolver.Resolve(_getCountMethodDef);
			return new ProjectedMethodBodyWriter(context, getCountMethod, method);
		}
		case "GetView":
			return new GetViewMethodBodyWriter(context, method);
		case "HasKey":
		{
			MethodReference containsKeyMethod = iDictionaryTypeResolver.Resolve(_containsKeyMethodDef);
			return new ProjectedMethodBodyWriter(context, containsKeyMethod, method);
		}
		case "Insert":
		{
			MethodReference setItemMethod = iDictionaryTypeResolver.Resolve(_setItemMethodDef);
			return new InsertMethodBodyWriter(context, setItemMethod, iDictionaryTypeResolver.Resolve(_containsKeyMethodDef), method);
		}
		case "Lookup":
		{
			MethodReference tryGetValueMethod = iDictionaryTypeResolver.Resolve(_tryGetValueMethodDef);
			return new LookupMethodBodyWriter(context, tryGetValueMethod, method);
		}
		case "Remove":
		{
			MethodReference removeMethod = iDictionaryTypeResolver.Resolve(_removeMethodDef);
			return new RemoveMethodBodyWriter(context, removeMethod, method);
		}
		case "Split":
			return new SplitMethodBodyWriter(context, this, iDictionaryTypeResolver, method);
		default:
			throw new NotSupportedException("DictionaryCCWWriter does not support writing method body for " + method.FullName + ".");
		}
	}

	private static void WriteThrowKeyNotFoundExceptionWithEBoundsHResult(IGeneratedMethodCodeWriter writer, MethodReference managedMethod, IRuntimeMetadataAccess metadataAccess)
	{
		TypeDefinition keyNotFoundException = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.KeyNotFoundException);
		MethodDefinition keyNotFoundExceptionConstructor = keyNotFoundException.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		string keyNotFoundExceptionVariableType = keyNotFoundException.CppNameForVariable;
		PropertyDefinition hresultProperty = writer.Context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
		string exceptionMessage = metadataAccess.StringLiteral("The given key was not present in the dictionary.", writer.Context.Global.Services.TypeProvider.Corlib);
		writer.AddIncludeForTypeDefinition(writer.Context, keyNotFoundException);
		writer.AddIncludeForMethodDeclaration(keyNotFoundExceptionConstructor);
		writer.AddIncludeForMethodDeclaration(hresultProperty.SetMethod);
		writer.WriteLine($"{keyNotFoundExceptionVariableType} e = {Emit.NewObj(writer.Context, keyNotFoundException, metadataAccess)};");
		writer.WriteMethodCallStatement(metadataAccess, "e", managedMethod, keyNotFoundExceptionConstructor, MethodCallType.Normal, exceptionMessage);
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("E_BOUNDS");
		}
		writer.WriteMethodCallStatement(metadataAccess, "e", managedMethod, hresultProperty.SetMethod, MethodCallType.Normal, (-2147483637).ToString());
		writer.WriteStatement(Emit.RaiseManagedException("e"));
	}
}
