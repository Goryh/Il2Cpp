using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NiceIO;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.Api.Output.Analytics;

namespace il2cpp;

public static class AnalyticsWriter
{
	[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonSerializable(typeof(Il2CppAnalyticsOutput), GenerationMode = JsonSourceGenerationMode.Serialization)]
	[GeneratedCode("System.Text.Json.SourceGeneration", "7.0.9.1816")]
	internal class AnalyticsWriterJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
	{
		private JsonTypeInfo<string>? _String;

		private JsonTypeInfo<bool>? _Boolean;

		private JsonTypeInfo<bool?>? _NullableBoolean;

		private JsonTypeInfo<int>? _Int32;

		private JsonTypeInfo<int?>? _NullableInt32;

		private JsonTypeInfo<string[]>? _StringArray;

		private JsonTypeInfo<Il2CppDataTable>? _Il2CppDataTable;

		private JsonTypeInfo<Il2CppAnalyticsOutput>? _Il2CppAnalyticsOutput;

		private static AnalyticsWriterJsonContext? s_defaultContext;

		private static readonly JsonEncodedText PropName_DataTable = JsonEncodedText.Encode("DataTable");

		private static readonly JsonEncodedText PropName_build_event_id = JsonEncodedText.Encode("build_event_id");

		private static readonly JsonEncodedText PropName_node_executed = JsonEncodedText.Encode("node_executed");

		private static readonly JsonEncodedText PropName_attribute_total_count_eager_static_constructor = JsonEncodedText.Encode("attribute_total_count_eager_static_constructor");

		private static readonly JsonEncodedText PropName_attribute_total_count_set_option = JsonEncodedText.Encode("attribute_total_count_set_option");

		private static readonly JsonEncodedText PropName_attribute_total_count_generate_into_own_cpp_file = JsonEncodedText.Encode("attribute_total_count_generate_into_own_cpp_file");

		private static readonly JsonEncodedText PropName_attribute_total_count_ignore_by_deep_profiler = JsonEncodedText.Encode("attribute_total_count_ignore_by_deep_profiler");

		private static readonly JsonEncodedText PropName_extra_types_total_count = JsonEncodedText.Encode("extra_types_total_count");

		private static readonly JsonEncodedText PropName_option_extra_types_file_count = JsonEncodedText.Encode("option_extra_types_file_count");

		private static readonly JsonEncodedText PropName_option_debug_assembly_name_count = JsonEncodedText.Encode("option_debug_assembly_name_count");

		private static readonly JsonEncodedText PropName_option_additional_cpp_count = JsonEncodedText.Encode("option_additional_cpp_count");

		private static readonly JsonEncodedText PropName_option_emit_null_checks = JsonEncodedText.Encode("option_emit_null_checks");

		private static readonly JsonEncodedText PropName_option_enable_stacktrace = JsonEncodedText.Encode("option_enable_stacktrace");

		private static readonly JsonEncodedText PropName_option_enable_deep_profiler = JsonEncodedText.Encode("option_enable_deep_profiler");

		private static readonly JsonEncodedText PropName_option_enable_stats = JsonEncodedText.Encode("option_enable_stats");

		private static readonly JsonEncodedText PropName_option_enable_array_bounds_check = JsonEncodedText.Encode("option_enable_array_bounds_check");

		private static readonly JsonEncodedText PropName_option_enable_divide_by_zero_check = JsonEncodedText.Encode("option_enable_divide_by_zero_check");

		private static readonly JsonEncodedText PropName_option_emit_comments = JsonEncodedText.Encode("option_emit_comments");

		private static readonly JsonEncodedText PropName_option_disable_generic_sharing = JsonEncodedText.Encode("option_disable_generic_sharing");

		private static readonly JsonEncodedText PropName_option_maximum_recursive_generic_depth = JsonEncodedText.Encode("option_maximum_recursive_generic_depth");

		private static readonly JsonEncodedText PropName_option_generic_virtual_method_iterations = JsonEncodedText.Encode("option_generic_virtual_method_iterations");

		private static readonly JsonEncodedText PropName_option_code_generation_option = JsonEncodedText.Encode("option_code_generation_option");

		private static readonly JsonEncodedText PropName_option_file_generation_option = JsonEncodedText.Encode("option_file_generation_option");

		private static readonly JsonEncodedText PropName_option_generics_option = JsonEncodedText.Encode("option_generics_option");

