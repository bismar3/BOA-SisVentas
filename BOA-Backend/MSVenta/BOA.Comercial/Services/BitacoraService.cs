using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BOA.Comercial.Models;
using BOA.Comercial.Repositories;

namespace BOA.Comercial.Services
{
    public class BitacoraService : IBitacoraService
    {
        private readonly ContextDatabase _context;
        public BitacoraService(ContextDatabase context)
        {
            _context = context;
        }

        public async Task Registrar(string tabla, string transaccion, int? idUsuario, int? nroRegistro = null)
        {
            var fechaUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var horaTexto = DateTime.Now.ToString("HH:mm:ss");

            await _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO bitacora (fecha, Tabla, Transaccion, ID_Usuario, hora, NroRegistro) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                fechaUnix, tabla, transaccion, idUsuario, horaTexto, nroRegistro
            );
        }

        public async Task<IEnumerable<Bitacora>> GetAllBitacoras()
        {
            return await _context.Bitacoras.ToListAsync();
        }

        public async Task<Bitacora> GetBitacoraId(int id)
        {
            return await _context.Bitacoras.FirstOrDefaultAsync(b => b.ID_Bitacora == id);
        }

        public async Task UpdateBitacora(Bitacora bitacora)
        {
            _context.Entry(bitacora).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBitacora(int id)
        {
            var item = await _context.Bitacoras.AsNoTracking().FirstOrDefaultAsync(b => b.ID_Bitacora == id);
            if (item != null)
            {
                _context.Bitacoras.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}