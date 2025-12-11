using System;
using Newtonsoft.Json;

namespace Beamable.Common.Semantics.JsonConverters
{
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
                default:
                    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamCid");
            }
        }
        
        public override void WriteJson(JsonWriter writer, BeamCid value, JsonSerializer serializer)
        {
            writer.WriteValue(value.AsLong);
        }
    }
}
