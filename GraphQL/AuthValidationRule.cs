using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation;
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
                new MatchingNodeVisitor<Operation>((operation, context) =>
                {
                    if (operation.OperationType != OperationType.Mutation)
                        return;

                    if (!context.IsAuthenticated())
                    {
                        context.ReportError(new ValidationError(context.Document.OriginalQuery!, "6.1.1", $"Authorization is required to access {operation.Name}", operation) { Code = "auth-required" });
                        return;
                    }

                    var dbToken = (OAuthToken?)context.UserContext["auth"];
                    if (dbToken == null || dbToken.IsExpired())
                        context.ReportError(new ValidationError(context.Document.OriginalQuery!, "6.1.1", $"Authorization is required to access {operation.Name}", operation) { Code = "auth-required" });
                }),
                new MatchingNodeVisitor<Field>((field, context) =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();
                    if (fieldDef == null) return;
                    if (fieldDef.RequirePermission().ToBool() && !context.IsAuthenticated())
                    {
                        context.ReportError(new ValidationError(context.Document.OriginalQuery!, "6.1.1", $"Authorization is required to access {field.Name}", field) { Code = "auth-required" });
                        return;
                    }
                    
                    var dbToken = (OAuthToken?)context.UserContext["auth"]; 
                    if (fieldDef.RequirePermission().ToBool() && (dbToken == null || dbToken.IsExpired() || !fieldDef.HasPermission(dbToken.GetScopes()).ToBool()))
                        context.ReportError(new ValidationError(context.Document.OriginalQuery!, "6.1.1", $"Authorization is required to access {field.Name}", field) { Code = "auth-required" });
                })
            );

            return Task.FromResult(nodeVisitor as INodeVisitor);
        }
    }
}