		private static readonly JsonEncodedText PropName_option_feature = JsonEncodedText.Encode("option_feature");

		private static readonly JsonEncodedText PropName_option_diagnostic_option = JsonEncodedText.Encode("option_diagnostic_option");

		private static readonly JsonEncodedText PropName_option_convert_to_cpp = JsonEncodedText.Encode("option_convert_to_cpp");

		private static readonly JsonEncodedText PropName_option_compile_cpp = JsonEncodedText.Encode("option_compile_cpp");

		private static readonly JsonEncodedText PropName_option_development_mode = JsonEncodedText.Encode("option_development_mode");

		private static readonly JsonEncodedText PropName_option_enable_debugger = JsonEncodedText.Encode("option_enable_debugger");

		private static readonly JsonEncodedText PropName_option_generate_usym_file = JsonEncodedText.Encode("option_generate_usym_file");

		private static readonly JsonEncodedText PropName_option_jobs = JsonEncodedText.Encode("option_jobs");

		public JsonTypeInfo<string> String => _String ?? (_String = Create_String(base.Options, makeReadOnly: true));

		public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = Create_Boolean(base.Options, makeReadOnly: true));

		public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = Create_NullableBoolean(base.Options, makeReadOnly: true));

		public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = Create_Int32(base.Options, makeReadOnly: true));

		public JsonTypeInfo<int?> NullableInt32 => _NullableInt32 ?? (_NullableInt32 = Create_NullableInt32(base.Options, makeReadOnly: true));

		public JsonTypeInfo<string[]> StringArray => _StringArray ?? (_StringArray = Create_StringArray(base.Options, makeReadOnly: true));

		public JsonTypeInfo<Il2CppDataTable> Il2CppDataTable => _Il2CppDataTable ?? (_Il2CppDataTable = Create_Il2CppDataTable(base.Options, makeReadOnly: true));

		public JsonTypeInfo<Il2CppAnalyticsOutput> Il2CppAnalyticsOutput => _Il2CppAnalyticsOutput ?? (_Il2CppAnalyticsOutput = Create_Il2CppAnalyticsOutput(base.Options, makeReadOnly: true));

		private static JsonSerializerOptions s_defaultOptions { get; } = new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			IgnoreReadOnlyFields = false,
			IgnoreReadOnlyProperties = false,
			IncludeFields = false,
			WriteIndented = true
		};

		public static AnalyticsWriterJsonContext Default => s_defaultContext ?? (s_defaultContext = new AnalyticsWriterJsonContext(new JsonSerializerOptions(s_defaultOptions)));

		protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

		private JsonTypeInfo<string> Create_String(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<string> jsonTypeInfo = null;
			JsonConverter customConverter;
			jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(string))) == null) ? JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter) : JsonMetadataServices.CreateValueInfo<string>(options, customConverter));
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<bool> jsonTypeInfo = null;
			JsonConverter customConverter;
			jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(bool))) == null) ? JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter) : JsonMetadataServices.CreateValueInfo<bool>(options, customConverter));
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private JsonTypeInfo<bool?> Create_NullableBoolean(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<bool?> jsonTypeInfo = null;
			JsonConverter customConverter;
			jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(bool?))) == null) ? JsonMetadataServices.CreateValueInfo<bool?>(options, JsonMetadataServices.GetNullableConverter<bool>(options)) : JsonMetadataServices.CreateValueInfo<bool?>(options, customConverter));
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<int> jsonTypeInfo = null;
			JsonConverter customConverter;
			jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(int))) == null) ? JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter) : JsonMetadataServices.CreateValueInfo<int>(options, customConverter));
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private JsonTypeInfo<int?> Create_NullableInt32(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<int?> jsonTypeInfo = null;
			JsonConverter customConverter;
			jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(int?))) == null) ? JsonMetadataServices.CreateValueInfo<int?>(options, JsonMetadataServices.GetNullableConverter<int>(options)) : JsonMetadataServices.CreateValueInfo<int?>(options, customConverter));
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private JsonTypeInfo<string[]> Create_StringArray(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<string[]> jsonTypeInfo = null;
			JsonConverter customConverter;
			if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(string[]))) != null)
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string[]>(options, customConverter);
			}
			else
			{
				JsonCollectionInfoValues<string[]> info = new JsonCollectionInfoValues<string[]>
				{
					ObjectCreator = null,
					NumberHandling = JsonNumberHandling.Strict,
					SerializeHandler = StringArraySerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateArrayInfo(options, info);
			}
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private void StringArraySerializeHandler(Utf8JsonWriter writer, string[]? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartArray();
			for (int i = 0; i < value.Length; i++)
			{
				writer.WriteStringValue(value[i]);
			}
			writer.WriteEndArray();
		}

		private JsonTypeInfo<Il2CppDataTable> Create_Il2CppDataTable(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<Il2CppDataTable> jsonTypeInfo = null;
			JsonConverter customConverter;
			if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Il2CppDataTable))) != null)
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Il2CppDataTable>(options, customConverter);
			}
			else
			{
				JsonObjectInfoValues<Il2CppDataTable> objectInfo = new JsonObjectInfoValues<Il2CppDataTable>
				{
					ObjectCreator = () => new Il2CppDataTable(),
					ObjectWithParameterizedConstructorCreator = null,
					PropertyMetadataInitializer = null,
					ConstructorParameterMetadataInitializer = null,
					NumberHandling = JsonNumberHandling.Strict,
					SerializeHandler = Il2CppDataTableSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			}
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private void Il2CppDataTableSerializeHandler(Utf8JsonWriter writer, Il2CppDataTable? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			if (value.build_event_id != null)
			{
				writer.WriteString(PropName_build_event_id, value.build_event_id);
			}
			if (value.node_executed.HasValue)
			{
				writer.WritePropertyName(PropName_node_executed);
				JsonSerializer.Serialize(writer, value.node_executed, Default.NullableBoolean);
			}
			writer.WriteNumber(PropName_attribute_total_count_eager_static_constructor, value.attribute_total_count_eager_static_constructor);
			writer.WriteNumber(PropName_attribute_total_count_set_option, value.attribute_total_count_set_option);
			writer.WriteNumber(PropName_attribute_total_count_generate_into_own_cpp_file, value.attribute_total_count_generate_into_own_cpp_file);
			writer.WriteNumber(PropName_attribute_total_count_ignore_by_deep_profiler, value.attribute_total_count_ignore_by_deep_profiler);
			writer.WriteNumber(PropName_extra_types_total_count, value.extra_types_total_count);
			if (value.option_extra_types_file_count.HasValue)
			{
				writer.WritePropertyName(PropName_option_extra_types_file_count);
				JsonSerializer.Serialize(writer, value.option_extra_types_file_count, Default.NullableInt32);
			}
			if (value.option_debug_assembly_name_count.HasValue)
			{
				writer.WritePropertyName(PropName_option_debug_assembly_name_count);
				JsonSerializer.Serialize(writer, value.option_debug_assembly_name_count, Default.NullableInt32);
			}
			if (value.option_additional_cpp_count.HasValue)
			{
				writer.WritePropertyName(PropName_option_additional_cpp_count);
				JsonSerializer.Serialize(writer, value.option_additional_cpp_count, Default.NullableInt32);
			}
			if (value.option_emit_null_checks.HasValue)
			{
				writer.WritePropertyName(PropName_option_emit_null_checks);
				JsonSerializer.Serialize(writer, value.option_emit_null_checks, Default.NullableBoolean);
			}
			if (value.option_enable_stacktrace.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_stacktrace);
				JsonSerializer.Serialize(writer, value.option_enable_stacktrace, Default.NullableBoolean);
			}
			if (value.option_enable_deep_profiler.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_deep_profiler);
				JsonSerializer.Serialize(writer, value.option_enable_deep_profiler, Default.NullableBoolean);
			}
			if (value.option_enable_stats.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_stats);
				JsonSerializer.Serialize(writer, value.option_enable_stats, Default.NullableBoolean);
			}
			if (value.option_enable_array_bounds_check.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_array_bounds_check);
				JsonSerializer.Serialize(writer, value.option_enable_array_bounds_check, Default.NullableBoolean);
			}
			if (value.option_enable_divide_by_zero_check.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_divide_by_zero_check);
				JsonSerializer.Serialize(writer, value.option_enable_divide_by_zero_check, Default.NullableBoolean);
			}
			if (value.option_emit_comments.HasValue)
			{
				writer.WritePropertyName(PropName_option_emit_comments);
				JsonSerializer.Serialize(writer, value.option_emit_comments, Default.NullableBoolean);
			}
			if (value.option_disable_generic_sharing.HasValue)
			{
				writer.WritePropertyName(PropName_option_disable_generic_sharing);
				JsonSerializer.Serialize(writer, value.option_disable_generic_sharing, Default.NullableBoolean);
			}
			if (value.option_maximum_recursive_generic_depth.HasValue)
			{
				writer.WritePropertyName(PropName_option_maximum_recursive_generic_depth);
				JsonSerializer.Serialize(writer, value.option_maximum_recursive_generic_depth, Default.NullableInt32);
			}
			if (value.option_generic_virtual_method_iterations.HasValue)
			{
				writer.WritePropertyName(PropName_option_generic_virtual_method_iterations);
				JsonSerializer.Serialize(writer, value.option_generic_virtual_method_iterations, Default.NullableInt32);
			}
			if (value.option_code_generation_option != null)
			{
				writer.WritePropertyName(PropName_option_code_generation_option);
				StringArraySerializeHandler(writer, value.option_code_generation_option);
			}
			if (value.option_file_generation_option != null)
			{
				writer.WritePropertyName(PropName_option_file_generation_option);
				StringArraySerializeHandler(writer, value.option_file_generation_option);
			}
			if (value.option_generics_option != null)
			{
				writer.WritePropertyName(PropName_option_generics_option);
				StringArraySerializeHandler(writer, value.option_generics_option);
			}
			if (value.option_feature != null)
			{
				writer.WritePropertyName(PropName_option_feature);
				StringArraySerializeHandler(writer, value.option_feature);
			}
			if (value.option_diagnostic_option != null)
			{
				writer.WritePropertyName(PropName_option_diagnostic_option);
				StringArraySerializeHandler(writer, value.option_diagnostic_option);
			}
			if (value.option_convert_to_cpp.HasValue)
			{
				writer.WritePropertyName(PropName_option_convert_to_cpp);
				JsonSerializer.Serialize(writer, value.option_convert_to_cpp, Default.NullableBoolean);
			}
			if (value.option_compile_cpp.HasValue)
			{
				writer.WritePropertyName(PropName_option_compile_cpp);
				JsonSerializer.Serialize(writer, value.option_compile_cpp, Default.NullableBoolean);
			}
			if (value.option_development_mode.HasValue)
			{
				writer.WritePropertyName(PropName_option_development_mode);
				JsonSerializer.Serialize(writer, value.option_development_mode, Default.NullableBoolean);
			}
			if (value.option_enable_debugger.HasValue)
			{
				writer.WritePropertyName(PropName_option_enable_debugger);
				JsonSerializer.Serialize(writer, value.option_enable_debugger, Default.NullableBoolean);
			}
			if (value.option_generate_usym_file.HasValue)
			{
				writer.WritePropertyName(PropName_option_generate_usym_file);
				JsonSerializer.Serialize(writer, value.option_generate_usym_file, Default.NullableBoolean);
			}
			if (value.option_jobs.HasValue)
			{
				writer.WritePropertyName(PropName_option_jobs);
				JsonSerializer.Serialize(writer, value.option_jobs, Default.NullableInt32);
			}
			writer.WriteEndObject();
		}

		private JsonTypeInfo<Il2CppAnalyticsOutput> Create_Il2CppAnalyticsOutput(JsonSerializerOptions options, bool makeReadOnly)
		{
			JsonTypeInfo<Il2CppAnalyticsOutput> jsonTypeInfo = null;
			JsonConverter customConverter;
			if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Il2CppAnalyticsOutput))) != null)
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Il2CppAnalyticsOutput>(options, customConverter);
			}
			else
			{
				JsonObjectInfoValues<Il2CppAnalyticsOutput> objectInfo = new JsonObjectInfoValues<Il2CppAnalyticsOutput>
				{
					ObjectCreator = () => new Il2CppAnalyticsOutput(),
					ObjectWithParameterizedConstructorCreator = null,
					PropertyMetadataInitializer = null,
					ConstructorParameterMetadataInitializer = null,
					NumberHandling = JsonNumberHandling.Strict,
					SerializeHandler = Il2CppAnalyticsOutputSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			}
			if (makeReadOnly)
			{
				jsonTypeInfo.MakeReadOnly();
			}
			return jsonTypeInfo;
		}

		private void Il2CppAnalyticsOutputSerializeHandler(Utf8JsonWriter writer, Il2CppAnalyticsOutput? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			if (value.DataTable != null)
			{
				writer.WritePropertyName(PropName_DataTable);
				Il2CppDataTableSerializeHandler(writer, value.DataTable);
			}
			writer.WriteEndObject();
		}

		public AnalyticsWriterJsonContext()
			: base(null)
		{
		}

		public AnalyticsWriterJsonContext(JsonSerializerOptions options)
			: base(options)
		{
		}

		private static JsonConverter? GetRuntimeProvidedCustomConverter(JsonSerializerOptions options, Type type)
		{
			IList<JsonConverter> converters = options.Converters;
			for (int i = 0; i < converters.Count; i++)
			{
				JsonConverter converter = converters[i];
				if (!converter.CanConvert(type))
				{
					continue;
				}
				if (converter is JsonConverterFactory factory)
				{
					converter = factory.CreateConverter(type, options);
					if (converter == null || converter is JsonConverterFactory)
					{
						throw new InvalidOperationException($"The converter '{factory.GetType()}' cannot return null or a JsonConverterFactory instance.");
					}
				}
				return converter;
			}
			return null;
		}

		public override JsonTypeInfo GetTypeInfo(Type type)
		{
			if (type == typeof(Il2CppAnalyticsOutput))
			{
				return Il2CppAnalyticsOutput;
			}
			if (type == typeof(Il2CppDataTable))
			{
				return Il2CppDataTable;
			}
			if (type == typeof(string))
			{
				return String;
			}
			if (type == typeof(bool?))
			{
				return NullableBoolean;
			}
			if (type == typeof(bool))
			{
				return Boolean;
			}
			if (type == typeof(int))
			{
				return Int32;
			}
			if (type == typeof(int?))
			{
				return NullableInt32;
			}
			if (type == typeof(string[]))
			{
				return StringArray;
			}
			return null;
		}

		JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			if (type == typeof(Il2CppAnalyticsOutput))
			{
				return Create_Il2CppAnalyticsOutput(options, makeReadOnly: false);
			}
			if (type == typeof(Il2CppDataTable))
			{
				return Create_Il2CppDataTable(options, makeReadOnly: false);
			}
			if (type == typeof(string))
			{
				return Create_String(options, makeReadOnly: false);
			}
			if (type == typeof(bool?))
			{
				return Create_NullableBoolean(options, makeReadOnly: false);
			}
			if (type == typeof(bool))
			{
				return Create_Boolean(options, makeReadOnly: false);
			}
			if (type == typeof(int))
			{
				return Create_Int32(options, makeReadOnly: false);
			}
			if (type == typeof(int?))
			{
				return Create_NullableInt32(options, makeReadOnly: false);
			}
			if (type == typeof(string[]))
			{
				return Create_StringArray(options, makeReadOnly: false);
			}
			return null;
		}
	}

	public static void Write(NPath outputDirectory, Il2CppDataTable dataTable, Il2CppCommandLineArguments options)
	{
		if (!options.ConversionRequest.EnableAnalytics && !options.ConversionRequest.Settings.Feature.HasFlag(Features.EnableAnalytics))
		{
			return;
		}
		NPath nPath = outputDirectory.Combine("analytics.json");
		Il2CppAnalyticsOutput output = new Il2CppAnalyticsOutput
		{
			DataTable = (dataTable ?? new Il2CppDataTable())
		};
		AssignOptionsToDataTable(output.DataTable, options.ConversionRequest, options.ConversionRequest.Settings, options, options.SettingsForConversionAndCompilation);
		using FileStream stream = new FileStream(nPath, FileMode.OpenOrCreate);
		using Utf8JsonWriter writer = new Utf8JsonWriter((Stream)stream, new JsonWriterOptions
		{
			Indented = true
		});
		JsonSerializer.Serialize(writer, output, typeof(Il2CppAnalyticsOutput), AnalyticsWriterJsonContext.Default);
	}

	public static void AssignOptionsToDataTable(Il2CppDataTable dataTable, ConversionRequest options1, ConversionSettings options2, Il2CppCommandLineArguments options3, SettingsForConversionAndCompilation options4)
	{
		string[] extraTypesFile = options1.ExtraTypesFile;
		dataTable.option_extra_types_file_count = ((extraTypesFile != null) ? extraTypesFile.Length : 0);
		string[] debugAssemblyName = options1.DebugAssemblyName;
		dataTable.option_debug_assembly_name_count = ((debugAssemblyName != null) ? debugAssemblyName.Length : 0);
		string[] additionalCpp = options1.AdditionalCpp;
		dataTable.option_additional_cpp_count = ((additionalCpp != null) ? additionalCpp.Length : 0);
		dataTable.option_emit_null_checks = options2.EmitNullChecks;
		dataTable.option_enable_stacktrace = options2.EnableStacktrace;
		dataTable.option_enable_deep_profiler = options2.EnableDeepProfiler;
		dataTable.option_enable_stats = options2.EnableStats;
		dataTable.option_enable_array_bounds_check = options2.EnableArrayBoundsCheck;
		dataTable.option_enable_divide_by_zero_check = options2.EnableDivideByZeroCheck;
		dataTable.option_emit_comments = options2.EmitComments;
		dataTable.option_disable_generic_sharing = options2.DisableGenericSharing;
		dataTable.option_maximum_recursive_generic_depth = options2.MaximumRecursiveGenericDepth;
		dataTable.option_generic_virtual_method_iterations = options2.GenericVirtualMethodIterations;
		dataTable.option_code_generation_option = ToStringArray((int)options2.CodeGenerationOption, options2.CodeGenerationOption);
		dataTable.option_file_generation_option = ToStringArray((int)options2.FileGenerationOption, options2.FileGenerationOption);
		dataTable.option_generics_option = ToStringArray((int)options2.GenericsOption, options2.GenericsOption);
		dataTable.option_feature = ToStringArray((int)options2.Feature, options2.Feature);
		dataTable.option_diagnostic_option = ToStringArray((long)options2.DiagnosticOption, options2.DiagnosticOption);
		dataTable.option_convert_to_cpp = options3.ConvertToCpp;
		dataTable.option_compile_cpp = options3.CompileCpp;
		dataTable.option_development_mode = options4.DevelopmentMode;
		dataTable.option_enable_debugger = options4.EnableDebugger;
		dataTable.option_generate_usym_file = options4.GenerateUsymFile;
		dataTable.option_jobs = options4.Jobs;
	}

	private static string[] ToStringArray(int instanceValue, CodeGenerationOptions instanceValueAsType)
	{
		List<string> results = new List<string>();
		CodeGenerationOptions[] values = Enum.GetValues<CodeGenerationOptions>();
		foreach (CodeGenerationOptions value in values)
		{
			int casted = (int)value;
			if (casted != 0 && (instanceValue & casted) != 0)
			{
				results.Add(Enum.GetName(value));
			}
		}
		return results.ToArray();
	}

	private static string[] ToStringArray(int instanceValue, FileGenerationOptions instanceValueAsType)
	{
		List<string> results = new List<string>();
		FileGenerationOptions[] values = Enum.GetValues<FileGenerationOptions>();
		foreach (FileGenerationOptions value in values)
		{
			int casted = (int)value;
			if (casted != 0 && (instanceValue & casted) != 0)
			{
				results.Add(Enum.GetName(value));
			}
		}
		return results.ToArray();
	}

	private static string[] ToStringArray(int instanceValue, GenericsOptions instanceValueAsType)
	{
		List<string> results = new List<string>();
		GenericsOptions[] values = Enum.GetValues<GenericsOptions>();
		foreach (GenericsOptions value in values)
		{
			int casted = (int)value;
			if (casted != 0 && (instanceValue & casted) != 0)
			{
				results.Add(Enum.GetName(value));
			}
		}
		return results.ToArray();
	}

	private static string[] ToStringArray(int instanceValue, Features instanceValueAsType)
	{
		List<string> results = new List<string>();
		Features[] values = Enum.GetValues<Features>();
		foreach (Features value in values)
		{
			int casted = (int)value;
			if (casted != 0 && (instanceValue & casted) != 0)
			{
				results.Add(Enum.GetName(value));
			}
		}
		return results.ToArray();
	}

	private static string[] ToStringArray(long instanceValue, DiagnosticOptions instanceValueAsType)
	{
		List<string> results = new List<string>();
		DiagnosticOptions[] values = Enum.GetValues<DiagnosticOptions>();
		foreach (DiagnosticOptions value in values)
		{
			long casted = (long)value;
			if (casted != 0L && (instanceValue & casted) != 0L)
			{
				results.Add(Enum.GetName(value));
			}
		}
		return results.ToArray();
	}
}
