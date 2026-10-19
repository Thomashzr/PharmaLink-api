using Microsoft.EntityFrameworkCore;
using PharmaLink.Models;

namespace PharmaLink.Context;

public class PharmaLinkContext(DbContextOptions<PharmaLinkContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Dispensacion> Dispensaciones => Set<Dispensacion>();
    public DbSet<Reposicion> Reposiciones => Set<Reposicion>();
    public DbSet<Entrega> Entregas => Set<Entrega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índice único en NombreUsuario
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        // Conversión para columnas decimal en SQLite (compatibilidad con double)
        modelBuilder.Entity<Medicamento>()
            .Property(m => m.PrecioUnitario)
            .HasConversion<double>();

        modelBuilder.Entity<Insumo>()
            .Property(i => i.PrecioUnitario)
            .HasConversion<double>();

        // Integridad referencial: Restringir borrado de medicamento si existen dispensaciones
        modelBuilder.Entity<Dispensacion>()
            .HasOne(d => d.Medicamento)
            .WithMany(m => m.Dispensaciones)
            .HasForeignKey(d => d.MedicamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Integridad referencial: Borrado en cascada de entregas al eliminar reposición
        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Reposicion)
            .WithMany(r => r.Entregas)
            .HasForeignKey(e => e.ReposicionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Integridad referencial: Restringir borrado de insumo si existen entregas históricas
        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Insumo)
            .WithMany(i => i.Entregas)
            .HasForeignKey(e => e.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
