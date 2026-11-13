using System;
using Beamable.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Beamable.Server.Common
{
	// Registered only for the federation DTOs migrated to generated API models.
	internal sealed class GeneratedFederationModelConverter<T> : JsonConverter<T>
		where T : class, JsonSerializable.ISerializable, new()
	{
		public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			JObject.Parse(JsonSerializable.ToJson(value)).WriteTo(writer);
		}

		public override T ReadJson(JsonReader reader, Type objectType, T existingValue,
			bool hasExistingValue, JsonSerializer serializer)
		{
			return reader.TokenType == JsonToken.Null
				? null
				: JsonSerializable.FromJson<T>(JObject.Load(reader).ToString(Formatting.None), true);
		}
	}
}
