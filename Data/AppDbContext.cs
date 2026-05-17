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
    }
}
