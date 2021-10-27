#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelSaber.API
{
    public static class Cursor
    {
        public static DateTime? FromCursorDateTime(string? cursor) => !string.IsNullOrWhiteSpace(cursor) ? DateTime.FromBinary(BitConverter.ToInt64(Convert.FromBase64String(cursor))) : null;

        public static Guid? FromCursorGuid(string? cursor) => !string.IsNullOrWhiteSpace(cursor) ? new Guid(Convert.FromBase64String(cursor)) : null;

        public static string ToCursor(DateTime? dt) => (dt.HasValue ? Convert.ToBase64String(BitConverter.GetBytes(dt.Value.ToBinary())) : null)!;

        public static string ToCursor(Guid? guid) => (guid.HasValue ? Convert.ToBase64String(guid.Value.ToByteArray()) : null)!;

        public static (string, string) GetFirstAndLastCursor<T>(List<T> obj, Func<T?, DateTime?> action)
        {
            return (ToCursor(action(obj.FirstOrDefault())), ToCursor(action(obj.FirstOrDefault())));
        }
    }
}
