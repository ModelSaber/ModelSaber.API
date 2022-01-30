using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.GraphQL
{
    public static class AuthGraphQLExtensions
    {
        public static readonly string PermissionsKey = "Auth";

        public static bool? RequirePermission(this IProvideMetadata type) => type.GetMetadata<IEnumerable<string>>(PermissionsKey)?.Any();

        public static void RequiresPermission(this IProvideMetadata type, string permission) => 
            type.Metadata[PermissionsKey] = (type.GetMetadata<IEnumerable<string>>(PermissionsKey) ?? Array.Empty<string>()).Concat(new[] { permission });

        public static bool? HasPermission(this IProvideMetadata type, string permission) => type.GetMetadata<IEnumerable<string>>(PermissionsKey)?.Contains(permission);

        public static bool? HasPermission(this IProvideMetadata type, IEnumerable<string> permissions) => permissions.Any(t => type.HasPermission(t).ToBool());

        public static bool IsAuthenticated(this ValidationContext context) => context.UserContext.ContainsKey("auth") && !string.IsNullOrWhiteSpace(context.UserContext["auth"]?.ToString());
        
        public static FieldBuilder<TS, TR> RequirePermission<TS, TR>(this FieldBuilder<TS, TR> builder, string permission = "public")
        {
            builder.FieldType.RequiresPermission(permission);
            return builder;
        }
    }
}
