using HirePathAI.Application.Interfaces;
using HirePathAI.Application.Services;
using HirePathAI.Infrastructure;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Infrastructure.Data;
using HirePathAI.Infrastructure.Identity;
using HirePathAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Registers SQL Server, Identity, JWT and authorization.
builder.Services.AddInfrastructure(
    builder.Configuration);

// Existing resume-processing services.
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

// Existing temporary stores.
builder.Services.AddSingleton<
    IAtsDashboardStore,
    InMemoryAtsDashboardStore>();

builder.Services.AddSingleton<
    IAtsAnalysisResultStore,
    InMemoryAtsAnalysisResultStore>();

builder.Services.AddSingleton<
    IJobStore,
    InMemoryJobStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Run migrations before seeding roles.
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

// Seed Admin, Recruiter, Candidate and HiringManager.
await IdentitySeeder.SeedRolesAsync(
    app.Services);

// Optional administrator seeding.
var adminEmail =
    builder.Configuration["SeedAdmin:Email"];

var adminPassword =
    builder.Configuration["SeedAdmin:Password"];

if (!string.IsNullOrWhiteSpace(adminEmail) &&
    !string.IsNullOrWhiteSpace(adminPassword))
{
    await IdentitySeeder.SeedAdminAsync(
        app.Services,
        adminEmail,
        adminPassword);
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization.
app.UseAuthentication();

app.UseAuthorization();

// Maps controllers that use attribute routes such as /api/auth.
app.MapControllers();

// Maps existing MVC/Razor routes.
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();