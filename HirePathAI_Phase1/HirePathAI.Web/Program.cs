using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HirePathAI.Application.Interfaces;
using HirePathAI.Application.Services;
using HirePathAI.Infrastructure;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Infrastructure.Data;
using HirePathAI.Infrastructure.Identity;
using HirePathAI.Infrastructure.Repositories;
using HirePathAI.Web.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder =
    WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// MVC AND API CONTROLLERS
// ---------------------------------------------------------

builder.Services.AddControllersWithViews();

// ---------------------------------------------------------
// API VERSIONING
// ---------------------------------------------------------

builder.Services
    .AddApiVersioning(options =>
    {
        // Default API version is version 1.0.
        options.DefaultApiVersion =
            new ApiVersion(1, 0);

        // Allows a request without a version to use v1.
        options.AssumeDefaultVersionWhenUnspecified =
            true;

        // Adds supported and deprecated API versions
        // to response headers.
        options.ReportApiVersions = true;

        // API version is taken from the URL.
        // Example: /api/v1/auth/login
        options.ApiVersionReader =
            new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        // Produces names such as v1, v2 and v1.1.
        options.GroupNameFormat = "'v'VVV";

        // Replaces {version:apiVersion}
        // with the actual version in Swagger.
        options.SubstituteApiVersionInUrl = true;
    });

// ---------------------------------------------------------
// SWAGGER
// ---------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<
    IConfigureOptions<SwaggerGenOptions>,
    ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<
        SwaggerDefaultValues>();

    // Displays enums in a readable format.
    options.UseInlineDefinitionsForEnums();

    // Prevents schema name conflicts when different
    // namespaces contain classes with the same name.
    options.CustomSchemaIds(type =>
        type.FullName?
            .Replace("+", ".")
        ?? type.Name);
});

// ---------------------------------------------------------
// DATABASE, IDENTITY AND JWT
// ---------------------------------------------------------

builder.Services.AddInfrastructure(
    builder.Configuration);

// ---------------------------------------------------------
// EXISTING AI RESUME SERVICES
// ---------------------------------------------------------

builder.Services.AddScoped<PdfService>();

builder.Services.AddScoped<ResumeService>();

builder.Services.AddScoped<
    IPdfExtractor,
    EnhancedPdfService>();

builder.Services.AddScoped<
    IResumeParser,
    ResumeParserService>();

builder.Services.AddScoped<
    IResumeScorer,
    ScoringService>();

// ---------------------------------------------------------
// EXISTING TEMPORARY IN-MEMORY STORES
// ---------------------------------------------------------

builder.Services.AddSingleton<
    IAtsDashboardStore,
    InMemoryAtsDashboardStore>();

builder.Services.AddSingleton<
    IAtsAnalysisResultStore,
    InMemoryAtsAnalysisResultStore>();

builder.Services.AddSingleton<
    IJobStore,
    InMemoryJobStore>();

// ---------------------------------------------------------
// BUILD APPLICATION
// ---------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------
// DATABASE MIGRATION
// ---------------------------------------------------------

using (var scope =
       app.Services.CreateScope())
{
    try
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        await dbContext.Database
            .MigrateAsync();
    }
    catch (Exception exception)
    {
        var logger =
            scope.ServiceProvider
                .GetRequiredService<
                    ILogger<Program>>();

        logger.LogError(
            exception,
            "An error occurred while migrating the database.");

        throw;
    }
}

// ---------------------------------------------------------
// SEED IDENTITY ROLES
// ---------------------------------------------------------

try
{
    await IdentitySeeder.SeedRolesAsync(
        app.Services);
}
catch (Exception exception)
{
    var logger =
        app.Services
            .GetRequiredService<
                ILogger<Program>>();

    logger.LogError(
        exception,
        "An error occurred while seeding Identity roles.");

    throw;
}

// ---------------------------------------------------------
// OPTIONAL ADMIN ACCOUNT SEEDING
// ---------------------------------------------------------

var adminEmail =
    builder.Configuration[
        "SeedAdmin:Email"];

var adminPassword =
    builder.Configuration[
        "SeedAdmin:Password"];

if (!string.IsNullOrWhiteSpace(adminEmail) &&
    !string.IsNullOrWhiteSpace(adminPassword))
{
    try
    {
        await IdentitySeeder.SeedAdminAsync(
            app.Services,
            adminEmail,
            adminPassword);
    }
    catch (Exception exception)
    {
        var logger =
            app.Services
                .GetRequiredService<
                    ILogger<Program>>();

        logger.LogError(
            exception,
            "An error occurred while seeding the administrator account.");

        throw;
    }
}

// ---------------------------------------------------------
// ERROR HANDLING
// ---------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

// ---------------------------------------------------------
// SWAGGER MIDDLEWARE
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        var apiVersionProvider =
            app.Services
                .GetRequiredService<
                    IApiVersionDescriptionProvider>();

        foreach (var description in
                 apiVersionProvider
                     .ApiVersionDescriptions
                     .Reverse())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"HirePathAI API " +
                $"{description.GroupName.ToUpperInvariant()}");
        }

        // Swagger address:
        // https://localhost:<port>/swagger
        options.RoutePrefix = "swagger";

        options.DocumentTitle =
            "HirePathAI API Documentation";

        options.DisplayRequestDuration();

        options.EnableFilter();

        options.EnableDeepLinking();

        // Hides the Schemas section at the bottom.
        options.DefaultModelsExpandDepth(-1);
    });
}

// ---------------------------------------------------------
// HTTP PIPELINE
// ---------------------------------------------------------

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must come before authorization.
app.UseAuthentication();

app.UseAuthorization();

// Maps attribute-routed API controllers.
app.MapControllers();

// Maps existing MVC controllers and views.
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();