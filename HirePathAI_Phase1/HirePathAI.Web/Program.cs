using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HirePathAI.Application.Interfaces;
using HirePathAI.Infrastructure;
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
        options.DefaultApiVersion =
            new ApiVersion(1, 0);

        options.AssumeDefaultVersionWhenUnspecified =
            true;

        options.ReportApiVersions = true;

        options.ApiVersionReader =
            new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";

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

    options.UseInlineDefinitionsForEnums();

    options.CustomSchemaIds(type =>
        type.FullName?
            .Replace("+", ".")
        ?? type.Name);
});

// ---------------------------------------------------------
// DATABASE, IDENTITY, JWT AND ATS SERVICES
// ---------------------------------------------------------

builder.Services.AddInfrastructure(
    builder.Configuration);

// ---------------------------------------------------------
// EXISTING JOB STORE
// ---------------------------------------------------------
// Your current JobController still depends on IJobStore.
// Therefore, keep this registration until JobController
// is converted to use ApplicationDbContext.

builder.Services.AddSingleton<
    IJobStore,
    InMemoryJobStore>();

// ---------------------------------------------------------
// BUILD APPLICATION
// ---------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------
// APPLY DATABASE MIGRATIONS
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
            "An error occurred while applying database migrations.");

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
// SWAGGER
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
                description.GroupName
                    .ToUpperInvariant());
        }

        options.RoutePrefix = "swagger";

        options.DocumentTitle =
            "HirePathAI API Documentation";

        options.DisplayRequestDuration();

        options.EnableFilter();

        options.EnableDeepLinking();

        options.DefaultModelsExpandDepth(-1);
    });
}

// ---------------------------------------------------------
// HTTP PIPELINE
// ---------------------------------------------------------

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must be before authorization.
app.UseAuthentication();

app.UseAuthorization();

// Maps versioned API controllers.
app.MapControllers();

// Maps normal MVC controllers and views.
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();