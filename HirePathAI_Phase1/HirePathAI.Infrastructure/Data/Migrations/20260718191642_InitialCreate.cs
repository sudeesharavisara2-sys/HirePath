using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HirePathAI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentPosition = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TotalExperienceYears = table.Column<double>(type: "float", nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResumeFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PreferredSkills = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MinimumExperienceYears = table.Column<int>(type: "int", nullable: false),
                    MinimumEducation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    CoverLetter = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AtsScore = table.Column<double>(type: "float", nullable: true),
                    MatchPercentage = table.Column<double>(type: "float", nullable: true),
                    AiRecommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResumeAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CandidateEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CandidatePhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ResumeFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResumeFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtractedSkills = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Certifications = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TotalExperienceYears = table.Column<double>(type: "float", nullable: false),
                    SkillsScore = table.Column<double>(type: "float", nullable: false),
                    ExperienceScore = table.Column<double>(type: "float", nullable: false),
                    EducationScore = table.Column<double>(type: "float", nullable: false),
                    CertificationScore = table.Column<double>(type: "float", nullable: false),
                    AtsScore = table.Column<double>(type: "float", nullable: false),
                    MatchPercentage = table.Column<double>(type: "float", nullable: false),
                    MlConfidence = table.Column<double>(type: "float", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeAnalyses_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_Email",
                table: "Candidates",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CandidateId_JobId",
                table: "JobApplications",
                columns: new[] { "CandidateId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId",
                table: "JobApplications",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumeAnalyses_JobId",
                table: "ResumeAnalyses",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "ResumeAnalyses");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
