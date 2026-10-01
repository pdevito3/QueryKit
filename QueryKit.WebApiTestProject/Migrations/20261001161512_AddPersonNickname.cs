using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryKit.WebApiTestProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "people",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nickname",
                table: "people");
        }
    }
}
