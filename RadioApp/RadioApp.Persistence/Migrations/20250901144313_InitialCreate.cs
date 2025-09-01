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
                name: "Countries",
                columns: table => new
                {
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    FlagImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    DetailsUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Country);
                });

            migrationBuilder.CreateTable(
                name: "RadioStation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Button = table.Column<short>(type: "INTEGER", nullable: false),
                    StationDetailsUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SabaFrequency = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RadioLogoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    CountryFlagBase64 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioStation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RadioStationInfos",
                columns: table => new
                {
                    DetailsUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    RegionInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Likes = table.Column<int>(type: "INTEGER", nullable: false),
                    Dislikes = table.Column<int>(type: "INTEGER", nullable: false),
                    StationDescription = table.Column<string>(type: "TEXT", nullable: false),
                    StationWebPage = table.Column<string>(type: "TEXT", nullable: true),
                    StationImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    StationStreamUrl = table.Column<string>(type: "TEXT", nullable: true),
                    StationProcessed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioStationInfos", x => x.DetailsUrl);
                });

            migrationBuilder.CreateTable(
                name: "SpotifySettings",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: true),
                    RedirectUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuthToken = table.Column<string>(type: "TEXT", nullable: true),
                    AuthTokenExpiration = table.Column<long>(type: "INTEGER", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: true),
                    PlaylistName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifySettings", x => x.ClientId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RadioStation_StationDetailsUrl",
                table: "RadioStation",
                column: "StationDetailsUrl");

            migrationBuilder.CreateIndex(
                name: "IX_RadioStationInfos_Country",
                table: "RadioStationInfos",
                column: "Country");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "RadioStation");

            migrationBuilder.DropTable(
                name: "RadioStationInfos");

            migrationBuilder.DropTable(
                name: "SpotifySettings");
        }
    }
}
