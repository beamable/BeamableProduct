using System;
using Newtonsoft.Json;

namespace Beamable.Common.Semantics.JsonConverters
{
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
                default:
                    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamGamerTag");
            }
        }
        
        public override void WriteJson(JsonWriter writer, BeamGamerTag value, JsonSerializer serializer)
        {
            writer.WriteValue(value.AsLong);
        }
    }
}
