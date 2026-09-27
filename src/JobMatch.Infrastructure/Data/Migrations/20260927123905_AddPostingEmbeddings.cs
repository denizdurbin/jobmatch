using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace JobMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostingEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostingEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Vector = table.Column<Vector>(type: "vector(768)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingEmbeddings_Postings_PostingId",
                        column: x => x.PostingId,
                        principalTable: "Postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostingEmbeddings_PostingId_Kind",
                table: "PostingEmbeddings",
                columns: new[] { "PostingId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingEmbeddings_Vector",
                table: "PostingEmbeddings",
                column: "Vector")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostingEmbeddings");
        }
    }
}
