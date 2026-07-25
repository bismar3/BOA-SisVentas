using Microsoft.EntityFrameworkCore;
using MSVenta.Seguridad.DTOs;
using MSVenta.Seguridad.Models;
using MSVenta.Seguridad.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace MSVenta.Seguridad.Services
{
    public class HistorialService : IHistorialService
    {
        private readonly ContextDatabase _context;
        public HistorialService(ContextDatabase context)
        {
            _context = context;
        }
        public async Task CrearHistorial(int? UserId, string tipoEvento, bool existoso)
        {
            var ahora = DateTime.Now;
            var nuevoHistorial = new Historial()
            {
                UserId = UserId,
                tipo_evento= tipoEvento,
                exitoso= existoso,
                fecha= ahora,
                hora=ahora.TimeOfDay,
            };
            _context.Historials.Add(nuevoHistorial);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Historial>> GetAllHistorial()
        {
            var registros = await _context.Historials
            .Include(h => h.Usuario)
            .OrderByDescending(h => h.id_historial)
            .ToListAsync();
            return registros;
        }
        public async Task<IEnumerable<Historial>> GetHistorialByUsuario(int usuario)
        {
            var registros = await _context.Historials
            .Include(h => h.Usuario)
            .Where(h => h.UserId == usuario)
            .OrderByDescending(h => h.id_historial)
            .ToListAsync();
            return registros;
        }
    }
}
    

