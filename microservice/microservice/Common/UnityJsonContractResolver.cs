using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Serialization.SmallerJSON;
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
            ContractResolver = UnityJsonContractResolver.Instance
        };
        private UnitySerializationSettings() { }
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

    public class UnitySerializationCallbackConverter : JsonConverter
    {
        public readonly JsonConverter BaseConverter;

        public UnitySerializationCallbackConverter(JsonConverter baseConverter)
        {
            BaseConverter = baseConverter;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is ISerializationCallbackReceiver receiver)
            {
                receiver.OnBeforeSerialize();
            }
            BaseConverter.WriteJson(writer, value, serializer);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = BaseConverter.ReadJson(reader, objectType, existingValue, serializer);
            if (result is ISerializationCallbackReceiver receiver)
            {
                receiver.OnAfterDeserialize();
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return BaseConverter.CanConvert(objectType);
        }
    }

    public class UnityJsonContractResolver : DefaultContractResolver
    {
        private static UnityJsonContractResolver _instance;
        public static UnityJsonContractResolver Instance => _instance ??= new UnityJsonContractResolver();

        private UnityJsonContractResolver() { }
        
        protected override JsonProperty CreateProperty( MemberInfo member, MemberSerialization memberSerialization )
        {
            var jsonProperty = base.CreateProperty( member, memberSerialization );

            if (typeof(ISerializationCallbackReceiver).IsAssignableFrom(jsonProperty.PropertyType))
            {
                jsonProperty.ValueProvider = new SerializationCallbackReceiverValueProvider(jsonProperty.ValueProvider);
            }

            return jsonProperty;
        }
        
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
            bool IsMarkedSerializeAttribute(FieldInfo field) => field.GetCustomAttribute<SerializeField>() != null;

            bool ShouldIncludeField(FieldInfo field) => IsPublicField(field) || IsMarkedSerializeAttribute(field);

            var validFields = fields
               .Where(ShouldIncludeField)
               .ToList();

            return validFields.Cast<MemberInfo>().ToList();
        }

        protected override JsonConverter? ResolveContractConverter(Type objectType)
        {
            var baseConverter = base.ResolveContractConverter(objectType);
            if (objectType.IsSubclassOf(typeof(ISerializationCallbackReceiver)))
            {
                return new UnitySerializationCallbackConverter(baseConverter);
            }
            return baseConverter;
        }

        public class SerializationCallbackReceiverValueProvider : IValueProvider
        {
            private IValueProvider _baseProvider;

            public SerializationCallbackReceiverValueProvider(IValueProvider baseProvider)
            {
                _baseProvider = baseProvider;
            }

            // SetValue gets called by Json.Net during deserialization.
            public void SetValue(object target, object value)
            {
                _baseProvider.SetValue(target, value);
            }

            // GetValue is called by Json.Net during serialization.
            public object GetValue(object target)
            {
                var value = _baseProvider.GetValue(target);
                
                ((ISerializationCallbackReceiver) value)?.OnBeforeSerialize();

                // temporary solution because i can't access to generic properties to serialize from SerializableDictionary<TKey, TValue> for request from inside of Microservice (like SendMail)
                var serializeDictMethod = value?.GetType().GetMethod("Serialize");
                
                if (serializeDictMethod != null)
                {
                    return serializeDictMethod.Invoke(value, null);
                }

                return value;
            }
        }
    }
}