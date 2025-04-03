using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_products_AuthorId",
                table: "products",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_authors_AuthorId",
                table: "products",
                column: "AuthorId",
                principalTable: "authors",
                principalColumn: "AuthorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_authors_AuthorId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_AuthorId",
                table: "products");
        }
    }
}
