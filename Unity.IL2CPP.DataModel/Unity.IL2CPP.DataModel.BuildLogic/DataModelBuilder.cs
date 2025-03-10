using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;
using Unity.IL2CPP.DataModel.BuildLogic.Inflation;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Builders;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public class DataModelBuilder : IDisposable
{
	private readonly TypeContext _context;

	private readonly LoadSettings _settings;

	private readonly TinyProfiler2 _tinyProfiler;

	private LoadedAssemblyContext _cecilContext;

	private bool _disposed;

	public TypeContext Context => _context;

	internal ReadOnlyCollection<Mono.Cecil.AssemblyDefinition> CecilAssemblies => _cecilContext.Assemblies;

	public DataModelBuilder(TinyProfiler2 tinyProfiler, LoadSettings settings)
	{
		_context = new TypeContext(settings.Parameters);
		_settings = settings;
		_tinyProfiler = tinyProfiler;
	}

	public void Build()
	{
		if (_settings.AssemblySettings.Count == 0)
		{
			throw new ArgumentException("No assemblies specified");
		}
		UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> systemAssembly;
		ReadOnlyCollection<UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>> assemblies = Initialize(out systemAssembly);
		ReadOnlyCollection<CecilSourcedAssemblyData> assemblyCecilData;
		DefinitionModelBuilder definitionModelBuilder = BuildCecilSourcedData(assemblies, out assemblyCecilData);
		ThreadSafeMemberStore memberStore = new ThreadSafeMemberStore(new UnderConstructionMemberStore());
		UnderConstructionMemberRepositories repositories = new UnderConstructionMemberRepositories(_context, memberStore);
		ConstructionTimeTypeFactory typeFactory = new ConstructionTimeTypeFactory(_context, repositories);
		DefinitionBuildingStage2(definitionModelBuilder, assemblyCecilData, systemAssembly, typeFactory);
		TypeReferenceResolver typeReferenceResolver = new TypeReferenceResolver(_context, repositories.Types);
		MethodReferenceResolver methodReferenceResolver = new MethodReferenceResolver(_context, repositories.Methods);
		FieldReferenceResolver fieldReferenceResolver = new FieldReferenceResolver(_context, repositories.Fields);
		ResolveReferences(assemblyCecilData, methodReferenceResolver, fieldReferenceResolver, typeReferenceResolver);
		PopulateCecilSourcedDefinitions(assemblyCecilData);
		GenerateDefinitionsForComAndInteropSupport(typeFactory);
		Inflation(typeFactory, repositories);
		ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes = repositories.Types.CurrentItems();
		ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods = repositories.Methods.CurrentItems();
		ReadOnlyCollection<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> allNonDefinitionFields = repositories.Fields.CurrentItems();
		ReferencePopulationStage1(allNonDefinitionTypes, allNonDefinitionMethods);
		ReferencePopulationStage2(allNonDefinitionTypes, allNonDefinitionMethods, allNonDefinitionFields);
		PopulateGenerHiddenMethodUsage();
		ReadOnlyCollection<AssemblyDefinition> assembliesSortedForPublicExposure = GetAssembliesSortedForPublicExposure();
		_context.CompleteBuild(memberStore, assembliesSortedForPublicExposure);
	}

	private void ReferencePopulationStage2(ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods, ReadOnlyCollection<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> allNonDefinitionFields)
	{
		using (_tinyProfiler.Section("ReferencePopulater"))
		{
			using (_tinyProfiler.Section("Types"))
			{
				ReferencePopulater.PopulateStage2(allNonDefinitionTypes);
			}
			using (_tinyProfiler.Section("Methods"))
			{
				ReferencePopulater.PopulateStage2(allNonDefinitionMethods);
				ReferencePopulater.PopulateStage2(_context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition d) => d.AllMethods()));
			}
			using (_tinyProfiler.Section("Fields"))
			{
				ReferencePopulater.PopulateStage2(_context.AssembliesOrderedByCostToProcess.SelectMany((AssemblyDefinition d) => d.AllFields()));
				ReferencePopulater.PopulateStage2(allNonDefinitionFields);
			}
		}
	}

	private void ReferencePopulationStage1(ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		using (_tinyProfiler.Section("ReferencePopulater"))
		{
			using (_tinyProfiler.Section("Types"))
			{
				ReferencePopulater.Populate(_context, allNonDefinitionTypes);
			}
			using (_tinyProfiler.Section("Methods"))
			{
				ReferencePopulater.Populate(_context, allNonDefinitionMethods);
			}
		}
	}

	private void Inflation(ConstructionTimeTypeFactory typeFactory, UnderConstructionMemberRepositories repositories)
	{
		using (_tinyProfiler.Section("DefinitionInflater"))
		{
			new DefinitionInflater(_context, typeFactory).Process();
		}
	}

	private void PopulateCecilSourcedDefinitions(ReadOnlyCollection<CecilSourcedAssemblyData> assemblyData)
	{
		using (_tinyProfiler.Section("ResolveAssemblyTypeReferences"))
		{
			foreach (CecilSourcedAssemblyData asm in assemblyData)
			{
				DefinitionPopulater.PopulateAssembly(_context, asm);
			}
		}
		using (_tinyProfiler.Section("DefinitionPopulater"))
		{
			ParallelHelpers.ForEach(assemblyData.SelectMany((CecilSourcedAssemblyData a) => a.DefinitionTables.Types), delegate(UnderConstructionMember<TypeDefinition, Mono.Cecil.TypeDefinition> typeDef)
			{
				using (_tinyProfiler.Section(typeDef.Ours.Name))
				{
					DefinitionPopulater.PopulateTypeDef(_context, typeDef);
				}
			}, _settings.Parameters.EnableSerial);
		}
	}

	private void GenerateDefinitionsForComAndInteropSupport(ITypeFactory typeFactory)
	{
		using (_tinyProfiler.Section("COM and Interop Support"))
		{
			using (_tinyProfiler.Section("WindowsRuntimeProjectionsBuilder"))
			{
				_context.SetWindowsRuntimeProjects(new WindowsRuntimeProjectionsBuilder(_context).Build());
			}
			using (_tinyProfiler.Section("ComAndWindowsRuntimeSupport"))
			{
				ParallelHelpers.ForEach(_context.AssembliesOrderedByCostToProcess, delegate(AssemblyDefinition data)
				{
					using (_tinyProfiler.Section(data.Name.Name))
					{
						foreach (TypeDefinition current in data.GetAllTypes())
						{
							ComAndWindowsRuntimeSupport.ProcessType(_context.CreateEditContext(), current, typeFactory);
							MarshalInfoSupport.ProcessType(current);
						}
					}
				}, _settings.Parameters.EnableSerial);
			}
		}
	}

	private void ResolveReferences(ReadOnlyCollection<CecilSourcedAssemblyData> assemblyData, MethodReferenceResolver methodReferenceResolver, FieldReferenceResolver fieldReferenceResolver, TypeReferenceResolver typeReferenceResolver)
	{
		ParallelHelpers.ForEach(assemblyData, delegate(CecilSourcedAssemblyData data)
		{
			using (_tinyProfiler.Section("ResolveReferences", data.Assembly.Ours.Name.Name))
			{
				typeReferenceResolver.ProcessAssembly(data);
				methodReferenceResolver.ProcessAssembly(data);
				fieldReferenceResolver.ProcessAssembly(data);
			}
		}, _context.Parameters.EnableSerial);
	}

	private void DefinitionBuildingStage2(DefinitionModelBuilder definitionModelBuilder, ReadOnlyCollection<CecilSourcedAssemblyData> assemblyCecilData, UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> systemAssembly, ITypeFactory typeFactory)
	{
		GlobalDefinitionTables globalDefinitionTables;
		using (_tinyProfiler.Section("Build Global Definitions"))
		{
			globalDefinitionTables = GlobalDefinitionTables.Build(assemblyCecilData);
		}
		BuildSystemTypeArray(_context, systemAssembly.Source, globalDefinitionTables);
		BuildIl2CppTypesArray(_context, typeFactory);
		BuildGraftedArrayInterfaceArray();
		definitionModelBuilder.PopulateDefinitions(globalDefinitionTables);
		BuildSharedEnumTypes(_context);
		_context.SetTypeProvider();
		ValidateIl2CppExpectations(_context);
	}

	private DefinitionModelBuilder BuildCecilSourcedData(ReadOnlyCollection<UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>> assemblies, out ReadOnlyCollection<CecilSourcedAssemblyData> stage1Data)
	{
		DefinitionModelBuilder definitionModelBuilder = new DefinitionModelBuilder(_context);
		using (_tinyProfiler.Section("BuildCecilSourcedData"))
		{
			stage1Data = (from data in ParallelHelpers.Map(assemblies, delegate(UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> asm)
				{
					using (_tinyProfiler.Section(asm.Ours.Name.Name))
					{
						bool num = asm.Source != null;
						CecilSourcedAssemblyData cecilSourcedAssemblyData = null;
						if (num)
						{
							cecilSourcedAssemblyData = new CecilSourcedAssemblyData(_context, asm);
							if (!asm.Ours.LoadedForExportsOnly)
							{
								using (_tinyProfiler.Section("ImmediateRead"))
								{
									CecilAssemblyLoader.ImmediateRead(asm);
								}
							}
							using (_tinyProfiler.Section("BuildAssemblyDefinitionTable"))
							{
								definitionModelBuilder.BuildAssemblyDefinitionTable(cecilSourcedAssemblyData);
							}
							ReferenceUsages data2;
							using (_tinyProfiler.Section("ReferencesCollector"))
							{
								data2 = ReferencesCollector.Collect(asm.Source, asm.Ours.LoadedForExportsOnly);
							}
							cecilSourcedAssemblyData.SetData(data2);
						}
						return cecilSourcedAssemblyData;
					}
				}, _settings.Parameters.EnableSerial)
				where data != null
				select data).ToArray().AsReadOnly();
		}
		return definitionModelBuilder;
	}

	private ReadOnlyCollection<UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>> Initialize(out UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> systemAssembly)
	{
		using (_tinyProfiler.Section("LoadDeferred"))
		{
			_cecilContext = CecilAssemblyLoader.LoadDeferred(_tinyProfiler, _settings.AssemblySettings, _settings.Parameters);
		}
		Mono.Cecil.AssemblyDefinition cecilCoreLibrary = _cecilContext.Assemblies.First().MainModule.TypeSystem.Object.Resolve().Module.Assembly;
		UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>[] assemblies = _cecilContext.Assemblies.Select((Mono.Cecil.AssemblyDefinition asm) => new UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>(new AssemblyDefinition(asm, _context, _settings.AssemblySettings.SingleOrDefault((AssemblyLoadSettings s) => s.Path == asm.MainModule.FileName?.ToNPath())?.ExportsOnly ?? false), asm)).ToArray();
		systemAssembly = assemblies.First((UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> asm) => asm.Ours.Name.Name == cecilCoreLibrary.Name.Name);
		GeneratedTypesAssemblyDefinition generatedTypesAssembly = new GeneratedTypesAssemblyDefinition(_context, systemAssembly.Ours);
		assemblies = assemblies.Append(new UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition>(generatedTypesAssembly, null)).ToArray();
		_context.InitializeAssemblies(systemAssembly.Ours, generatedTypesAssembly, assemblies.Select((UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> pair) => pair.Ours).ToArray().AsReadOnly(), _cecilContext.WindowsRuntimeAssembliesLoaded);
		return assemblies.AsReadOnly();
	}

	private void ComputeCppNames(ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		using (_tinyProfiler.Section("Cpp Naming"))
		{
			CppNamePopulator.ComputeAllNames(_context, _tinyProfiler, allNonDefinitionTypes, allNonDefinitionMethods);
		}
	}

	private void BuildSystemTypeArray(TypeContext context, Mono.Cecil.AssemblyDefinition systemAssembly, GlobalDefinitionTables definitionTables)
	{
		SystemTypeReference[] systemTypeNames = SystemTypeName.GetSystemTypeNames();
		TypeDefinition[] systemTypes = new TypeDefinition[systemTypeNames.Length];
		for (int i = 0; i < systemTypeNames.Length; i++)
		{
			SystemTypeReference typeName = systemTypeNames[i];
			Mono.Cecil.TypeDefinition typeDefinition = null;
			if (typeName.IsInSystem)
			{
				typeDefinition = systemAssembly.MainModule.GetType(typeName.Namespace, typeName.Name);
			}
			else if (context.IsAssemblyLoaded(typeName.AssemblyNameReference.Name))
			{
				Mono.Cecil.TypeReference typeReference = new Mono.Cecil.TypeReference(typeName.Namespace, typeName.Name, systemAssembly.MainModule, typeName.AssemblyNameReference);
				try
				{
					typeDefinition = typeReference.Resolve();
				}
				catch (AssemblyResolutionException)
				{
					typeDefinition = null;
				}
				catch (InvalidOperationException)
				{
					typeDefinition = null;
				}
			}
			if (typeDefinition != null)
			{
				systemTypes[i] = definitionTables.GetDef(typeDefinition);
			}
		}
		_context.SetSystemTypes(systemTypes);
	}

	private void BuildSharedEnumTypes(TypeContext context)
	{
		if (context.Parameters.CanShareEnumTypes)
		{
			Dictionary<TypeReference, TypeReference> enumMap = new Dictionary<TypeReference, TypeReference>();
			enumMap.Add(context.GetSystemType(SystemType.SByte), context.GetSystemType(SystemType.SByteEnum));
			enumMap.Add(context.GetSystemType(SystemType.Int16), context.GetSystemType(SystemType.Int16Enum));
			enumMap.Add(context.GetSystemType(SystemType.Int32), context.GetSystemType(SystemType.Int32Enum));
			enumMap.Add(context.GetSystemType(SystemType.Int64), context.GetSystemType(SystemType.Int64Enum));
			enumMap.Add(context.GetSystemType(SystemType.Byte), context.GetSystemType(SystemType.ByteEnum));
			enumMap.Add(context.GetSystemType(SystemType.Char), context.GetSystemType(SystemType.UInt16Enum));
			enumMap.Add(context.GetSystemType(SystemType.UInt16), context.GetSystemType(SystemType.UInt16Enum));
			enumMap.Add(context.GetSystemType(SystemType.UInt32), context.GetSystemType(SystemType.UInt32Enum));
			enumMap.Add(context.GetSystemType(SystemType.UInt64), context.GetSystemType(SystemType.UInt64Enum));
			KeyValuePair<TypeReference, TypeReference>[] missingSharedEnumTypes = enumMap.Where((KeyValuePair<TypeReference, TypeReference> kvp) => kvp.Value == null).ToArray();
			if (missingSharedEnumTypes.Any())
			{
				throw new InvalidOperationException("One or more shared enum types could not be found.  Was the embedded mscorlib.xml file present when UnityLinker Ran?\nMissing types were\n" + missingSharedEnumTypes.AggregateWithNewLine());
			}
			context.SetSharedEnumTypes(enumMap.AsReadOnly());
		}
	}

	private void BuildIl2CppTypesArray(TypeContext context, ITypeFactory typeFactory)
	{
		EditContext editContext = context.CreateEditContext();
		TypeDefinition activationFactory = null;
		if (_settings.Parameters.SupportWindowsRuntime)
		{
			TypeAttributes activationFactoryAttributes = TypeAttributes.ClassSemanticMask | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime | TypeAttributes.BeforeFieldInit;
			TypeDefinitionBuilder typeDefinitionBuilder = editContext.BuildClass(string.Empty, "IActivationFactory", activationFactoryAttributes);
			typeDefinitionBuilder.BuildMethod("ActivateInstance", MethodAttributes.Public | MethodAttributes.Virtual, context.GetSystemType(SystemType.Object)).WithMethodImplAttributes(MethodImplAttributes.IL);
			activationFactory = typeDefinitionBuilder.CompleteBuildStage(typeFactory);
		}
		TypeDefinition il2cppComObject = editContext.BuildClass("System", "__Il2CppComObject", TypeAttributes.BeforeFieldInit).CompleteBuildStage(typeFactory);
		TypeDefinition il2cppComDelegateType = null;
		if (_settings.Parameters.SupportWindowsRuntime)
		{
			il2cppComDelegateType = editContext.BuildType("System", "__Il2CppComDelegate", TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, il2cppComObject, MetadataType.Class).CompleteBuildStage(typeFactory);
		}
		TypeDefinition il2cppFullySharedType = null;
		TypeDefinition il2cppFullySharedStructType = null;
		if (!_settings.Parameters.DisableFullGenericSharing)
		{
			il2cppFullySharedType = editContext.BuildStruct("Unity.IL2CPP.Metadata", "__Il2CppFullySharedGenericType", TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit).CompleteBuildStage(typeFactory, RuntimeStorageKind.VariableSizedAny);
			il2cppFullySharedStructType = editContext.BuildStruct("Unity.IL2CPP.Metadata", "__Il2CppFullySharedGenericStructType", TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit).CompleteBuildStage(typeFactory, RuntimeStorageKind.VariableSizedValueType);
		}
		FunctionPointerType il2cppFunctionPointer = editContext.BuildFunctionPointer(_context.GetSystemType(SystemType.Void)).Complete();
		TypeReference[] types = new TypeReference[typeof(Il2CppCustomType).GetFields().Length];
		types[0] = activationFactory;
		types[1] = il2cppComObject;
		types[2] = il2cppComDelegateType;
		types[3] = il2cppFullySharedType;
		types[4] = il2cppFullySharedStructType;
		types[5] = il2cppFunctionPointer;
		_context.SetIl2CppTypes(types);
	}

	private void ValidateIl2CppExpectations(TypeContext context)
	{
		TypeDefinition intPtr = context.GetSystemType(SystemType.IntPtr);
		if (intPtr != null)
		{
			ThrowIfUnexpectedStaticFieldsExist(intPtr, intPtr.Fields.Where((FieldDefinition f) => f.IsStatic && f.Name != "Zero"));
		}
		TypeDefinition uintPtr = context.GetSystemType(SystemType.IntPtr);
		if (uintPtr != null)
		{
			ThrowIfUnexpectedStaticFieldsExist(uintPtr, uintPtr.Fields.Where((FieldDefinition f) => f.IsStatic && f.Name != "Zero"));
		}
		TypeDefinition bitConverter = context.GetSystemType(SystemType.BitConverter);
		if (bitConverter != null)
		{
			ThrowIfUnexpectedStaticFieldsExist(uintPtr, bitConverter.Fields.Where((FieldDefinition f) => f.IsStatic && f.Name != "IsLittleEndian"));
		}
	}

	private static void ThrowIfUnexpectedStaticFieldsExist(TypeDefinition typeDefinition, IEnumerable<FieldDefinition> staticFields)
	{
		if (staticFields.Any())
		{
			throw new InvalidOperationException($"Unexpected static field(s) found on {typeDefinition} - {string.Join(", ", staticFields.Select((FieldDefinition f) => f.Name))}");
		}
	}

	private void BuildGraftedArrayInterfaceArray()
	{
		TypeDefinition array = _context.GetSystemType(SystemType.Array);
		TypeDefinition iCollectionType = _context.GetSystemType(SystemType.ICollection_1);
		TypeDefinition iListType = _context.GetSystemType(SystemType.IList_1);
		TypeDefinition iEnumerableType = _context.GetSystemType(SystemType.IEnumerable_1);
		TypeDefinition iReadOnlyListType = _context.GetSystemType(SystemType.IReadOnlyList_1);
		TypeDefinition iReadOnlyCollectionType = _context.GetSystemType(SystemType.IReadOnlyCollection_1);
		_context.SetGraftedArrayInterfaceTypes(new TypeDefinition[5] { iListType, iCollectionType, iEnumerableType, iReadOnlyListType, iReadOnlyCollectionType }.Where((TypeDefinition t) => t != null).ToArray().AsReadOnly());
		List<MethodDefinition> graftedArrayMethods = new List<MethodDefinition>();
		if (iCollectionType != null)
		{
			graftedArrayMethods.AddRange(GetArrayInterfaceMethods(array, iCollectionType, "InternalArray__ICollection_"));
		}
		if (iListType != null)
		{
			graftedArrayMethods.AddRange(GetArrayInterfaceMethods(array, iListType, "InternalArray__"));
		}
		if (iEnumerableType != null)
		{
			graftedArrayMethods.AddRange(GetArrayInterfaceMethods(array, iEnumerableType, "InternalArray__IEnumerable_"));
		}
		if (iReadOnlyListType != null)
		{
			graftedArrayMethods.AddRange(GetArrayInterfaceMethods(array, iReadOnlyListType, "InternalArray__IReadOnlyList_"));
		}
		if (iReadOnlyCollectionType != null)
		{
			graftedArrayMethods.AddRange(GetArrayInterfaceMethods(array, iReadOnlyCollectionType, "InternalArray__IReadOnlyCollection_"));
		}
		_context.SetGraftedArrayInterfaceMethods(graftedArrayMethods.AsReadOnly());
	}

	internal static IEnumerable<MethodDefinition> GetArrayInterfaceMethods(TypeDefinition arrayType, TypeDefinition interfaceType, string arrayMethodPrefix)
	{
		if (interfaceType == null)
		{
			yield break;
		}
		foreach (MethodDefinition method in interfaceType.Methods)
		{
			string methodName = method.Name;
			MethodDefinition arrayMethod = arrayType.Methods.SingleOrDefault((MethodDefinition m) => m.Name.Length == arrayMethodPrefix.Length + methodName.Length && m.Name.StartsWith(arrayMethodPrefix) && m.Name.EndsWith(methodName));
			if (arrayMethod != null && arrayMethod.HasGenericParameters)
			{
				yield return arrayMethod;
			}
		}
	}

	internal ReadOnlyCollection<AssemblyDefinition> GetAssembliesSortedForPublicExposure()
	{
		ReadOnlyCollection<AssemblyDefinition> source = _context.AssembliesOrderedByCostToProcess;
		List<AssemblyDefinition> list = new List<AssemblyDefinition>(source);
		list.Sort(new AssemblyDependencyComparer(source));
		return list.AsReadOnly();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_cecilContext?.Dispose();
			_disposed = true;
		}
	}

	private void PopulateGenerHiddenMethodUsage()
	{
		for (int iteration = 0; iteration < 10; ++iteration)
		{
			bool anyFlips = false;
			foreach (var assembly in _context.AssembliesOrderedByCostToProcess)
			{
				foreach (var method in assembly.AllMethods())
				{
					if (!method.HasBody)
						continue;

					if (!method.HasGenericParameters && !method.DeclaringType.HasGenericParameters)
						continue;

					if (method.IsGenericHiddenMethodNeverUsed)
						continue;

					bool hasNoAnyGenericMethodCalls = true;
					var instructions = method.Body.Instructions;
					foreach (var instr in instructions)
					{
						if (instr.OpCode == OpCodes.Initobj ||
							instr.OpCode == OpCodes.Ldelema ||
							instr.OpCode == OpCodes.Ldelem_I || instr.OpCode == OpCodes.Stelem_I ||
							instr.OpCode == OpCodes.Ldelem_I1 || instr.OpCode == OpCodes.Stelem_I ||
							instr.OpCode == OpCodes.Ldelem_I2 || instr.OpCode == OpCodes.Stelem_I2 ||
							instr.OpCode == OpCodes.Ldelem_I4 || instr.OpCode == OpCodes.Stelem_I4 ||
							instr.OpCode == OpCodes.Ldelem_I8 || instr.OpCode == OpCodes.Stelem_I8 ||
							instr.OpCode == OpCodes.Ldelem_R4 || instr.OpCode == OpCodes.Stelem_R4 ||
							instr.OpCode == OpCodes.Ldelem_R8 || instr.OpCode == OpCodes.Stelem_R8 ||
							instr.OpCode == OpCodes.Ldelem_Ref || instr.OpCode == OpCodes.Stelem_Ref ||
							instr.OpCode == OpCodes.Ldelem_Any || instr.OpCode == OpCodes.Stelem_Any ||
							instr.OpCode == OpCodes.Ldelem_U1 ||
							instr.OpCode == OpCodes.Ldelem_U2 ||
							instr.OpCode == OpCodes.Ldelem_U4)
							continue;

						if (instr.OpCode == OpCodes.Newobj ||
							instr.OpCode == OpCodes.Newarr ||
							instr.OpCode == OpCodes.Castclass)
						{
							if (instr.Operand is MethodReference)
							{
								var operandMethod = instr.Operand as MethodReference;

								if (operandMethod.FullName == "System.Void System.ByReference`1<T>::.ctor(T&)")
									continue;

								if (operandMethod.ContainsGenericParameter || operandMethod.DeclaringType.ContainsGenericParameter)
								{
									hasNoAnyGenericMethodCalls = false;
									break;
								}
							}
							if (instr.Operand is MethodRefOnTypeInst ||
								instr.Operand is GenericInstanceType)
							{
								hasNoAnyGenericMethodCalls = false;
								break;
							}
						}

						if (instr.Operand is MethodReference)
						{
							var operandMethod = instr.Operand as MethodReference;

							if (operandMethod is GenericInstanceMethod)
							{
								var genericOperandMethod = operandMethod as GenericInstanceMethod;
								if (genericOperandMethod.HasGenericArguments)
								{
									hasNoAnyGenericMethodCalls = false;
									break;
								}
							}

							if (operandMethod.DeclaringType is GenericInstanceType)
							{
								var genericInstanceType = operandMethod.DeclaringType as GenericInstanceType;
								if (operandMethod.Name == "Invoke" && genericInstanceType.TypeDef.BaseType.FullName == "System.MulticastDelegate")
								{
									if (operandMethod is MethodRefOnTypeInst)
									{
										var operandMethodOnTypeInst = operandMethod as MethodRefOnTypeInst;
										operandMethodOnTypeInst.MethodDef.InitializeInternalGenericUsage(true);
									}
									continue;
								}
							}


							if (operandMethod.ContainsGenericParameter || operandMethod.DeclaringType.ContainsGenericParameter)
							{
								if (!operandMethod.IsGenericHiddenMethodNeverUsed)
								{
									hasNoAnyGenericMethodCalls = false;
									break;
								}
							}
						}

						if (instr.Operand is GenericParameter)
						{
							hasNoAnyGenericMethodCalls = false;
							break;
						}

						if (instr.Operand is FieldInst)
						{
							var field = instr.Operand as FieldInst;
							if (field.IsStatic)
							{
								hasNoAnyGenericMethodCalls = false;
								break;
							}
						}
					}

					//if( hasNoAnyGenericMethodCalls )
					//	Console.WriteLine($"{method.FullName}");

					anyFlips |= hasNoAnyGenericMethodCalls;
					method.InitializeInternalGenericUsage(hasNoAnyGenericMethodCalls);
				}
			}

			if (!anyFlips)
				break;
		}
	}
}
