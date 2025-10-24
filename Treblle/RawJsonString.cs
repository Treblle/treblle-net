using Newtonsoft.Json;
using System;

namespace Treblle.Net
{
    /// <summary>
    /// Wrapper for raw JSON strings to avoid double deserialization
    /// Serializes as the original JSON without quotes
    /// </summary>
    [JsonConverter(typeof(RawJsonStringConverter))]
    public class RawJsonString
    {
        public string Json { get; }

        public RawJsonString(string json)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));
        }
    }

    /// <summary>
    /// Custom JSON converter that outputs raw JSON without escaping
    /// </summary>
    public class RawJsonStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RawJsonString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json = reader.Value?.ToString();
            return json != null ? new RawJsonString(json) : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var rawJson = value as RawJsonString;
            if (rawJson != null)
            {
                // Write raw JSON directly without escaping
                writer.WriteRawValue(rawJson.Json);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
