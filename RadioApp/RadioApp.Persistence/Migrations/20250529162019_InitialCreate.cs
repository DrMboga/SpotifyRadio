using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadioApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadioRegion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SabaRadioButton = table.Column<short>(type: "INTEGER", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRegion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RadioStation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Button = table.Column<short>(type: "INTEGER", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SabaFrequency = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamUrl = table.Column<string>(type: "TEXT", nullable: false),
                    RadioLogoBase64 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioStation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpotifySettings",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: true),
                    RedirectUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuthToken = table.Column<string>(type: "TEXT", nullable: true),
                    AuthTokenExpiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: true),
                    PlaylistName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifySettings", x => x.ClientId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RadioRegion_Region",
                table: "RadioRegion",
                column: "Region",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioStation_Region",
                table: "RadioStation",
                column: "Region");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioRegion");

            migrationBuilder.DropTable(
                name: "RadioStation");

            migrationBuilder.DropTable(
                name: "SpotifySettings");
        }
    }
}
