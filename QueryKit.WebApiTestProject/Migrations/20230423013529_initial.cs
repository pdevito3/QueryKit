using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryKit.WebApiTestProject.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "people");
        }
    }
}
