using Microsoft.EntityFrameworkCore;
using BOA.Comercial.Models;

namespace BOA.Comercial.Repositories
{
    public class ContextDatabase : DbContext
    {
        public ContextDatabase(DbContextOptions<ContextDatabase> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Devolucion> Devoluciones { get; set; }
        public DbSet<Bitacora> Bitacoras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>().ToTable("clientes");
            modelBuilder.Entity<Venta>().ToTable("ventas");
            modelBuilder.Entity<Ticket>().ToTable("tickets");
            modelBuilder.Entity<Transaccion>().ToTable("transacciones");
            modelBuilder.Entity<Reserva>().ToTable("reservas");
            modelBuilder.Entity<Devolucion>().ToTable("devoluciones");
            modelBuilder.Entity<Bitacora>().ToTable("bitacora");
        }
    }
}