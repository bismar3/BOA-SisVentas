using BOA.Comercial.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BOA.Comercial.Services
{
    public interface IBitacoraService
    {
        Task Registrar(string tabla, string transaccion, int? idUsuario, int? nroRegistro = null);
        Task<IEnumerable<Bitacora>> GetAllBitacoras();
        Task<Bitacora> GetBitacoraId(int id);
        Task UpdateBitacora(Bitacora bitacora);
        Task DeleteBitacora(int id);
    }
}
