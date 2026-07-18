using HirePathAI.Domain.Entities;
using HirePathAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HirePathAI.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<Candidate> Candidates => Set<Candidate>();

    public DbSet<JobApplication> JobApplications =>
        Set<JobApplication>();

    public DbSet<ResumeAnalysis> ResumeAnalyses =>
        Set<ResumeAnalysis>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureJob(modelBuilder);

        ConfigureCandidate(modelBuilder);

        ConfigureJobApplication(modelBuilder);

        ConfigureResumeAnalysis(modelBuilder);
    }

    private static void ConfigureJob(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Jobs");

            entity.HasKey(job => job.Id);

            entity.Property(job => job.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(job => job.CompanyName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(job => job.Description)
                .HasMaxLength(5000)
                .IsRequired();

            entity.Property(job => job.Location)
                .HasMaxLength(250);

            entity.Property(job => job.RequiredSkills)
                .HasMaxLength(2000);

            entity.Property(job => job.PreferredSkills)
                .HasMaxLength(2000);

            entity.Property(job => job.MinimumEducation)
                .HasMaxLength(250);

            entity.Property(job => job.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private static void ConfigureCandidate(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.ToTable("Candidates");

            entity.HasKey(candidate => candidate.Id);

            entity.Property(candidate => candidate.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(candidate => candidate.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(candidate => candidate.Email)
                .IsUnique();

            entity.Property(candidate => candidate.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(candidate => candidate.Address)
                .HasMaxLength(500);

            entity.Property(candidate => candidate.CurrentPosition)
                .HasMaxLength(150);

            entity.Property(candidate => candidate.Skills)
                .HasMaxLength(3000);

            entity.Property(candidate => candidate.Education)
                .HasMaxLength(2000);

            entity.Property(candidate => candidate.ResumeFilePath)
                .HasMaxLength(1000);

            entity.Property(candidate => candidate.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private static void ConfigureJobApplication(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("JobApplications");

            entity.HasKey(application => application.Id);

            entity.Property(application => application.CoverLetter)
                .HasMaxLength(5000);

            entity.Property(application => application.AiRecommendation)
                .HasMaxLength(500);

            entity.Property(application => application.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasOne(application => application.Candidate)
                .WithMany(candidate => candidate.Applications)
                .HasForeignKey(application => application.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(application => application.Job)
                .WithMany(job => job.Applications)
                .HasForeignKey(application => application.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(application => new
            {
                application.CandidateId,
                application.JobId
            }).IsUnique();
        });
    }

    private static void ConfigureResumeAnalysis(
     ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResumeAnalysis>(entity =>
        {
            entity.ToTable("ResumeAnalyses");

            entity.HasKey(analysis => analysis.Id);

            entity.Property(analysis =>
                    analysis.CandidateName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(analysis =>
                    analysis.CandidateEmail)
                .HasMaxLength(256);

            entity.Property(analysis =>
                    analysis.CandidatePhone)
                .HasMaxLength(30);

            entity.Property(analysis =>
                    analysis.ResumeFileName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(analysis =>
                    analysis.ResumeFilePath)
                .HasMaxLength(1000);

            entity.Property(analysis =>
                    analysis.ExtractedSkills)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.MatchedSkills)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.MissingSkills)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.Education)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.Experience)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.Certifications)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.Recommendation)
                .HasMaxLength(500);

            entity.Property(analysis =>
                    analysis.Summary)
                .HasColumnType("nvarchar(max)");

            entity.Property(analysis =>
                    analysis.ProcessedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(analysis =>
                    analysis.Job)
                .WithMany(job =>
                    job.ResumeAnalyses)
                .HasForeignKey(analysis =>
                    analysis.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(analysis =>
                analysis.JobId);

            entity.HasIndex(analysis =>
                analysis.ProcessedAt);
        });
    }
}