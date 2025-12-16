using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using Beamable.Common.Semantics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using System.Linq;

namespace Beamable.Server.Common
{
	
	
	/// <summary>
	/// Custom JSON serialization settings optimized for Unity applications.
	/// </summary>
    public class UnitySerializationSettings : JsonSerializerSettings
    {
	    /// <summary>
	    /// Gets the singleton instance of <see cref="UnitySerializationSettings"/>.
	    /// </summary>
        public static UnitySerializationSettings Instance => _instance ??= new UnitySerializationSettings
        {
			Converters = new List<JsonConverter>
			{
				new MapOfStringConverter(),
				new StringToSomethingDictionaryConverter<string>(),
				new StringToSomethingDictionaryConverter<int>(),
				new StringToSomethingDictionaryConverter<long>(),
				new StringToSomethingDictionaryConverter<CurrencyPropertyList>(),
				new StringToSomethingDictionaryConverter<List<FederatedItemProxy>>(),
				new BeamAccountIdConverter(),
				new BeamCidConverter(),
				new BeamContentIdConverter(),
				new BeamContentManifestIdConverter(),
				new BeamGamerTagConverter(),
				new BeamPidConverter(),
				new BeamStatsConverter(),
				new ServiceNameConverter(),
				new OptionalConverter(),
				// THIS MUST BE LAST, because it is hacky, and falls back onto other converts as its _normal_ behaviour. If its not last, then other converts can run twice, which causes newtonsoft to explode.
				new UnitySerializationCallbackInvoker(),

			},
            ContractResolver = UnityJsonContractResolver.Instance
        };

        private static UnitySerializationSettings _instance;

        private UnitySerializationSettings()
        {

        }
    }

	public class OptionalConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (!(value is Optional optional))
				throw new Exception("Cannot handle non-optional value in optional convert");

			if (!optional.HasValue) return; // nothing to write!
			
			serializer.Serialize(writer, optional.GetValue());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{

			var instance = (Optional)Activator.CreateInstance(objectType);

			// Handle object representation (when JSON is { "Value": value, "HasValue": true }
			if (reader.TokenType == JsonToken.StartObject)
			{
				var jObject = JObject.Load(reader);
				var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				foreach (var field in fields)
				{
					if (jObject.TryGetValue(field.Name, out var token))
					{
						var fieldValue = token.ToObject(field.FieldType, serializer);
						field.SetValue(instance, fieldValue);
					}
				}

				return instance;
			}

			// Handle direct value representation (when JSON is { "value": value }
			var optionalType = instance.GetOptionalType();
			var value = serializer.Deserialize(reader, optionalType);
			instance.SetValue(value);
			return instance;
		}

