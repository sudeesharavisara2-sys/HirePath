using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HirePathAI.Web.Configurations;

public class SwaggerDefaultValues
    : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |=
            apiDescription.IsDeprecated();

        foreach (var responseType in
                 context.ApiDescription
                     .SupportedResponseTypes)
        {
            var responseKey =
                responseType.IsDefaultResponse
                    ? "default"
                    : responseType.StatusCode.ToString();

            if (operation.Responses.ContainsKey(
                    responseKey))
            {
                continue;
            }

            operation.Responses.Add(
                responseKey,
                new OpenApiResponse
                {
                    Description =
                        responseType.ModelMetadata
                            ?.Description
                        ?? "Response"
                });
        }

        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in
                 operation.Parameters)
        {
            var description =
                apiDescription.ParameterDescriptions
                    .FirstOrDefault(
                        item =>
                            item.Name ==
                            parameter.Name);

            parameter.Description ??=
                description?.ModelMetadata
                    ?.Description;

            parameter.Required |=
                description?.IsRequired
                ?? false;
        }
    }
}