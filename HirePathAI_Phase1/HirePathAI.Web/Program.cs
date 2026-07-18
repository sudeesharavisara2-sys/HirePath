using HirePathAI.Infrastructure.Repositories;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Application.Services;
using HirePathAI.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Register existing services (backward compatible)
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ResumeService>();

// Register new enhanced services
builder.Services.AddScoped<IPdfExtractor, EnhancedPdfService>();
builder.Services.AddScoped<IResumeParser, ResumeParserService>();
builder.Services.AddScoped<IResumeScorer, ScoringService>();
builder.Services.AddSingleton<IAtsDashboardStore, InMemoryAtsDashboardStore>();
builder.Services.AddSingleton<IAtsAnalysisResultStore, InMemoryAtsAnalysisResultStore>();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
