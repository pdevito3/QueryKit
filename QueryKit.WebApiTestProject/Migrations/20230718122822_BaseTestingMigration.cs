using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryKit.WebApiTestProject.Migrations
{
    /// <inheritdoc />
    public partial class BaseTestingMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    birth_month = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<decimal>(type: "numeric", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    favorite = table.Column<bool>(type: "boolean", nullable: true),
                    specific_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    specific_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    physical_address_line1 = table.Column<string>(type: "text", nullable: false),
                    physical_address_line2 = table.Column<string>(type: "text", nullable: false),
                    physical_address_city = table.Column<string>(type: "text", nullable: false),
                    physical_address_state = table.Column<string>(type: "text", nullable: false),
                    physical_address_postal_code = table.Column<string>(type: "text", nullable: false),
                    physical_address_country = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    directions = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    date_of_origin = table.Column<DateOnly>(type: "date", nullable: true),
                    have_made_it_myself = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "author",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_author", x => x.id);
                    table.ForeignKey(
                        name: "fk_author_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<string>(type: "text", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    measure = table.Column<string>(type: "text", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_author_recipe_id",
                table: "author",
                column: "recipe_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_recipe_id",
                table: "ingredient",
                column: "recipe_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "author");

            migrationBuilder.DropTable(
                name: "ingredient");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "recipes");
        }
    }
}
