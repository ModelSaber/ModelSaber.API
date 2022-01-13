using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace ModelSaber.API.GraphQL
{
    public class ULongGraphType : ScalarGraphType
    {
        public ULongGraphType()
        {
            Name = "UInt64";
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

        public override object? ParseLiteral(IValue value) =>
            value switch
            {
                NullValue => null,
                StringValue stringValue => ParseValue(stringValue.Value),
                _ => ThrowLiteralConversionError(value)
            };
    }
}
