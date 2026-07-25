using MSVenta.Seguridad.DTOs;
using MSVenta.Seguridad.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Services
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioDTO>> GetAllUsuarios();
        Task<UsuarioDTO> GetUsuarioById(int id);
        Task<Usuario> CreateUsuario(Usuario usuario, int? adminId = null);
        Task UpdateUsuario(Usuario usuario, int? adminId = null);
        Task DeleteUsuario(int id, int? adminId = null);
        
        Task<LoginResult> ValidateAsync(string userName, string password);
    }
}