using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata;

public class GenericClassWriter : MetadataWriter<IGeneratedCodeWriter>
{
	public GenericClassWriter(IGeneratedCodeWriter writer)
		: base(writer)
	{
	}

	public void WriteDefinition(ReadOnlyContext context, Il2CppGenericInstanceRuntimeType type)
	{
		base.Writer.WriteExternForIl2CppGenericInst(type.GenericArguments);
		if (!context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(type.GenericTypeDefinition.Type))
		{
			base.Writer.WriteExternForIl2CppType(type.GenericTypeDefinition);
		}
		WriteLine($"Il2CppGenericClass {context.Global.Services.Naming.ForGenericClass(context, type.Type)} = {{ &{context.Global.Services.Naming.ForIl2CppType(context, type.GenericTypeDefinition)}, {{ &{context.Global.Services.Naming.ForGenericInst(context, type.GenericArguments)}, {"NULL"} }}, {"NULL"} }};");
	}
}
