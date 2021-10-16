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
    public class LongConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value.ToString() ?? string.Empty);
            t.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (objectType)
            {
                case Type t when t == typeof(ulong):
                    return Convert.ToUInt64(reader.Value);
                case Type t when t == typeof(long):
                    return Convert.ToInt64(reader.Value);
                default:
                    return 0;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ulong) || objectType == typeof(long);
        }
    }
}
