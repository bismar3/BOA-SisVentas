using BOA.Comercial.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BOA.Comercial.Services
{
    public interface IClienteService
    {
        Task<IEnumerable<Cliente>> GetAll();
        Task<Cliente> GetById(int id);
        Task<Cliente> GetByUsuarioId(int usuarioId);
        Task Create(Cliente c);
        Task Update(Cliente c);
        Task Delete(int id);
    }
}