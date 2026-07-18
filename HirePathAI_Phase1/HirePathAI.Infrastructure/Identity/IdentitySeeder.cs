using HirePathAI.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HirePathAI.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider)
    {
        using var scope =
            serviceProvider.CreateScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<int>>>();

        var logger =
            scope.ServiceProvider
                .GetRequiredService<
                    ILoggerFactory>()
                .CreateLogger("IdentitySeeder");

        foreach (var roleName in UserRoles.All)
        {
            if (await roleManager.RoleExistsAsync(
                    roleName))
            {
                continue;
            }

            var result =
                await roleManager.CreateAsync(
                    new IdentityRole<int>
                    {
                        Name = roleName
                    });

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description));

                logger.LogError(
                    "Could not create role {Role}. Errors: {Errors}",
                    roleName,
                    errors);

                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': {errors}");
            }

            logger.LogInformation(
                "Created role {Role}.",
                roleName);
        }
    }
    public static async Task SeedAdminAsync(
    IServiceProvider serviceProvider,
    string email,
    string password)
    {
        using var scope =
            serviceProvider.CreateScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var admin =
            await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = "System Administrator",
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(
                        error => error.Description));

                throw new InvalidOperationException(
                    $"Could not create administrator: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                UserRoles.Admin))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    UserRoles.Admin);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description));

                throw new InvalidOperationException(
                    $"Could not assign Admin role: {errors}");
            }
        }
    }
}