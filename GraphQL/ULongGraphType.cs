using System;
using GraphQL.Types;

namespace ModelSaber.API.GraphQL
{
    public class ULongGraphType : ScalarGraphType
    {
        public ULongGraphType()
        {
            Name = "UInt64";
            Description = "Stringed representation of ulong due to javascript cant handle 64 bit large integers without derping";
        }

        public override object? Serialize(object? value)
        {
            return value?.ToString();
        }

        public override object? ParseValue(object? value)
        {
            if (value == null) return null;
            try
            {
                return Convert.ToUInt64(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
