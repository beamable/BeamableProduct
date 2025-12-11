using System;
using Newtonsoft.Json;

namespace Beamable.Common.Semantics.JsonConverters
{
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
                default:
                    throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing BeamAccountId");
            }
        }
        
        public override void WriteJson(JsonWriter writer, BeamAccountId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.AsLong);
        }

        
    }
}
