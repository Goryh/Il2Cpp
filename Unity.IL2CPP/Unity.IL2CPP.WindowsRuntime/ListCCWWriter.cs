using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class ListCCWWriter : IProjectedComCallableWrapperMethodWriter
{
	private class ExceptionWithEBoundsHResultMethodBodyWriter : ProjectedMethodBodyWriter
	{
		public ExceptionWithEBoundsHResultMethodBodyWriter(ReadOnlyContext context, MethodReference getItemMethod, MethodReference method)
			: base(context, getItemMethod, method)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				if (_managedMethod.ReturnType.MetadataType != MetadataType.Void)
				{
					WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
				}
				else
				{
					WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer);
				}
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
			using (new BlockWriter(writer))
			{
				TypeDefinition argumentOutOfRangeException = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentOutOfRangeException);
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"if (IsInst(ex.ex, {metadataAccess.TypeInfoFor(argumentOutOfRangeException)}))");
				using (new BlockWriter(writer))
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"ex.ex->hresult = {-2147483637}; // E_BOUNDS");
				}
				writer.WriteLine();
				writer.WriteLine("throw;");
			}
		}
	}

	private class GetManyMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly TypeReference _itemType;

		private readonly MethodReference _getCountMethod;

		private readonly MethodReference _getItemMethod;

		private readonly TypeDefinition _argumentOutOfRangeException;

		private readonly MethodDefinition _argumentOutOfRangeExceptionConstructor;

		protected override bool AreParametersMarshaled { get; }

		public GetManyMethodBodyWriter(ReadOnlyContext context, MethodReference getCountMethod, MethodReference getItemMethod, MethodReference method)
			: base(context, method, method)
		{
			_itemType = ((GenericInstanceType)getCountMethod.DeclaringType).GenericArguments[0];
			_getCountMethod = getCountMethod;
			_getItemMethod = getItemMethod;
			_argumentOutOfRangeException = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentOutOfRangeException);
			_argumentOutOfRangeExceptionConstructor = _argumentOutOfRangeException.Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string startIndexVariableName = MarshaledParameterTypes[0].VariableName;
			string itemsLengthVariableName = MarshaledParameterTypes[1].VariableName;
			string itemsVariableName = MarshaledParameterTypes[2].VariableName;
			DefaultMarshalInfoWriter itemMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _itemType, MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
			string elementsInCollectionVariableName = "elementsInCollection";
			string signedElementsInCollectionVariableName = "signedElementsInCollection";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"uint32_t {elementsInCollectionVariableName};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"int32_t {signedElementsInCollectionVariableName};");
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getCountMethod, MethodCallType.Virtual, writer, signedElementsInCollectionVariableName);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteStatement($"{elementsInCollectionVariableName} = {signedElementsInCollectionVariableName}");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({startIndexVariableName} != {elementsInCollectionVariableName} && {itemsVariableName} != {"NULL"})");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"if ({startIndexVariableName} > {elementsInCollectionVariableName} || {startIndexVariableName} > {int.MaxValue})");
				using (new BlockWriter(writer))
				{
					WriteRaiseManagedExceptionWithCustomHResult(writer, _argumentOutOfRangeExceptionConstructor, -2147483637, "E_BOUNDS", metadataAccess, metadataAccess.StringLiteral("index"));
				}
				writer.AddStdInclude("algorithm");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = std::min({itemsLengthVariableName}, {elementsInCollectionVariableName} - {startIndexVariableName});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"for (uint32_t i = 0; i < {writer.Context.Global.Services.Naming.ForInteropReturnValue()}; i++)");
				using (new BlockWriter(writer))
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{_itemType.CppNameForVariable} itemManaged;");
					WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getItemMethod, MethodCallType.Virtual, writer, "itemManaged", "i + " + startIndexVariableName);
					itemMarshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue("itemManaged"), itemsVariableName + "[i]", null, metadataAccess);
				}
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = 0;");
			}
		}
	}

	private class GetViewMethodBodyWriter : ProjectedMethodBodyWriter
	{
		public GetViewMethodBodyWriter(ReadOnlyContext context, MethodReference method)
			: base(context, method, method)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference iReadOnlyList = _typeResolver.Resolve(InteropMethod.ReturnType);
			TypeDefinition readOnlyCollection = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.ReadOnlyCollection);
			TypeReference readOnlyCollectionInstance = _typeResolver.Resolve(readOnlyCollection);
			MethodReference readOnlyCollectionCtor = _typeResolver.Resolve(readOnlyCollection.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Resolve().FullName == "System.Collections.Generic.IList`1"));
			writer.AddIncludeForTypeDefinition(writer.Context, readOnlyCollectionInstance);
			string thisLocalVariableName = "__thisValue";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{InteropMethod.DeclaringType.CppNameForVariable} {thisLocalVariableName} = {ManagedObjectExpression};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IsInst({thisLocalVariableName}, {metadataAccess.TypeInfoFor(iReadOnlyList)}))");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = {thisLocalVariableName};");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{readOnlyCollectionInstance.CppNameForVariable} readOnlyCollection = {Emit.NewObj(writer.Context, _typeResolver.Resolve(readOnlyCollectionInstance), metadataAccess)};");
				WriteMethodCallStatement(metadataAccess, "readOnlyCollection", readOnlyCollectionCtor, MethodCallType.Normal, writer, thisLocalVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = readOnlyCollection;");
			}
		}
	}

	private class NonGenericGetViewMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly TypeDefinition _adapterType;

		private readonly MethodDefinition _adapterCtor;

		public NonGenericGetViewMethodBodyWriter(ReadOnlyContext context, MethodReference method)
			: base(context, method, method)
		{
			_adapterType = context.Global.Services.TypeProvider.GetSystemType(SystemType.ListToBindableVectorViewAdapter);
			if (_adapterType == null)
			{
				throw new InvalidOperationException("Failed to resolve ListToBindableVectorViewAdapter, which is required for generating marshaling code for IBindableVector.GetView(). Is linker broken?");
			}
			_adapterCtor = _adapterType.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
			if (_adapterCtor == null)
			{
				throw new InvalidOperationException("Failed to find ListToBindableVectorViewAdapter constructor, which is required for generating marshaling code for IBindableVector.GetView(). Is linker broken?");
			}
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string adapterVariable = "viewAdapter";
			writer.AddIncludeForTypeDefinition(writer.Context, _adapterType);
			writer.AddIncludeForMethodDeclaration(_adapterCtor);
			writer.WriteStatement(Emit.Assign(_adapterType.CppNameForVariable + " " + adapterVariable, Emit.NewObj(writer.Context, _adapterType, metadataAccess)));
			WriteMethodCallStatement(metadataAccess, adapterVariable, _adapterCtor, MethodCallType.Normal, writer, ManagedObjectExpression);
			writer.WriteStatement(Emit.Assign(writer.Context.Global.Services.Naming.ForInteropReturnValue(), adapterVariable));
		}
	}

	private class IndexOfMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly TypeReference _itemType;

		private readonly MethodReference _getCountMethod;

		private readonly MethodReference _getItemMethod;

		public IndexOfMethodBodyWriter(ReadOnlyContext context, MethodReference getCountMethod, MethodReference getItemMethod, MethodReference method)
			: base(context, method, method)
		{
			GenericInstanceType declaringGenericInstanceType = getCountMethod.DeclaringType as GenericInstanceType;
			_itemType = ((declaringGenericInstanceType != null) ? declaringGenericInstanceType.GenericArguments[0] : context.Global.Services.TypeProvider.ObjectTypeReference);
			_getCountMethod = getCountMethod;
			_getItemMethod = getItemMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string valueToLookForVariable = localVariableNames[0];
			string outIndexVariable = localVariableNames[1];
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{Emit.Dereference(outIndexVariable)} = 0;");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = false;");
			writer.WriteLine();
			string elementsInCollectionVariableName = "elementsInCollection";
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"int {elementsInCollectionVariableName};");
			WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getCountMethod, MethodCallType.Virtual, writer, elementsInCollectionVariableName);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"for (int i = 0; i < {elementsInCollectionVariableName}; i++)");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{_itemType.CppNameForVariable} item;");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getItemMethod, MethodCallType.Virtual, writer, "item", "i");
				string compareResultVariableName = "compareResult";
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"bool {compareResultVariableName};");
				WriteComparisonExpression(writer, "item", valueToLookForVariable, metadataAccess, compareResultVariableName);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"if ({compareResultVariableName})");
				using (new BlockWriter(writer))
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{Emit.Dereference(outIndexVariable)} = static_cast<uint32_t>(i);");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForInteropReturnValue()} = true;");
					writer.WriteLine("break;");
				}
			}
		}

		private void WriteComparisonExpression(IGeneratedMethodCodeWriter writer, string value1, string value2, IRuntimeMetadataAccess metadataAccess, string resultVariable)
		{
			switch (_itemType.MetadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Single:
			case MetadataType.Double:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				writer.WriteStatement($"{resultVariable} = {value1} == {value2}");
				return;
			case MetadataType.String:
			{
				MethodDefinition stringEquals = writer.Context.Global.Services.TypeProvider.SystemString.Methods.Single((MethodDefinition m) => !m.HasThis && m.Name == "Equals" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
				writer.AddIncludeForMethodDeclaration(stringEquals);
				WriteMethodCallStatementWithResult(metadataAccess, "NULL", stringEquals, MethodCallType.Normal, writer, resultVariable, value1, value2);
				return;
			}
			}
			MethodDefinition objectEquals = writer.Context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.HasThis && m.IsVirtual && m.Name == "Equals" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.Object);
			if (_itemType.IsValueType)
			{
				VTableMultipleGenericInterfaceImpls multipleGenericInterfaceImpls;
				MethodReference targetMethod = writer.Context.Global.Services.VTable.GetVirtualMethodTargetMethodForConstrainedCallOnValueType(writer.Context, _itemType, objectEquals, out multipleGenericInterfaceImpls);
				if (targetMethod != null && targetMethod.DeclaringType == _itemType)
				{
					writer.AddIncludeForMethodDeclaration(targetMethod);
					WriteMethodCallStatementWithResult(metadataAccess, Emit.AddressOf(value1), targetMethod, MethodCallType.Normal, writer, resultVariable, Emit.Box(writer.Context, _itemType, value2, metadataAccess));
					return;
				}
				value1 = Emit.Box(writer.Context, _itemType, value1, metadataAccess);
				value2 = Emit.Box(writer.Context, _itemType, value2, metadataAccess);
			}
			WriteMethodCallStatementWithResult(metadataAccess, value1, objectEquals, MethodCallType.Virtual, writer, resultVariable, value2);
		}
	}

	private class RemoveAtEndMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodReference _getCountMethod;

		private readonly MethodReference _removeAtMethod;

		public RemoveAtEndMethodBodyWriter(ReadOnlyContext context, MethodReference getCountMethod, MethodReference removeAtMethod, MethodReference method)
			: base(context, method, method)
		{
			_getCountMethod = getCountMethod;
			_removeAtMethod = removeAtMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string thisType = _managedMethod.DeclaringType.CppNameForVariable;
			string thisLocalVariableName = "__thisValue";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{thisType} {thisLocalVariableName} = ({thisType}){ManagedObjectExpression};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_getCountMethod.ReturnType.CppNameForVariable} itemsInCollection;");
			WriteMethodCallStatementWithResult(metadataAccess, thisLocalVariableName, _getCountMethod, MethodCallType.Virtual, writer, "itemsInCollection");
			writer.WriteLine("if (itemsInCollection == 0)");
			using (new BlockWriter(writer))
			{
				MethodDefinition invalidOperationExceptionConstructor = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.InvalidOperationException).Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
				string message = "Cannot remove the last element from an empty collection.";
				WriteRaiseManagedExceptionWithCustomHResult(writer, invalidOperationExceptionConstructor, -2147483637, "E_BOUNDS", metadataAccess, metadataAccess.StringLiteral(message));
			}
			WriteMethodCallStatement(metadataAccess, thisLocalVariableName, _removeAtMethod, MethodCallType.Virtual, writer, "itemsInCollection - 1");
		}
	}

	private class ReplaceAllMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodReference _clearMethod;

		private readonly MethodReference _addMethod;

		public ReplaceAllMethodBodyWriter(ReadOnlyContext context, MethodReference clearMethod, MethodReference addMethod, MethodReference replaceAllMethod)
			: base(context, replaceAllMethod, replaceAllMethod)
		{
			_clearMethod = clearMethod;
			_addMethod = addMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string thisType = _managedMethod.DeclaringType.CppNameForVariable;
			string thisLocalVariableName = "__thisValue";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{thisType} {thisLocalVariableName} = ({thisType}){ManagedObjectExpression};");
			WriteMethodCallStatement(metadataAccess, thisLocalVariableName, _clearMethod, MethodCallType.Virtual, writer);
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ({localVariableNames[0]} != {"NULL"})");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{"il2cpp_array_size_t"} itemsInCollection = {localVariableNames[0]}->max_length;");
				writer.WriteLine("for (il2cpp_array_size_t i = 0; i < itemsInCollection; i++)");
				using (new BlockWriter(writer))
				{
					string itemExpression = Emit.LoadArrayElement(localVariableNames[0], "i", useArrayBoundsCheck: false);
					WriteMethodCallStatement(metadataAccess, thisLocalVariableName, _addMethod, MethodCallType.Virtual, writer, itemExpression);
				}
			}
		}
	}

	private readonly TypeDefinition _iListTypeDef;

	private readonly TypeDefinition _iCollectionTypeDef;

	private readonly TypeReference _iCollectionTypeRef;

	private readonly MethodDefinition _addMethodDef;

	private readonly MethodDefinition _clearMethodDef;

	private readonly MethodDefinition _getCountMethodDef;

	private readonly MethodDefinition _getItemMethodDef;

	private readonly MethodDefinition _insertMethodDef;

	private readonly MethodDefinition _removeAtMethodDef;

	private readonly MethodDefinition _setItemMethodDef;

	public ListCCWWriter(TypeDefinition iList)
	{
		bool iCollectionIsGeneric = true;
		_iListTypeDef = iList;
		_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "IReadOnlyCollection`1")?.InterfaceType;
		if (_iCollectionTypeRef == null)
		{
			_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection`1")?.InterfaceType;
			if (_iCollectionTypeRef == null)
			{
				_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection").InterfaceType;
				iCollectionIsGeneric = false;
			}
		}
		_iCollectionTypeDef = _iCollectionTypeRef.Resolve();
		if (iCollectionIsGeneric)
		{
			_addMethodDef = _iCollectionTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Add");
			_clearMethodDef = _iCollectionTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
		}
		else
		{
			_addMethodDef = iList.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Add");
			_clearMethodDef = iList.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
		}
		_getCountMethodDef = _iCollectionTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
		_getItemMethodDef = _iListTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Item");
		_insertMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Insert");
		_removeAtMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "RemoveAt");
		_setItemMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "set_Item");
	}

	public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
	}

	public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
	{
		TypeReference iVectorViewType = method.DeclaringType;
		TypeResolver iListTypeResolver = context.Global.Services.TypeFactory.ResolverFor(context.Global.Services.WindowsRuntime.ProjectToCLR(iVectorViewType));
		TypeResolver iCollectionTypeResolver = context.Global.Services.TypeFactory.ResolverFor(iListTypeResolver.Resolve(_iCollectionTypeRef));
		MethodReference getCountMethod = iCollectionTypeResolver.Resolve(_getCountMethodDef);
		MethodReference getItemMethod = iListTypeResolver.Resolve(_getItemMethodDef);
		switch (method.Name)
		{
		case "Append":
			return new ProjectedMethodBodyWriter(context, iCollectionTypeResolver.Resolve(_addMethodDef), method);
		case "Clear":
			return new ProjectedMethodBodyWriter(context, iCollectionTypeResolver.Resolve(_clearMethodDef), method);
		case "get_Size":
			return new ProjectedMethodBodyWriter(context, getCountMethod, method);
		case "GetAt":
			return new ExceptionWithEBoundsHResultMethodBodyWriter(context, getItemMethod, method);
		case "GetMany":
			return new GetManyMethodBodyWriter(context, getCountMethod, getItemMethod, method);
		case "GetView":
			if (!_iListTypeDef.HasGenericParameters)
			{
				return new NonGenericGetViewMethodBodyWriter(context, method);
			}
			return new GetViewMethodBodyWriter(context, method);
		case "IndexOf":
			return new IndexOfMethodBodyWriter(context, getCountMethod, getItemMethod, method);
		case "InsertAt":
		{
			MethodReference insertMethod = iListTypeResolver.Resolve(_insertMethodDef);
			return new ExceptionWithEBoundsHResultMethodBodyWriter(context, insertMethod, method);
		}
		case "RemoveAt":
			return new ExceptionWithEBoundsHResultMethodBodyWriter(context, iListTypeResolver.Resolve(_removeAtMethodDef), method);
		case "RemoveAtEnd":
			return new RemoveAtEndMethodBodyWriter(context, getCountMethod, iListTypeResolver.Resolve(_removeAtMethodDef), method);
		case "ReplaceAll":
			return new ReplaceAllMethodBodyWriter(context, iCollectionTypeResolver.Resolve(_clearMethodDef), iCollectionTypeResolver.Resolve(_addMethodDef), method);
		case "SetAt":
		{
			MethodReference setItemMethod = iListTypeResolver.Resolve(_setItemMethodDef);
			return new ExceptionWithEBoundsHResultMethodBodyWriter(context, setItemMethod, method);
		}
		default:
			throw new NotSupportedException("ListCCWWriter does not support writing method body for " + method.FullName + ".");
		}
	}
}
