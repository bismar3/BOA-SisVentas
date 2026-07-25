using Microsoft.EntityFrameworkCore;
using BOA.Comercial.Models;
using BOA.Comercial.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BOA.Comercial.Services
{
    public class ClienteService : IClienteService
    {
        private readonly ContextDatabase _context;
        public ClienteService(ContextDatabase context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Cliente>> GetAll() =>
            await _context.Clientes.ToListAsync();
        public async Task<Cliente> GetById(int id) =>
            await _context.Clientes.FindAsync(id);
        public async Task<Cliente> GetByUsuarioId(int usuarioId) =>
            await _context.Clientes.FirstOrDefaultAsync(c => c.Usuario_Id == usuarioId);
        public async Task Create(Cliente c)
        {
            _context.Clientes.Add(c);
            await _context.SaveChangesAsync();
        }
        public async Task Update(Cliente c)
        {
            _context.Clientes.Update(c);
            await _context.SaveChangesAsync();
        }
        public async Task Delete(int id)
        {
            var item = await _context.Clientes.FindAsync(id);
            if (item != null)
            {
                _context.Clientes.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}