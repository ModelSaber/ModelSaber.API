using System.Threading.Tasks;
using GraphQL.Validation;

namespace ModelSaber.API.GraphQL
{
    public class AuthValidationRule : IValidationRule
    {
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var userContext = context.UserContext;

            throw new System.NotImplementedException();
        }
    }
}
