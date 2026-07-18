using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HirePathAI.Infrastructure;
using HirePathAI.Infrastructure.Data;
using HirePathAI.Infrastructure.Identity;
using HirePathAI.Application.Interfaces;
using HirePathAI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// MVC CONTROLLERS AND VIEWS
// =========================================================

builder.Services.AddControllersWithViews();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.DictionaryKeyPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });


// =========================================================
// INFRASTRUCTURE
// Database, Identity, JWT and ATS services
// =========================================================

builder.Services.AddInfrastructure(
    builder.Configuration);


// =========================================================
// EXISTING FRONTEND COMPATIBILITY
// =========================================================

// Keep this registration only if HomeController or the existing
// frontend dashboard still uses IAtsDashboardStore.
//
// If it is already registered inside DependencyInjection.cs,
// this registration is not required and may be removed.

builder.Services.AddSingleton<
    IAtsDashboardStore,
    InMemoryAtsDashboardStore>();


// =========================================================
// API VERSIONING
// =========================================================

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion =
            new ApiVersion(1, 0);

        options.AssumeDefaultVersionWhenUnspecified =
            true;

        options.ReportApiVersions =
            true;

        options.ApiVersionReader =
            ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader(
                    "X-Api-Version"),
                new QueryStringApiVersionReader(
                    "api-version"));
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat =
            "'v'VVV";

        options.SubstituteApiVersionInUrl =
            true;
    });


// =========================================================
// SWAGGER
// =========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "HirePathAI API",
            Version = "v1",
            Description =
                "HirePathAI recruitment and ATS API documentation.",
            Contact = new OpenApiContact
            {
                Name = "HirePathAI Development Team"
            }
        });

    /*
     * Prevents Swagger errors when two classes in different
     * namespaces have the same class name.
     */
    options.CustomSchemaIds(
        type => type.FullName?.Replace("+", "."));

    /*
     * Temporary protection against duplicate controller routes.
     *
     * Swagger will use the first matching action when duplicate
     * HTTP method and route combinations are found.
     *
     * You should still correct duplicate routes in controllers.
     */
    options.ResolveConflictingActions(
        apiDescriptions =>
            apiDescriptions.First());

    // JWT Bearer authentication button in Swagger.
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description =
                "Enter the JWT token. Example: Bearer eyJhbGciOi...",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });

    // Include XML comments when the XML documentation file exists.
    var xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(
            AppContext.BaseDirectory,
            xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


// =========================================================
// CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "HirePathAICorsPolicy",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// APPLY DATABASE MIGRATIONS AND SEED ROLES
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var serviceProvider =
        scope.ServiceProvider;

    try
    {
        var dbContext =
            serviceProvider.GetRequiredService<
                ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();

        var roleManager =
            serviceProvider.GetRequiredService<
                RoleManager<IdentityRole<int>>>();

        var userManager =
            serviceProvider.GetRequiredService<
                UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);

        await SeedAdministratorAsync(
            userManager,
            roleManager,
            app.Configuration);
    }
    catch (Exception exception)
    {
        var logger =
            serviceProvider.GetRequiredService<
                ILogger<Program>>();

        logger.LogError(
            exception,
            "An error occurred while migrating or seeding the database.");
    }
}


// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "HirePathAI API V1");

        options.DocumentTitle =
            "HirePathAI API Documentation";

        options.DisplayRequestDuration();

        options.EnableDeepLinking();

        options.EnableFilter();

        options.ShowExtensions();
    });
}
else
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors(
    "HirePathAICorsPolicy");

app.UseAuthentication();

app.UseAuthorization();


// =========================================================
// ENDPOINT MAPPING
// =========================================================

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();


// =========================================================
// ROLE SEEDING
// =========================================================

static async Task SeedRolesAsync(
    RoleManager<IdentityRole<int>> roleManager)
{
    string[] roles =
    {
        "Admin",
        "Recruiter",
        "Candidate",
        "HiringManager"
    };

    foreach (var roleName in roles)
    {
        var roleExists =
            await roleManager.RoleExistsAsync(
                roleName);

        if (!roleExists)
        {
            var result =
                await roleManager.CreateAsync(
                    new IdentityRole<int>
                    {
                        Name = roleName,
                        NormalizedName =
                            roleName.ToUpperInvariant()
                    });

            if (!result.Succeeded)
            {
                var errors =
                    string.Join(
                        ", ",
                        result.Errors.Select(
                            error =>
                                error.Description));

                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': {errors}");
            }
        }
    }
}


// =========================================================
// ADMIN USER SEEDING
// =========================================================

static async Task SeedAdministratorAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IConfiguration configuration)
{
    const string adminRole =
        "Admin";

    var adminEmail =
        configuration[
            "AdminUser:Email"]
        ?? "admin@hirepathai.com";

    var adminPassword =
        configuration[
            "AdminUser:Password"]
        ?? "Admin@12345";

    var adminFullName =
        configuration[
            "AdminUser:FullName"]
        ?? "HirePathAI Administrator";

    var existingAdmin =
        await userManager.FindByEmailAsync(
            adminEmail);

    if (existingAdmin is null)
    {
        var adminUser =
            new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminFullName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                LockoutEnabled = true
            };

        var createResult =
            await userManager.CreateAsync(
                adminUser,
                adminPassword);

        if (!createResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    createResult.Errors.Select(
                        error =>
                            error.Description));

            throw new InvalidOperationException(
                $"Unable to create administrator: {errors}");
        }

        existingAdmin = adminUser;
    }

    var adminRoleExists =
        await roleManager.RoleExistsAsync(
            adminRole);

    if (!adminRoleExists)
    {
        var roleResult =
            await roleManager.CreateAsync(
                new IdentityRole<int>
                {
                    Name = adminRole,
                    NormalizedName =
                        adminRole.ToUpperInvariant()
                });

        if (!roleResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    roleResult.Errors.Select(
                        error =>
                            error.Description));

            throw new InvalidOperationException(
                $"Unable to create administrator role: {errors}");
        }
    }

    var userRoles =
        await userManager.GetRolesAsync(
            existingAdmin);

    if (!userRoles.Contains(
            adminRole,
            StringComparer.OrdinalIgnoreCase))
    {
        var addRoleResult =
            await userManager.AddToRoleAsync(
                existingAdmin,
                adminRole);

        if (!addRoleResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    addRoleResult.Errors.Select(
                        error =>
                            error.Description));

            throw new InvalidOperationException(
                $"Unable to assign administrator role: {errors}");
        }
    }
}