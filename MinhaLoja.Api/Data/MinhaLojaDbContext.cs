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

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

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

        modelBuilder.Entity<Pedido>(pedido =>
        {
            pedido.ToTable("Pedidos");

            pedido.HasKey(p => p.Id);

            pedido.Property(p => p.CriadoEmUtc)
                .HasColumnType("datetime(6)");

            pedido.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            pedido.Property(p => p.Observacao)
                .HasMaxLength(1000);

            pedido.Property(p => p.Total)
                .HasPrecision(18, 2);

            pedido.HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            pedido.HasIndex(p => new { p.UsuarioId, p.CriadoEmUtc });
        });

        modelBuilder.Entity<PedidoItem>(item =>
        {
            item.ToTable("PedidoItens");

            item.HasKey(i => i.Id);

            item.Property(i => i.NomeProduto)
                .IsRequired()
                .HasMaxLength(150);

            item.Property(i => i.PrecoUnitario)
                .HasPrecision(10, 2);

            item.HasOne(i => i.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            item.HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);

            item.HasIndex(i => new { i.PedidoId, i.ProdutoId })
                .IsUnique();
        });
    }
}