using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.GraphQL
{
    public class AuthValidationRule : IValidationRule
    {
        private readonly IServiceProvider _provider;

        public AuthValidationRule(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task<INodeVisitor> ValidateAsync(ValidationContext _)
        {
            var nodeVisitor = new NodeVisitors(
                new MatchingNodeVisitor<GraphQLOperationDefinition>((operation, context) =>
                {
                    if (operation.Operation != OperationType.Mutation)
                        return;

                    if (!context.IsAuthenticated())
                    {
                        context.ReportError(new ValidationError(context.Document.Source, "6.1.1", $"Authorization is required to access {operation.Name}", operation) { Code = "auth-required" });
                        return;
                    }
                    
                    if (!CheckAuth(context.UserContext["auth"], dbToken => dbToken.IsExpired()))
                        context.ReportError(new ValidationError(context.Document.Source, "6.1.1", $"Authorization is required to access {operation.Name}", operation) { Code = "auth-required" });
                }),
                new MatchingNodeVisitor<GraphQLFieldDefinition>((field, context) =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();
                    if (fieldDef == null) return;
                    if (fieldDef.RequirePermission().ToBool() && !context.IsAuthenticated())
                    {
                        context.ReportError(new ValidationError(context.Document.Source, "6.1.1", $"Authorization is required to access {field.Name}", field) { Code = "auth-required" });
                        return;
                    }
                    
                    if (fieldDef.RequirePermission().ToBool() && !CheckAuth(context.UserContext["auth"], dbToken => dbToken.IsExpired() || fieldDef.HasPermission(dbToken.GetScopes()).ToBool()))
                        context.ReportError(new ValidationError(context.Document.Source, "6.1.1", $"Authorization is required to access {field.Name}", field) { Code = "auth-required" });
                })
            );

            return Task.FromResult(nodeVisitor as INodeVisitor);
        }

        internal static bool CheckAuth(object? context, Func<OAuthToken,bool> checkFunc) =>
            context != null && context switch
            {
                OAuthToken token => checkFunc(token),
                UserLogons _ => true,
                _ => false
            };

        ValueTask<INodeVisitor?> IValidationRule.ValidateAsync(ValidationContext context)
        {
            return new ValueTask<INodeVisitor?>(ValidateAsync(context)!);
        }
    }
}
