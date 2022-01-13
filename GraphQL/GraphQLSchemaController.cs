using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.GraphQL
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphQLSchemaController : ControllerBase
    {
        private readonly ISchema _schema;

        public GraphQLSchemaController(ISchema schema)
        {
            _schema = schema;
        }

        [HttpGet]
        public IActionResult GetSchema()
        {
            var printer = new SchemaPrinter(_schema, new SchemaPrinterOptions { IncludeDeprecationReasons = true, IncludeDescriptions = true });
            return Ok(printer.Print());
        }
    }
}
