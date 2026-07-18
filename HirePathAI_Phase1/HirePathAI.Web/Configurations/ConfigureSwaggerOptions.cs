using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HirePathAI.Web.Configurations;

public class ConfigureSwaggerOptions
    : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider
        _apiVersionDescriptionProvider;

    public ConfigureSwaggerOptions(
        IApiVersionDescriptionProvider
            apiVersionDescriptionProvider)
    {
        _apiVersionDescriptionProvider =
            apiVersionDescriptionProvider;
    }

    public void Configure(
        SwaggerGenOptions options)
    {
        foreach (var description in
                 _apiVersionDescriptionProvider
                     .ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                CreateVersionInfo(description));
        }

        AddJwtSecurity(options);
    }

    private static OpenApiInfo CreateVersionInfo(
        ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "HirePathAI API",
            Version = description.ApiVersion.ToString(),
            Description =
                "AI-powered recruitment, resume screening, " +
                "candidate matching and talent management API.",
            Contact = new OpenApiContact
            {
                Name = "HirePathAI Development Team"
            }
        };

        if (description.IsDeprecated)
        {
            info.Description +=
                " This API version is deprecated.";
        }

        return info;
    }

    private static void AddJwtSecurity(
        SwaggerGenOptions options)
    {
        const string securitySchemeName = "Bearer";

        options.AddSecurityDefinition(
            securitySchemeName,
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Enter the JWT token returned by the login " +
                    "endpoint. Do not type the word Bearer."
            });

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = securitySchemeName
                        }
                    },
                    Array.Empty<string>()
                }
            });
    }
}