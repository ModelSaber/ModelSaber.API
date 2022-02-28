using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModelSaber.API
{
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
