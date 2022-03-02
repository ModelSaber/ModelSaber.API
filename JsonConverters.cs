using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ModelSaber.API
{
    public class JsonConverters
    {
        public static JsonConverter[] Converters = { new UlongJsonConverter(), new NullableUlongJsonConverter(), new TypeJsonConverter(), new IntPtrJsonConverter(), new IsTypeOfJsonConverter() };
        public static JsonSerializerOptions Default = BuildDefault();

        public static JsonSerializerOptions BuildDefault()
        {
            var ret = new JsonSerializerOptions();
            ret.Converters.AddRange(Converters);
            return ret;
        }
    }

    public class UlongJsonConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Convert.ToUInt64(reader.GetString());

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }

    public class NullableUlongJsonConverter : JsonConverter<ulong?>
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

    public class TypeJsonConverter : JsonConverter<Type>
    {
        public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class IntPtrJsonConverter : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class IsTypeOfJsonConverter : JsonConverter<Func<object, bool>>
    {
        public override Func<object, bool>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, Func<object, bool> value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}
