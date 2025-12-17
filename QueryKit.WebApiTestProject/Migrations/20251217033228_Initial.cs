using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryKit.WebApiTestProject.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.CreateSequence<int>(
                name: "AUT",
                startValue: 100045702L);

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    birth_month = table.Column<int>(type: "integer", nullable: true),
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
                    secondary_id = table.Column<Guid>(type: "uuid", nullable: true),
                    have_made_it_myself = table.Column<bool>(type: "boolean", nullable: false),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    collection_email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internal_identifier = table.Column<string>(type: "text", nullable: false, defaultValueSql: "concat('AUT', nextval('\"AUT\"'))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authors", x => x.id);
                    table.ForeignKey(
                        name: "fk_authors_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<string>(type: "text", nullable: false),
                    quality_level = table.Column<long>(type: "bigint", nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    measure = table.Column<string>(type: "text", nullable: false),
                    minimum_quality = table.Column<int>(type: "integer", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredients_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_preparations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_preparations", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_preparations_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_authors_recipe_id",
                table: "authors",
                column: "recipe_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_preparations_ingredient_id",
                table: "ingredient_preparations",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_recipe_id",
                table: "ingredients",
                column: "recipe_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "ingredient_preparations");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropSequence(
                name: "AUT");
        }
    }
}
