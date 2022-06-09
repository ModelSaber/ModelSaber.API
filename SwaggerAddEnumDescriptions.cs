using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace ModelSaber.API
{
    public class SwaggerAddEnumDescriptions : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            bool checkOperationType(OperationType type)
            {
                return type is OperationType.Get or OperationType.Post or OperationType.Put;
            }
            // add enum descriptions to result models
            foreach (var (itemKey, property) in swaggerDoc.Components.Schemas)
            {
                IList<IOpenApiAny> propertyEnums = property.Enum;
                if (propertyEnums is { Count: > 0 })
                {
                    property.Description += DescribeEnum(itemKey, propertyEnums);
                }
            }

            // add enum descriptions to input parameters
            if (swaggerDoc.Paths.Count <= 0) return;
            foreach (var pathItem in swaggerDoc.Paths.Values)
            {
                DescribeEnumParameters(pathItem.Parameters);

                // head, patch, options, delete left out
                IDictionary<OperationType, OpenApiOperation> possibleParameterizedOperations = pathItem.Operations;
                possibleParameterizedOperations.Where(x => x.Value != null && checkOperationType(x.Key)).ToList().ForEach(x => DescribeEnumParameters(x.Value.Parameters));
            }
        }

        private void DescribeEnumParameters(IList<OpenApiParameter> parameters)
        {
            if (parameters == null) return;
            foreach (var param in parameters)
            {
                IList<IOpenApiAny> paramEnums = param.Schema.Enum;
                if (paramEnums is { Count: > 0 })
                {
                    param.Description += DescribeEnum(paramEnums);
                }
            }
        }

        private string DescribeEnum(IEnumerable<IOpenApiAny> enums)
        {
            return string.Join(", ", (from object enumOption in enums select $"{(int)enumOption} = {Enum.GetName(enumOption.GetType(), enumOption)}").ToArray());
        }

        private string DescribeEnum(string enumName, IEnumerable<IOpenApiAny> enums)
        {
            var enumType = GetEnumType("ModelSaber.Database.Models." + enumName);
            return enumType is not { IsEnum: true }
                ?
                ""
                :
                string.Join(", ",
                    (
                        from OpenApiPrimitive<int> enumOption in enums
                        select $"{enumOption.Value} = {Enum.GetName(enumType, enumOption.Value)}"
                    ).ToArray()
                );
        }

        private Type? GetEnumType(string enumName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => t.DefinedTypes.Any(f => f.FullName == enumName));
            return assembly?.DefinedTypes.FirstOrDefault(t => t.FullName == enumName)?.AsType();
        }
    }
}
