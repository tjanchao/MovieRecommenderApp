using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace movieRecommender.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonalDataOwnershipAndSeededDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Films",
                columns: table => new
                {
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ReleaseYear = table.Column<int>(type: "INTEGER", nullable: false),
                    RuntimeMin = table.Column<int>(type: "INTEGER", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: false),
                    PosterPath = table.Column<string>(type: "TEXT", nullable: true),
                    Synopsis = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Films", x => x.TmdbId);
                });

            migrationBuilder.CreateTable(
                name: "TasteProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TasteLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PreferredGenres = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredEras = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasteProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasteProfiles_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DismissedRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    DismissedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DismissedRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DismissedRecommendations_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DismissedRecommendations_Films_TmdbId",
                        column: x => x.TmdbId,
                        principalTable: "Films",
                        principalColumn: "TmdbId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentiment = table.Column<int>(type: "INTEGER", nullable: false),
                    RatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ratings_Films_TmdbId",
                        column: x => x.TmdbId,
                        principalTable: "Films",
                        principalColumn: "TmdbId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WatchHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    WatchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchHistory_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchHistory_Films_TmdbId",
                        column: x => x.TmdbId,
                        principalTable: "Films",
                        principalColumn: "TmdbId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Watchlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlist_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchlist_Films_TmdbId",
                        column: x => x.TmdbId,
                        principalTable: "Films",
                        principalColumn: "TmdbId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DismissedRecommendations_OwnerUserId",
                table: "DismissedRecommendations",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DismissedRecommendations_OwnerUserId_TmdbId",
                table: "DismissedRecommendations",
                columns: new[] { "OwnerUserId", "TmdbId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DismissedRecommendations_TmdbId",
                table: "DismissedRecommendations",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_OwnerUserId",
                table: "Ratings",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_OwnerUserId_TmdbId",
                table: "Ratings",
                columns: new[] { "OwnerUserId", "TmdbId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_TmdbId",
                table: "Ratings",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_TasteProfiles_OwnerUserId",
                table: "TasteProfiles",
                column: "OwnerUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistory_OwnerUserId",
                table: "WatchHistory",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistory_OwnerUserId_WatchedAt",
                table: "WatchHistory",
                columns: new[] { "OwnerUserId", "WatchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistory_TmdbId",
                table: "WatchHistory",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlist_OwnerUserId",
                table: "Watchlist",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlist_OwnerUserId_TmdbId",
                table: "Watchlist",
                columns: new[] { "OwnerUserId", "TmdbId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlist_TmdbId",
                table: "Watchlist",
                column: "TmdbId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DismissedRecommendations");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "TasteProfiles");

            migrationBuilder.DropTable(
                name: "WatchHistory");

            migrationBuilder.DropTable(
                name: "Watchlist");

            migrationBuilder.DropTable(
                name: "Films");
        }
    }
}
