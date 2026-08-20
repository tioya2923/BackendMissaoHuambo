using Microsoft.EntityFrameworkCore;
using MissaoBackend.Models;

namespace MissaoBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Topico> Topicos => Set<Topico>();
    public DbSet<Cantico> Canticos => Set<Cantico>();
    public DbSet<CanticoUmb> CanticosUmb => Set<CanticoUmb>();
    public DbSet<TopicoUmb> TopicosUmb => Set<TopicoUmb>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Gestor> Gestores => Set<Gestor>();

    public DbSet<CatecismoUb> CatecismosUb => Set<CatecismoUb>();
    public DbSet<CatecismoPt> CatecismosPt => Set<CatecismoPt>();
    public DbSet<CatecismoPtTopico> CatecismoPtTopicos => Set<CatecismoPtTopico>();
    public DbSet<CatecismoUbTopico> CatecismoUbTopicos => Set<CatecismoUbTopico>();

    public DbSet<TopicoLat> TopicosLat => Set<TopicoLat>();
    public DbSet<CanticoLat> CanticosLat => Set<CanticoLat>();
    public DbSet<CatecismoLatTopico> CatecismoLatTopicos => Set<CatecismoLatTopico>();
    public DbSet<CatecismoLat> CatecismosLat => Set<CatecismoLat>();

    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<FormaApoio> FormasApoio => Set<FormaApoio>();

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Encomenda> Encomendas => Set<Encomenda>();
    public DbSet<ItemEncomenda> ItensEncomenda => Set<ItemEncomenda>();
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<FormaPagamentoLoja> FormasPagamentoLoja => Set<FormaPagamentoLoja>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Topico>()
            .HasIndex(t => t.Slug)
            .IsUnique();

        modelBuilder.Entity<Cantico>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        modelBuilder.Entity<CanticoUmb>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        modelBuilder.Entity<TopicoUmb>()
            .ToTable("TopicoUmb")
            .HasIndex(t => t.Slug)
            .IsUnique();

        modelBuilder.Entity<CatecismoPtTopico>()
            .HasOne(t => t.Parent)
            .WithMany(t => t.SubTopicos)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Gestor>()
            .HasIndex(g => g.Email)
            .IsUnique();

        modelBuilder.Entity<Utilizador>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<CanticoUmb>()
            .HasOne(c => c.Topico)
            .WithMany(t => t.Canticos)
            .HasForeignKey(c => c.TopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TopicoLat>()
            .HasIndex(t => t.Slug)
            .IsUnique();

        modelBuilder.Entity<CanticoLat>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        modelBuilder.Entity<CanticoLat>()
            .HasOne(c => c.Topico)
            .WithMany(t => t.Canticos)
            .HasForeignKey(c => c.TopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Impede eliminar um tópico enquanto ainda tiver conteúdo associado
        // (o admin tem de mover/eliminar o conteúdo filho primeiro)
        modelBuilder.Entity<Cantico>()
            .HasOne(c => c.Topico)
            .WithMany()
            .HasForeignKey(c => c.TopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CatecismoUb>()
            .HasOne(c => c.CatecismoUbTopico)
            .WithMany(t => t.CatecismosUb)
            .HasForeignKey(c => c.CatecismoUbTopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CatecismoLat>()
            .HasOne(c => c.CatecismoLatTopico)
            .WithMany(t => t.CatecismosLat)
            .HasForeignKey(c => c.CatecismoLatTopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CatecismoPt>()
            .HasOne(c => c.CatecismoPtTopico)
            .WithMany(t => t.CatecismosPt)
            .HasForeignKey(c => c.CatecismoPtTopicoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Loja ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Produto>()
            .Property(p => p.Preco)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Produto>()
            .Property(p => p.PrecoPromocional)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Encomenda>()
            .Property(e => e.Total)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ItemEncomenda>()
            .Property(i => i.PrecoUnitario)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Encomenda>()
            .Property(e => e.ValorComissao)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Encomenda>()
            .Property(e => e.PercentualComissaoAplicado)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Loja>()
            .Property(l => l.PercentualComissao)
            .HasPrecision(5, 2);

        modelBuilder.Entity<ItemEncomenda>()
            .HasOne(i => i.Encomenda)
            .WithMany(e => e.Itens)
            .HasForeignKey(i => i.EncomendaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Impede eliminar um produto enquanto ainda tiver encomendas associadas
        // (o histórico fica guardado via snapshot em ItemEncomenda; ver checagem no controller)
        modelBuilder.Entity<ItemEncomenda>()
            .HasOne<Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Marketplace multi-loja ──────────────────────────────────────────
        modelBuilder.Entity<Loja>()
            .HasIndex(l => l.Email)
            .IsUnique();

        // Impede eliminar uma loja enquanto ainda tiver produtos (o dono desativa em vez disso)
        modelBuilder.Entity<Produto>()
            .HasOne(p => p.Loja)
            .WithMany()
            .HasForeignKey(p => p.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Preserva o histórico de encomendas mesmo que a loja seja eliminada mais tarde
        modelBuilder.Entity<Encomenda>()
            .HasOne(e => e.Loja)
            .WithMany()
            .HasForeignKey(e => e.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        // As formas de pagamento pertencem à loja; eliminam-se com ela
        modelBuilder.Entity<FormaPagamentoLoja>()
            .HasOne(f => f.Loja)
            .WithMany(l => l.FormasPagamento)
            .HasForeignKey(f => f.LojaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
