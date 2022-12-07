using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Beamable.Server.Common
{
    public class UnitySerializationSettings : JsonSerializerSettings
    {
        private static UnitySerializationSettings _instance;
        public static UnitySerializationSettings Instance => _instance ??= new UnitySerializationSettings
        {
			Converters = new List<JsonConverter>
			{
				new StringToSomethingDictionaryConverter<string>(),
				new StringToSomethingDictionaryConverter<int>(),
				new StringToSomethingDictionaryConverter<long>(),
				new StringToSomethingDictionaryConverter<CurrencyPropertyList>(),
				new StringToSomethingDictionaryConverter<ItemViewList>(),
				// THIS MUST BE LAST, because it is hacky, and falls back onto other converts as its _normal_ behaviour. If its not last, then other converts can run twice, which causes newtonsoft to explode.
				new UnitySerializationCallbackInvoker(),

			},
            ContractResolver = UnityJsonContractResolver.Instance
        };

        private UnitySerializationSettings()
        {

        }
    }

    public class JsonUtilityConverter : JsonUtility.IConverter
    {
        public static void Init()
        {
            JsonUtility.Converter = new JsonUtilityConverter();
        }

        private JsonUtilityConverter() { }

        public string ToJson(object data) => JsonConvert.SerializeObject(data, UnitySerializationSettings.Instance);

        public T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, UnitySerializationSettings.Instance);
    }

    public class StringToSomethingDictionaryConverter<T> : JsonConverter<SerializableDictionaryStringToSomething<T>>
    {
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
			    var keysAndValues = serializer.Deserialize<KeysAndValues>(reader);
			    if (keysAndValues.keys.Length != keysAndValues.values.Length)
			    {
				    BeamableLogger.LogWarning($"Deserializing dictionary but keys and values were different values. keyCount=[{keysAndValues.keys.Length}] valueCount=[{keysAndValues.values.Length}]");
			    }
			    for (var i = 0; i < keysAndValues.keys.Length && i < keysAndValues.values.Length; i++)
			    {
				    instance[keysAndValues.keys[i]] = keysAndValues.values[i];
			    }

			    return instance;
		    }

		    return reader.TokenType == JsonToken.StartArray
			    ? HandleArrayVariant()
			    : HandleObjectVariant();
	    }

	    public class KeyValue
	    {
		    public string name;
		    public T value;
	    }

	    public class KeysAndValues
	    {
		    public string[] keys;
		    public T[] values;
	    }
    }


    /// <summary>
    /// Call the ISerializationCallbackReceiver methods, and then fallback to the existing converter...
    ///  adapted from https://github.com/JamesNK/Newtonsoft.Json/issues/719
    /// </summary>
    public class UnitySerializationCallbackInvoker : JsonConverter<ISerializationCallbackReceiver>, IDisposable
    {
	    private readonly ThreadLocal<bool> _skip = new ThreadLocal<bool>(() => false);

	    public override bool CanRead => !_skip.Value || (_skip.Value = false);

	    public override bool CanWrite => !_skip.Value || (_skip.Value = false);

	    public override ISerializationCallbackReceiver ReadJson(JsonReader reader, Type objectType,
		    ISerializationCallbackReceiver? existingValue, bool hasExistingValue, JsonSerializer serializer)
	    {
		    var jObject = serializer.Deserialize<JObject>(reader);

		    _skip.Value = true;

		    var result = jObject.ToObject(objectType, serializer) as ISerializationCallbackReceiver;
			result?.OnAfterDeserialize();
		    return result;
	    }

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

	    public void Dispose()
	    {
		    _skip.Dispose();
	    }
    }

    public class UnityJsonContractResolver : DefaultContractResolver
    {
        private static UnityJsonContractResolver _instance;
        public static UnityJsonContractResolver Instance => _instance ??= new UnityJsonContractResolver();

        private UnityJsonContractResolver() { }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var baseProps = base.CreateProperties(type, memberSerialization);

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

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
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

            return validFields.Cast<MemberInfo>().ToList();
        }
    }
}
