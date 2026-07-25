using System.Collections.Generic;
using System.Threading.Tasks;
using MSVenta.Seguridad.DTOs;

namespace MSVenta.Seguridad.Services
{
    public interface IBitacoraService
    {
        Task<IEnumerable<BitacoraDTO>> GetAll();
        Task<BitacoraDTO> GetById(int id);
        Task<bool> Delete(int id);
    }
}
