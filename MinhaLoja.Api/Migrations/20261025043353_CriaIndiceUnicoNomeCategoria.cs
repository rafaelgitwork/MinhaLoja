using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaLoja.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaIndiceUnicoNomeCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias");
        }
    }
}
