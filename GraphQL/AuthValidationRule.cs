using System;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;

namespace ModelSaber.API.GraphQL
{
    public class AuthValidationRule : IValidationRule
    {
        private readonly IServiceProvider _provider;

        public AuthValidationRule(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var nodeVisitor = new NodeVisitors(
                new MatchingNodeVisitor<Operation>((astType, context) =>
                {
                    if (astType.OperationType != OperationType.Mutation)
                        return;

                    if (!context.UserContext.ContainsKey("auth") && !string.IsNullOrWhiteSpace(context.UserContext["auth"]?.ToString()))
                        return;

                    var type = context.TypeInfo.GetLastType();
                    var auth = context.UserContext["auth"];
                })
            );

            return Task.FromResult(nodeVisitor as INodeVisitor);
        }
    }
}
