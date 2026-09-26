using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JobMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostingExtractions",
                columns: table => new
                {
                    PostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seniority = table.Column<string>(type: "text", nullable: false),
                    MinYears = table.Column<int>(type: "integer", nullable: true),
                    ContractType = table.Column<string>(type: "text", nullable: false),
                    RemotePolicy = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    SalaryMin = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    SalaryMax = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    SalaryCurrency = table.Column<string>(type: "text", nullable: true),
                    SalaryPeriod = table.Column<string>(type: "text", nullable: true),
                    LanguagesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    PromptVersion = table.Column<string>(type: "text", nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingExtractions", x => x.PostingId);
                    table.ForeignKey(
                        name: "FK_PostingExtractions_Postings_PostingId",
                        column: x => x.PostingId,
                        principalTable: "Postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostingSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingSkills_Postings_PostingId",
                        column: x => x.PostingId,
                        principalTable: "Postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostingExtractions_City",
                table: "PostingExtractions",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_PostingExtractions_ContractType",
                table: "PostingExtractions",
                column: "ContractType");

            migrationBuilder.CreateIndex(
                name: "IX_PostingSkills_NormalizedName",
                table: "PostingSkills",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_PostingSkills_PostingId",
                table: "PostingSkills",
                column: "PostingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostingExtractions");

            migrationBuilder.DropTable(
                name: "PostingSkills");
        }
    }
}
