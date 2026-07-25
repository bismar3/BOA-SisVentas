using Microsoft.EntityFrameworkCore;
using MSVenta.Seguridad.Models;

namespace MSVenta.Seguridad.Repositories
{
    public class ContextDatabase : DbContext
    {
        public ContextDatabase(DbContextOptions<ContextDatabase> options)
        : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<RolPermiso> RolPermisos { get; set; }
        public DbSet<RolPermisoUsuario> RolPermisoUsuarios { get; set; }
        public DbSet<Bitacora> Bitacoras { get; set; }
        public DbSet<Historial> Historials { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Permiso>().ToTable("Permiso");
            modelBuilder.Entity<Rol>().ToTable("Rol");
            modelBuilder.Entity<RolPermiso>().ToTable("Rol_Permiso");
            modelBuilder.Entity<RolPermisoUsuario>().ToTable("Rol_Permiso_Usuario");
            modelBuilder.Entity<Bitacora>().ToTable("Bitacora");
            modelBuilder.Entity<Historial>().ToTable("Historial");

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.Rol_Id)
                .OnDelete(DeleteBehavior.SetNull);

            // Bitacora → Usuario (opcional; ID_Usuario es solo índice en la DB, no FK real).
            modelBuilder.Entity<Bitacora>()
                .HasOne(b => b.Usuario)
                .WithMany()
                .HasForeignKey(b => b.ID_Usuario)
                .OnDelete(DeleteBehavior.SetNull);
            //historial
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
 
            modelBuilder.Entity<RolPermiso>()
                .HasOne(rp => rp.Rol)
                .WithMany(r => r.RolPermisos)
                .HasForeignKey(rp => rp.ID_Rol)
                .HasConstraintName("FK_RolPermiso_Rol");

 
            modelBuilder.Entity<RolPermiso>()
                .HasOne(rp => rp.Permiso)
                .WithMany(p => p.RolPermisos)
                .HasForeignKey(rp => rp.ID_Permiso)
                .HasConstraintName("FK_RolPermiso_Permiso");

            modelBuilder.Entity<RolPermisoUsuario>()
                .HasOne(rpu => rpu.Usuario)
                .WithMany(u => u.RolPermisoUsuarios)
                .HasForeignKey(rpu => rpu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolPermisoUsuario>()
                .HasOne(rpu => rpu.RolPermiso)
                .WithMany(rp => rp.RolPermisoUsuarios)
                .HasForeignKey(rpu => rpu.ID_Rol_Permiso)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}