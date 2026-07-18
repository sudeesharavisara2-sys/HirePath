using HirePathAI.Application.Interfaces;
using HirePathAI.Application.Services;
using HirePathAI.Infrastructure;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Infrastructure.Data;
using HirePathAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SQL Server and Entity Framework Core
builder.Services.AddInfrastructure(builder.Configuration);

// Existing resume-processing services
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ResumeService>();

builder.Services.AddScoped<IPdfExtractor, EnhancedPdfService>();
builder.Services.AddScoped<IResumeParser, ResumeParserService>();
builder.Services.AddScoped<IResumeScorer, ScoringService>();

// Existing temporary stores.
// These will be replaced with database repositories later.
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

// Apply pending migrations automatically during development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();