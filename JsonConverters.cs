using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ModelSaber.API
{
    [Obsolete("Replace with System.Text.Json")]
    public class JsonNetLongConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var t = JToken.FromObject(value?.ToString() ?? string.Empty);
            t.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return objectType switch
            {
                { } t when t == typeof(ulong) => Convert.ToUInt64(reader.Value),
                { } t when t == typeof(long) => Convert.ToInt64(reader.Value),
                _ => 0,
            };
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ulong) || objectType == typeof(long);
        }
    }

    public class JsonConverters
    {
        public static System.Text.Json.Serialization.JsonConverter[] Converters = { new UlongJsonConverter(), new NullableUlongJsonConverter() };
    }

    public class UlongJsonConverter : System.Text.Json.Serialization.JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Convert.ToUInt64(reader.GetString());

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }

    public class NullableUlongJsonConverter : System.Text.Json.Serialization.JsonConverter<ulong?>
    {
        public override ulong? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return string.IsNullOrWhiteSpace(str) ? null : Convert.ToUInt64(str);
        }

        public override void Write(Utf8JsonWriter writer, ulong? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.ToString());
            else
                writer.WriteNullValue();
        }
    }
}
