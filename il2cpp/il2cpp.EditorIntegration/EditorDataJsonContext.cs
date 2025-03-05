using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using UnityEditorInternal;

namespace il2cpp.EditorIntegration;

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Il2CppToEditorData), GenerationMode = JsonSourceGenerationMode.Serialization)]
[GeneratedCode("System.Text.Json.SourceGeneration", "7.0.9.1816")]
internal class EditorDataJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<Il2CppMessageType>? _Il2CppMessageType;

	private JsonTypeInfo<string>? _String;

	private JsonTypeInfo<Message>? _Message;

	private JsonTypeInfo<List<Message>>? _ListMessage;

	private JsonTypeInfo<Il2CppToEditorData>? _Il2CppToEditorData;

	private static EditorDataJsonContext? s_defaultContext;

	private static readonly JsonEncodedText PropName_Messages = JsonEncodedText.Encode("Messages");

	private static readonly JsonEncodedText PropName_CommandLine = JsonEncodedText.Encode("CommandLine");

	private static readonly JsonEncodedText PropName_Type = JsonEncodedText.Encode("Type");

	private static readonly JsonEncodedText PropName_Text = JsonEncodedText.Encode("Text");

	public JsonTypeInfo<Il2CppMessageType> Il2CppMessageType => _Il2CppMessageType ?? (_Il2CppMessageType = Create_Il2CppMessageType(base.Options, makeReadOnly: true));

	public JsonTypeInfo<string> String => _String ?? (_String = Create_String(base.Options, makeReadOnly: true));

	public JsonTypeInfo<Message> Message => _Message ?? (_Message = Create_Message(base.Options, makeReadOnly: true));

	public JsonTypeInfo<List<Message>> ListMessage => _ListMessage ?? (_ListMessage = Create_ListMessage(base.Options, makeReadOnly: true));

	public JsonTypeInfo<Il2CppToEditorData> Il2CppToEditorData => _Il2CppToEditorData ?? (_Il2CppToEditorData = Create_Il2CppToEditorData(base.Options, makeReadOnly: true));

	private static JsonSerializerOptions s_defaultOptions { get; } = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		IgnoreReadOnlyFields = false,
		IgnoreReadOnlyProperties = false,
		IncludeFields = true,
		WriteIndented = true
	};

	public static EditorDataJsonContext Default => s_defaultContext ?? (s_defaultContext = new EditorDataJsonContext(new JsonSerializerOptions(s_defaultOptions)));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<Il2CppMessageType> Create_Il2CppMessageType(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<Il2CppMessageType> jsonTypeInfo = null;
		JsonConverter customConverter;
		jsonTypeInfo = ((options.Converters.Count <= 0 || (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Il2CppMessageType))) == null) ? JsonMetadataServices.CreateValueInfo<Il2CppMessageType>(options, JsonMetadataServices.GetEnumConverter<Il2CppMessageType>(options)) : JsonMetadataServices.CreateValueInfo<Il2CppMessageType>(options, customConverter));
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

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

	private JsonTypeInfo<Message> Create_Message(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<Message> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Message))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Message>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<Message> objectInfo = new JsonObjectInfoValues<Message>
			{
				ObjectCreator = () => new Message(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = MessageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void MessageSerializeHandler(Utf8JsonWriter writer, Message? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WritePropertyName(PropName_Type);
		JsonSerializer.Serialize(writer, value.Type, Default.Il2CppMessageType);
		if (value.Text != null)
		{
			writer.WriteString(PropName_Text, value.Text);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<List<Message>> Create_ListMessage(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<List<Message>> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(List<Message>))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<List<Message>>(options, customConverter);
		}
		else
		{
			JsonCollectionInfoValues<List<Message>> info = new JsonCollectionInfoValues<List<Message>>
			{
				ObjectCreator = () => new List<Message>(),
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = ListMessageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<Message>, Message>(options, info);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void ListMessageSerializeHandler(Utf8JsonWriter writer, List<Message>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			MessageSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<Il2CppToEditorData> Create_Il2CppToEditorData(JsonSerializerOptions options, bool makeReadOnly)
	{
		JsonTypeInfo<Il2CppToEditorData> jsonTypeInfo = null;
		JsonConverter customConverter;
		if (options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(options, typeof(Il2CppToEditorData))) != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<Il2CppToEditorData>(options, customConverter);
		}
		else
		{
			JsonObjectInfoValues<Il2CppToEditorData> objectInfo = new JsonObjectInfoValues<Il2CppToEditorData>
			{
				ObjectCreator = () => new Il2CppToEditorData(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = null,
				ConstructorParameterMetadataInitializer = null,
				NumberHandling = JsonNumberHandling.Strict,
				SerializeHandler = Il2CppToEditorDataSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
		}
		if (makeReadOnly)
		{
			jsonTypeInfo.MakeReadOnly();
		}
		return jsonTypeInfo;
	}

	private void Il2CppToEditorDataSerializeHandler(Utf8JsonWriter writer, Il2CppToEditorData? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		if (value.Messages != null)
		{
			writer.WritePropertyName(PropName_Messages);
			ListMessageSerializeHandler(writer, value.Messages);
		}
		if (value.CommandLine != null)
		{
			writer.WriteString(PropName_CommandLine, value.CommandLine);
		}
		writer.WriteEndObject();
	}

	public EditorDataJsonContext()
		: base(null)
	{
	}

	public EditorDataJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(Il2CppToEditorData))
		{
			return Il2CppToEditorData;
		}
		if (type == typeof(List<Message>))
		{
			return ListMessage;
		}
		if (type == typeof(Message))
		{
			return Message;
		}
		if (type == typeof(Il2CppMessageType))
		{
			return Il2CppMessageType;
		}
		if (type == typeof(string))
		{
			return String;
		}
		return null;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(Il2CppToEditorData))
		{
			return Create_Il2CppToEditorData(options, makeReadOnly: false);
		}
		if (type == typeof(List<Message>))
		{
			return Create_ListMessage(options, makeReadOnly: false);
		}
		if (type == typeof(Message))
		{
			return Create_Message(options, makeReadOnly: false);
		}
		if (type == typeof(Il2CppMessageType))
		{
			return Create_Il2CppMessageType(options, makeReadOnly: false);
		}
		if (type == typeof(string))
		{
			return Create_String(options, makeReadOnly: false);
		}
		return null;
	}
}
