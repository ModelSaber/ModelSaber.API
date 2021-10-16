using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ModelSaber.API
{
    public class SwaggerAddEnumDescriptions : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            bool checkOperationType(OperationType type)
            {
                return type == OperationType.Get || type == OperationType.Post || type == OperationType.Put;
            }
            // add enum descriptions to result models
            foreach ((string itemKey, OpenApiSchema property) in swaggerDoc.Components.Schemas)
            {
                IList<IOpenApiAny> propertyEnums = property.Enum;
                if (propertyEnums != null && propertyEnums.Count > 0)
                {
                    property.Description += DescribeEnum(itemKey, propertyEnums);
                }
            }

            // add enum descriptions to input parameters
            if (swaggerDoc.Paths.Count > 0)
            {
                foreach (OpenApiPathItem pathItem in swaggerDoc.Paths.Values)
                {
                    DescribeEnumParameters(pathItem.Parameters);

                    // head, patch, options, delete left out
                    IDictionary<OperationType, OpenApiOperation> possibleParameterisedOperations = pathItem.Operations;
                    possibleParameterisedOperations.Where(x => x.Value != null && checkOperationType(x.Key)).ToList().ForEach(x => DescribeEnumParameters(x.Value.Parameters));
                }
            }
        }

        private void DescribeEnumParameters(IList<OpenApiParameter> parameters)
        {
            if (parameters != null)
            {
                foreach (OpenApiParameter param in parameters)
                {
                    IList<IOpenApiAny> paramEnums = param.Schema.Enum;
                    if (paramEnums != null && paramEnums.Count > 0)
                    {
                        param.Description += DescribeEnum(paramEnums);
                    }
                }
            }
        }

        private string DescribeEnum(IList<IOpenApiAny> enums)
        {
            List<string> enumDescriptions = new List<string>();
            foreach (object enumOption in enums)
            {
                enumDescriptions.Add(string.Format("{0} = {1}", (int)enumOption, Enum.GetName(enumOption.GetType(), enumOption)));
            }
            return string.Join(", ", enumDescriptions.ToArray());
        }

        private string DescribeEnum(string enumName, IList<IOpenApiAny> enums)
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

        private Type GetEnumType(string enumName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = assemblies.FirstOrDefault(t => t.DefinedTypes.Any(f => f.FullName == enumName));
            if (assembly is not null)
                return assembly.DefinedTypes.FirstOrDefault(t => t.FullName == enumName)?.AsType();
            return null;
        }
    }
}
