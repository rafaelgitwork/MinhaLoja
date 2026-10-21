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

    public DbSet<Usuario> Usuarios => Set<Usuario>();

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

        modelBuilder.Entity<Usuario>(usuario =>
        {
            usuario.ToTable("Usuarios");

            usuario.HasKey(u => u.Id);

            usuario.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(150);

            usuario.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(254);

            usuario.Property(u => u.EmailNormalizado)
                .IsRequired()
                .HasMaxLength(254);

            usuario.HasIndex(u => u.EmailNormalizado)
                .IsUnique();

            usuario.Property(u => u.SenhaHash)
                .IsRequired()
                .HasMaxLength(512);

            usuario.Property(u => u.Perfil)
                .IsRequired()
                .HasMaxLength(20);
        });
    }
}