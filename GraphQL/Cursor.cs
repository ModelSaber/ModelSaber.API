#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable PossibleMultipleEnumeration

namespace ModelSaber.API.GraphQL
{
    public static class Cursor
    {
        public static T? FromCursor<T>(string? cursor) where T : struct
        {
            if (string.IsNullOrEmpty(cursor))
            {
                return null;
            }

            string decodedValue;
            try
            {
                decodedValue = Base64Decode(cursor);
            }
            catch (FormatException)
            {
                return default;
            }
            
            var type = typeof(T);
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(DateTimeOffset))
            {
                return (T)(object)DateTimeOffset.ParseExact(decodedValue, "s", CultureInfo.InvariantCulture);
            }

            if (type == typeof(Guid))
            {
                return (T)(object)new Guid(WebEncoders.Base64UrlDecode(cursor));
            }

            return (T)Convert.ChangeType(decodedValue, type, CultureInfo.InvariantCulture);
        }

        public static (string? FirstCursor, string? LastCursor) GetFirstAndLastCursor<TItem, TCursor>(
            IEnumerable<TItem> enumerable,
            Func<TItem, TCursor> getCursorProperty)
        {
            if (getCursorProperty is null)
            {
                throw new ArgumentNullException(nameof(getCursorProperty));
            }

            if (enumerable is null || !enumerable.Any())
            {
                return (null, null);
            }

            var firstCursor = ToCursor(getCursorProperty(enumerable.First()));
            var lastCursor = ToCursor(getCursorProperty(enumerable.Last()));

            return (firstCursor, lastCursor);
        }

        public static string ToCursor<T>(T value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return Base64Encode(dateTimeOffset.ToUniversalTime().ToString("s", CultureInfo.InvariantCulture));
            }

            if (value is Guid guid)
            {
                return WebEncoders.Base64UrlEncode(guid.ToByteArray());
            }

            return Base64Encode(value.ToString()!);
        }

        private static string Base64Decode(string value) => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));

        private static string Base64Encode(string value) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value));
    }
}
