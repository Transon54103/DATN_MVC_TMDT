using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "authors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "authors");
        }
    }
}
