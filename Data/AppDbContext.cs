using Microsoft.EntityFrameworkCore;
using MissaoBackend.Models;

namespace MissaoBackend.Data;

public class AppDbContext : DbContext
{
    public DbSet<Noticia> Noticias => Set<Noticia>();
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

        modelBuilder.Entity<Gestor>()
            .HasIndex(g => g.Email)
            .IsUnique();

        modelBuilder.Entity<CanticoUmb>()
      .HasOne(c => c.Topico)
      .WithMany(t => t.Canticos)
      .HasForeignKey(c => c.TopicoId)
      .OnDelete(DeleteBehavior.Restrict);


    }
}
