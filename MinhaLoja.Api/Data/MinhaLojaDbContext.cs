using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Entities;

namespace MinhaLoja.Api.Data;

public class MinhaLojaDbContext : DbContext
{
    public MinhaLojaDbContext(
        DbContextOptions<MinhaLojaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(categoria =>
        {
            categoria.ToTable("Categorias");

            categoria.HasKey(c => c.Id);

            categoria.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Produto>(produto =>
        {
            produto.ToTable("Produtos");

            produto.HasKey(p => p.Id);

            produto.Property(p => p.Nome)
                .IsRequired()
                .HasMaxLength(150);

            produto.Property(p => p.Descricao)
                .HasMaxLength(1000);

            produto.Property(p => p.Preco)
                .HasPrecision(10, 2);

            produto.HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}