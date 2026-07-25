using Microsoft.EntityFrameworkCore;
using BOA.Comercial.Models;
using BOA.Comercial.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BOA.Comercial.Services
{
    public class VentaService : IVentaService
    {
        private readonly ContextDatabase _context;

        public VentaService(ContextDatabase context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Venta>> GetAll() =>
            await _context.Ventas.ToListAsync();

        public async Task<IEnumerable<Venta>> GetByClienteId(int clienteId) =>
            await _context.Ventas.Where(v => v.Cliente_Id == clienteId).ToListAsync();

        public async Task<Venta> GetById(int id) =>
            await _context.Ventas.FindAsync(id);

        public async Task<IEnumerable<VentaDetalle>> GetAllDetalle()
        {
            var ventas = await _context.Ventas.ToListAsync();
            var clientes = await _context.Clientes.ToListAsync();
            var tickets = await _context.Tickets.ToListAsync();
            var transacciones = await _context.Transacciones.ToListAsync();

            return ventas.Select(v =>
            {
                var cliente = clientes.FirstOrDefault(c => c.Id == v.Cliente_Id);
                var ticket = tickets.FirstOrDefault(t => t.Venta_Id == v.Id);
                var transaccion = transacciones.FirstOrDefault(t => t.Venta_Id == v.Id);

                return new VentaDetalle
                {
                    Id = v.Id,
                    Codigo_Venta = v.Codigo_Venta,
                    Cliente_Id = v.Cliente_Id,
                    Cliente_Nombre = cliente != null ? $"{cliente.Nombre} {cliente.Apellido}" : "-",
                    Cliente_Documento = cliente?.Documento_Identidad ?? "-",
                    Programacion_Vuelo_Id = v.Programacion_Vuelo_Id,
                    Numero_Ticket = ticket?.Numero_Ticket ?? "-",
                    Asiento_Id = ticket?.Asiento_Id,
                    Pasajero_Nombre = ticket?.Pasajero_Nombre ?? "-",
                    Pasajero_Apellido = ticket?.Pasajero_Apellido ?? "-",
                    Metodo_Pago = transaccion?.Metodo_Pago ?? "-",
                    Monto_Total = v.Monto_Total,
                    Estado = v.Estado,
                    Transaccion_Estado = transaccion?.Estado ?? "-",
                    Created_At = v.Created_At
                };
            }).OrderByDescending(v => v.Id).ToList();
        }

        public async Task<IEnumerable<VentaDetalle>> GetDetalleByClienteId(int clienteId)
        {
            var ventas = await _context.Ventas.Where(v => v.Cliente_Id == clienteId).ToListAsync();
            var clientes = await _context.Clientes.ToListAsync();
            var tickets = await _context.Tickets.ToListAsync();
            var transacciones = await _context.Transacciones.ToListAsync();

            return ventas.Select(v =>
            {
                var cliente = clientes.FirstOrDefault(c => c.Id == v.Cliente_Id);
                var ticket = tickets.FirstOrDefault(t => t.Venta_Id == v.Id);
                var transaccion = transacciones.FirstOrDefault(t => t.Venta_Id == v.Id);

                return new VentaDetalle
                {
                    Id = v.Id,
                    Codigo_Venta = v.Codigo_Venta,
                    Cliente_Id = v.Cliente_Id,
                    Cliente_Nombre = cliente != null ? $"{cliente.Nombre} {cliente.Apellido}" : "-",
                    Cliente_Documento = cliente?.Documento_Identidad ?? "-",
                    Programacion_Vuelo_Id = v.Programacion_Vuelo_Id,
                    Numero_Ticket = ticket?.Numero_Ticket ?? "-",
                    Asiento_Id = ticket?.Asiento_Id,
                    Pasajero_Nombre = ticket?.Pasajero_Nombre ?? "-",
                    Pasajero_Apellido = ticket?.Pasajero_Apellido ?? "-",
                    Metodo_Pago = transaccion?.Metodo_Pago ?? "-",
                    Monto_Total = v.Monto_Total,
                    Estado = v.Estado,
                    Transaccion_Estado = transaccion?.Estado ?? "-",
                    Created_At = v.Created_At
                };
            }).OrderByDescending(v => v.Id).ToList();
        }

        public async Task<Venta> Create(Venta v)
        {
            v.Created_At = System.DateTime.Now;
            _context.Ventas.Add(v);
            await _context.SaveChangesAsync();
            return v;
        }

        public async Task Update(Venta v)
        {
            _context.Ventas.Update(v);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var item = await _context.Ventas.FindAsync(id);
            if (item != null)
            {
                // Borrar dependencias primero: devoluciones y tickets tienen FK a ventas
                // (transacciones no tiene FK, pero se limpia para no dejar huérfanos).
                _context.Devoluciones.RemoveRange(_context.Devoluciones.Where(d => d.Venta_Id == id));
                _context.Tickets.RemoveRange(_context.Tickets.Where(t => t.Venta_Id == id));
                _context.Transacciones.RemoveRange(_context.Transacciones.Where(t => t.Venta_Id == id));

                _context.Ventas.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}