		public override bool CanConvert(Type objectType)
		{
			var isOptional = objectType.IsAssignableTo(typeof(Optional));
			return isOptional;
		}
	}
	
	/// <summary>
	/// Custom JSON converter for JsonUtility that uses Newtonsoft.Json for serialization and deserialization.
	/// </summary>
    public class JsonUtilityConverter : JsonUtility.IConverter
    {
	    /// <summary>
	    /// Initializes the custom JSON converter by setting it as the active converter for JsonUtility.
	    /// </summary>
        public static void Init()
        {
            JsonUtility.Converter = new JsonUtilityConverter();
        }

        private JsonUtilityConverter() { }

        /// <summary>
        /// Serializes an object to JSON format.
        /// </summary>
        /// <param name="data">The object to be serialized.</param>
        /// <returns>The JSON representation of the object.</returns>
        public string ToJson(object data) => JsonConvert.SerializeObject(data, UnitySerializationSettings.Instance);

        /// <summary>
        /// Deserializes JSON into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="json">The JSON string to be deserialized.</param>
        /// <returns>The deserialized object of type T.</returns>
        public T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, UnitySerializationSettings.Instance);
    }

	public class MapOfStringConverter : JsonConverter<MapOfString>
	{
		public override void WriteJson(JsonWriter writer, MapOfString value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, new Dictionary<string, string>(value));
		}

		public override MapOfString ReadJson(JsonReader reader, Type objectType, MapOfString existingValue, bool hasExistingValue,
			JsonSerializer serializer)
		{
			var map = serializer.Deserialize<Dictionary<string, string>>(reader);
			return new MapOfString(map);
		}
	}

	/// <summary>
	/// Custom JSON converter for serializing and deserializing dictionaries with string keys and values of type T.
	/// </summary>
	/// <typeparam name="T">The type of values in the dictionary.</typeparam>
    public class StringToSomethingDictionaryConverter<T> : JsonConverter<SerializableDictionaryStringToSomething<T>>
    {
	    /// <summary>
	    /// Writes the dictionary to JSON format.
	    /// </summary>
	    public override void WriteJson(JsonWriter writer, SerializableDictionaryStringToSomething<T> value, JsonSerializer serializer)
	    {
		    // default to serializing an array...
		    var list = new KeyValue[value.Count];
		    var index = 0;
		    foreach (var kvp in value)
		    {
			    list[index++] = new KeyValue { name = kvp.Key, value = kvp.Value };
		    }
		    serializer.Serialize(writer, list);
	    }

	    /// <summary>
	    /// Reads the JSON and converts it back to a dictionary.
	    /// </summary>
	    public override SerializableDictionaryStringToSomething<T> ReadJson(JsonReader reader, Type objectType,
		    SerializableDictionaryStringToSomething<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
	    {
		    var instance = (SerializableDictionaryStringToSomething<T>)Activator.CreateInstance(objectType);


		    SerializableDictionaryStringToSomething<T> HandleArrayVariant()
		    {
			    var keyValues = serializer.Deserialize<KeyValue[]>(reader);
			    foreach (var kv in keyValues)
			    {
				    instance[kv.name] = kv.value;
			    }

			    return instance;
		    }


		    SerializableDictionaryStringToSomething<T> HandleObjectVariant()
		    {
			    try
			    {
				    var keysAndValues = serializer.Deserialize<KeysAndValues>(reader);
				    if (keysAndValues.keys.Length != keysAndValues.values.Length)
				    {
					    BeamableLogger.LogWarning(
						    $"Deserializing dictionary but keys and values were different values. keyCount=[{keysAndValues.keys.Length}] valueCount=[{keysAndValues.values.Length}]");
				    }

				    for (var i = 0; i < keysAndValues.keys.Length && i < keysAndValues.values.Length; i++)
				    {
					    instance[keysAndValues.keys[i]] = keysAndValues.values[i];
				    }

				    return instance;
			    }
			    catch (Exception ex)
			    {
				    BeamableLogger.LogError($"Failed to deserialize map type type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
				    throw;
			    }
		    }

		    return reader.TokenType == JsonToken.StartArray
			    ? HandleArrayVariant()
			    : HandleObjectVariant();
	    }

	    /// <summary>
	    /// Class representing a key-value pair for dictionary serialization.
	    /// </summary>
	    public class KeyValue
	    {
		    /// <summary>
		    /// The key of the dictionary entry.
		    /// </summary>
		    public string name;
		    /// <summary>
		    /// The value associated with the key.
		    /// </summary>
		    public T value;
	    }

	    /// <summary>
	    /// Class representing keys and values for dictionary serialization.
	    /// </summary>
	    public class KeysAndValues
	    {
		    /// <summary>
		    /// An array of keys in the dictionary.
		    /// </summary>
		    public string[] keys = Array.Empty<string>();
		    /// <summary>
		    /// An array of values in the dictionary.
		    /// </summary>
		    public T[] values = Array.Empty<T>();
	    }
    }


    /// <summary>
    /// Call the ISerializationCallbackReceiver methods, and then fallback to the existing converter...
    ///  adapted from https://github.com/JamesNK/Newtonsoft.Json/issues/719
    /// </summary>
    public class UnitySerializationCallbackInvoker : JsonConverter<ISerializationCallbackReceiver>, IDisposable
    {
	    private readonly ThreadLocal<bool> _skip = new ThreadLocal<bool>(() => false);

	    /// <summary>
	    /// Indicates whether the converter can read JSON.
	    /// </summary>
	    public override bool CanRead => !_skip.Value || (_skip.Value = false);

	    /// <summary>
	    /// Indicates whether the converter can write JSON.
	    /// </summary>
	    public override bool CanWrite => !_skip.Value || (_skip.Value = false);

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
	    /// <summary>
	    /// Reads JSON and invokes deserialization with callback methods.
	    /// </summary>
	    public override ISerializationCallbackReceiver ReadJson(JsonReader reader, Type objectType,
		    ISerializationCallbackReceiver? existingValue, bool hasExistingValue, JsonSerializer serializer)
	    {
		    var jObject = serializer.Deserialize<JObject>(reader);

		    _skip.Value = true;

		    var result = jObject.ToObject(objectType, serializer) as ISerializationCallbackReceiver;
			result?.OnAfterDeserialize();
		    return result;
	    }

	    /// <summary>
	    /// Writes JSON and invokes serialization with callback methods.
	    /// </summary>
	    public override void WriteJson(JsonWriter writer, ISerializationCallbackReceiver? value,
		    JsonSerializer serializer)
	    {
		    if (value == null)
		    {
			    writer.WriteNull();
			    return;
		    }
		    value.OnBeforeSerialize();

		    var thisIndex = serializer.Converters.IndexOf(this);
		    serializer.Converters.RemoveAt(thisIndex);

		    serializer.Serialize(writer, value);

		    serializer.Converters.Insert(thisIndex, this);
	    }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

	    /// <summary>
	    /// Disposes of the thread-local skip flag.
	    /// </summary>
	    public void Dispose()
	    {
		    _skip.Dispose();
	    }
    }
    
    
    /// <summary>
    /// Custom JSON converter for <see cref="BeamAccountId"/> to serialize as long and deserialize it from string and long
    /// </summary>
    public class BeamAccountIdConverter : JsonConverter<BeamAccountId>
    {
        
	    public override BeamAccountId ReadJson(JsonReader reader, Type objectType, BeamAccountId existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.Integer:
			    {
				    var longValue = Convert.ToInt64(reader.Value);
				    return new BeamAccountId(longValue);
			    }
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamAccountId(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamAccountId();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamAccountId");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamAccountId value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsLong);
	    }

        
    }
    
    /// <summary>
    /// Custom JSON converter for <see cref="BeamCid"/> to serialize as long and deserialize it from string and long
    /// </summary>
    public class BeamCidConverter : JsonConverter<BeamCid>
    {
	    public override BeamCid ReadJson(JsonReader reader, Type objectType, BeamCid existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.Integer:
			    {
				    var longValue = Convert.ToInt64(reader.Value);
				    return new BeamCid(longValue);
			    }
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamCid(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamCid();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamCid value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsLong);
	    }
    }

    /// <summary>
    /// Custom JSON converter for <see cref="BeamContentId"/> to serialize and deserialize it from string
    /// </summary>
    public class BeamContentIdConverter : JsonConverter<BeamContentId>
    {
	    public override BeamContentId ReadJson(JsonReader reader, Type objectType, BeamContentId existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamContentId(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamContentId();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamContentId value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsString);
	    }
    }
    
    /// <summary>
    /// Custom JSON converter for <see cref="BeamContentManifestId"/> to serialize and deserialize it from string
    /// </summary>
    public class BeamContentManifestIdConverter : JsonConverter<BeamContentManifestId>
    {
	    public override BeamContentManifestId ReadJson(JsonReader reader, Type objectType, BeamContentManifestId existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamContentManifestId(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamContentManifestId();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamContentManifestId value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsString);
	    }
    }
    
    
    /// <summary>
    /// Custom JSON converter for <see cref="BeamGamerTag"/> to serialize as long and deserialize it from string and long
    /// </summary>
    public class BeamGamerTagConverter : JsonConverter<BeamGamerTag>
    {
	    public override BeamGamerTag ReadJson(JsonReader reader, Type objectType, BeamGamerTag existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.Integer:
			    {
				    var longValue = Convert.ToInt64(reader.Value);
				    return new BeamGamerTag(longValue);
			    }
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamGamerTag(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamGamerTag();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamGamerTag value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsLong);
	    }
    }
    
    /// <summary>
    /// Custom JSON converter for <see cref="BeamPid"/> to serialize and deserialize it from string
    /// </summary>
    public class BeamPidConverter : JsonConverter<BeamPid>
    {
	    public override BeamPid ReadJson(JsonReader reader, Type objectType, BeamPid existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamPid(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamPid();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamPid value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsString);
	    }
    }

    /// <summary>
    /// Custom JSON converter for <see cref="BeamStats"/> to serialize and deserialize it from string
    /// </summary>
    public class BeamStatsConverter : JsonConverter<BeamStats>
    {
	    public override BeamStats ReadJson(JsonReader reader, Type objectType, BeamStats existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new BeamStats(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new BeamStats();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, BeamStats value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.AsString);
	    }
    }
    
    /// <summary>
    /// Custom JSON converter for <see cref="ServiceName"/> to serialize and deserialize it from string
    /// </summary>
    public class ServiceNameConverter : JsonConverter<ServiceName>
    {
	    public override ServiceName ReadJson(JsonReader reader, Type objectType, ServiceName existingValue, bool hasExistingValue,
		    JsonSerializer serializer)
	    {
		    switch (reader.TokenType)
		    {
			    case JsonToken.String:
			    {
				    var stringValue = (string)reader.Value;
				    return new ServiceName(stringValue);
			    }
			    case JsonToken.Null:
			    {
				    return new ServiceName();
			    }
			    default:
				    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
		    }
	    }
        
	    public override void WriteJson(JsonWriter writer, ServiceName value, JsonSerializer serializer)
	    {
		    writer.WriteValue(value.Value);
	    }
    }

    /// <summary>
    /// Custom contract resolver for JSON serialization in Unity, allowing for proper handling of SerializeField attributes.
    /// </summary>
    public class UnityJsonContractResolver : DefaultContractResolver
    {
        private static UnityJsonContractResolver _instance;
        /// <summary>
        /// Singleton instance of the UnityJsonContractResolver.
        /// </summary>
        public static UnityJsonContractResolver Instance => _instance ??= new UnityJsonContractResolver();

        private UnityJsonContractResolver() { }

        /// <summary>
        /// Creates a list of JSON properties for the given type with proper handling of SerializeField attributes.
        /// </summary>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var baseProps = base.CreateProperties(type, memberSerialization);

            foreach (var prop in baseProps)
            {
	            prop.ShouldSerialize = instance =>
	            {
		            try
		            {
			            var value = prop.ValueProvider.GetValue(instance);
			            if (value is Optional optionalValue)
			            {
				            return optionalValue.HasValue;
			            }
		            }
		            catch (Exception ex)
		            {
			            Debug.LogWarning("Failed to understand parse predicate. " + ex.GetType().Name + " -- " + ex.Message + "\n" + ex.StackTrace);
		            }

		            return true;
	            };
            }
            
            bool HasSerializeField(JsonProperty p)
            {
                var serializeFieldAttrs = p.AttributeProvider.GetAttributes(typeof(SerializeField), false);
                return serializeFieldAttrs != null && serializeFieldAttrs.Count > 0;
            }

            foreach (var serializeJsonField in baseProps.Where(HasSerializeField))
            {
                serializeJsonField.Writable = true;
                serializeJsonField.Readable = true;
            }

            return baseProps;
        }

        /// <summary>
        /// Gets a list of serialized fields for a given type.
        /// </summary>
        public static List<FieldInfo> GetSerializedFields(Type objectType)
        {
	        IEnumerable<FieldInfo> GetAllFields(Type t)
	        {
		        if (t == null)
			        return Enumerable.Empty<FieldInfo>();

		        BindingFlags flags = BindingFlags.Public |
		                             BindingFlags.NonPublic |
		                             BindingFlags.Instance |
		                             BindingFlags.DeclaredOnly;
		        return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
	        }

	        var fields = GetAllFields(objectType);

	        bool IsPublicField(FieldInfo field) => field.IsPublic;
	        bool IsReadOnly(FieldInfo field) => field.IsInitOnly;
	        bool IsPublicAndWritable(FieldInfo field) => IsPublicField(field) && !IsReadOnly(field);
	        bool IsMarkedSerializeAttribute(FieldInfo field) => field.GetCustomAttribute<SerializeField>() != null;

	        bool ShouldIncludeField(FieldInfo field) => IsPublicAndWritable(field) || IsMarkedSerializeAttribute(field);

	        var validFields = fields
		        .Where(ShouldIncludeField)
		        .ToList();

	        return validFields.ToList();
        }
        
        /// <summary>
        /// Gets a list of serializable members for a given type.
        /// </summary>
        /// <param name="objectType">The type for which to retrieve serializable members.</param>
        /// <returns>A list of MemberInfo objects representing the serializable members.</returns>
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
	        return GetSerializedFields(objectType).Cast<MemberInfo>().ToList();
        }
    }
}
