using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleWebApp.Migrations
{
    /// <inheritdoc />
    public partial class Localization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "localization");

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "localization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nchar(5)", fixedLength: true, maxLength: 5, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Texts",
                schema: "localization",
                columns: table => new
                {
                    TextId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Texts", x => new { x.TextId, x.ResourceId });
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                schema: "localization",
                columns: table => new
                {
                    LanguageId = table.Column<string>(type: "nchar(5)", fixedLength: true, maxLength: 5, nullable: false),
                    TextId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => new { x.LanguageId, x.TextId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_Translations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "localization",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Translations_Texts_TextId_ResourceId",
                        columns: x => new { x.TextId, x.ResourceId },
                        principalSchema: "localization",
                        principalTable: "Texts",
                        principalColumns: new[] { "TextId", "ResourceId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Translations_TextId_ResourceId",
                schema: "localization",
                table: "Translations",
                columns: new[] { "TextId", "ResourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Translations",
                schema: "localization");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "localization");

            migrationBuilder.DropTable(
                name: "Texts",
                schema: "localization");
        }
    }
}
