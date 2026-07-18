using System.Security.Claims;
using System.Text;
using HirePathAI.Application.Interfaces;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Infrastructure.Data;
using HirePathAI.Infrastructure.Identity;
using HirePathAI.Infrastructure.Repositories;
using HirePathAI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HirePathAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(
            services,
            configuration);

        AddIdentity(services);

        AddJwtAuthentication(
            services,
            configuration);

        AddInfrastructureServices(services);

        return services;
    }

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<ApplicationDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.MigrationsAssembly(
                            typeof(ApplicationDbContext)
                                .Assembly
                                .FullName);

                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
            });
    }

    private static void AddIdentity(
        IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;

                    options.User.RequireUniqueEmail = true;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);

                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedAccount = false;
                })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection =
            configuration.GetSection(
                JwtSettings.SectionName);

        services.Configure<JwtSettings>(
            jwtSection);

        var jwtSettings =
            jwtSection.Get<JwtSettings>();

        if (jwtSettings is null)
        {
            throw new InvalidOperationException(
                "JWT configuration was not found.");
        }

        if (string.IsNullOrWhiteSpace(
                jwtSettings.Key))
        {
            throw new InvalidOperationException(
                "JWT signing key was not configured.");
        }

        if (jwtSettings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(
                jwtSettings.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer was not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                jwtSettings.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience was not configured.");
        }

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtSettings.Key));

        services
            .AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
            .AddJwtBearer(
                options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer =
                                jwtSettings.Issuer,

                            ValidAudience =
                                jwtSettings.Audience,

                            IssuerSigningKey =
                                signingKey,

                            ClockSkew =
                                TimeSpan.Zero,

                            NameClaimType =
                                ClaimTypes.Name,

                            RoleClaimType =
                                ClaimTypes.Role
                        };

                    options.Events =
                        new JwtBearerEvents
                        {
                            OnAuthenticationFailed =
                                context =>
                                {
                                    if (context.Exception
                                        is SecurityTokenExpiredException)
                                    {
                                        context.Response
                                            .Headers["Token-Expired"] =
                                            "true";
                                    }

                                    return Task.CompletedTask;
                                },

                            OnChallenge =
                                context =>
                                {
                                    context.Response
                                        .Headers["Authentication-Failed"] =
                                            "true";

                                    return Task.CompletedTask;
                                },

                            OnForbidden =
                                context =>
                                {
                                    context.Response
                                        .Headers["Authorization-Failed"] =
                                            "true";

                                    return Task.CompletedTask;
                                }
                        };
                });

        services.AddAuthorization();
    }

    private static void AddInfrastructureServices(
        IServiceCollection services)
    {
        // JWT token creation service.
        services.AddScoped<
            IJwtTokenService,
            JwtTokenService>();

        // Existing PDF text extraction service.
        // This service does not use the removed trained ML model.
        services.AddScoped<
            IPdfExtractor,
            EnhancedPdfService>();

        // Temporary AI implementation.
        // Your teammate will later replace this registration
        // with the real public AI API implementation.
        services.AddScoped<
            IPublicAiResumeService,
            TemporaryPublicAiResumeService>();

        // New Phase 5 ATS service.
        services.AddScoped<
            IAtsService,
            AtsService>();

        // Keep the existing dashboard store so the current
        // HomeController and frontend dashboard continue working.
        services.AddSingleton<
            IAtsDashboardStore,
            InMemoryAtsDashboardStore>();
    }
}