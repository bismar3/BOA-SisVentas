using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSVenta.Seguridad.Models;
namespace MSVenta.Seguridad.Services
{
    public interface IHistorialService
    {
        Task CrearHistorial(int? userId, string tipoEvento, bool exitoso);
        Task<IEnumerable<Historial>> GetAllHistorial();
        Task<IEnumerable<Historial>> GetHistorialByUsuario(int usuario);
    }
}