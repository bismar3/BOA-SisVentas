using Microsoft.EntityFrameworkCore;
using MSVenta.Seguridad.DTOs;
using MSVenta.Seguridad.Models;
using MSVenta.Seguridad.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Services
{
    public class BitacoraService : IBitacoraService
    {
        private readonly ContextDatabase _context;

        public BitacoraService(ContextDatabase context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BitacoraDTO>> GetAll()
        {
            var registros = await _context.Bitacoras
                .Include(b => b.Usuario)
                .OrderByDescending(b => b.ID_Bitacora)
                .ToListAsync();
            return registros.Select(MapToDTO);
        }

        public async Task<BitacoraDTO> GetById(int id)
        {
            var registro = await _context.Bitacoras
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.ID_Bitacora == id);
            return registro == null ? null : MapToDTO(registro);
        }

        public async Task<bool> Delete(int id)
        {
            var registro = await _context.Bitacoras.FindAsync(id);
            if (registro == null) return false;

            _context.Bitacoras.Remove(registro);
            await _context.SaveChangesAsync();
            return true;
        }

        private static BitacoraDTO MapToDTO(Bitacora b)
        {
            string usuarioUsername;
            string usuarioNombre;

            if (b.Usuario != null)
            {
                usuarioUsername = b.Usuario.Username;
                var nombreCompleto = $"{b.Usuario.Nombre} {b.Usuario.Apellido}".Trim();
                usuarioNombre = string.IsNullOrEmpty(nombreCompleto) ? b.Usuario.Username : nombreCompleto;
            }
            else if (b.ID_Usuario != null)
            {
                // El usuario referenciado ya no existe (p. ej. se borró después de generar el registro).
                usuarioUsername = null;
                usuarioNombre = "(usuario eliminado)";
            }
            else
            {
                usuarioUsername = null;
                usuarioNombre = null;
            }

            return new BitacoraDTO
            {
                ID_Bitacora = b.ID_Bitacora,
                fecha = b.fecha,
                Tabla = b.Tabla,
                Transaccion = b.Transaccion,
                ID_Usuario = b.ID_Usuario,
                hora = b.hora?.ToString(@"hh\:mm\:ss"),
                NroRegistro = b.NroRegistro,
                Usuario_Username = usuarioUsername,
                Usuario_Nombre = usuarioNombre
            };
        }
    }
}
