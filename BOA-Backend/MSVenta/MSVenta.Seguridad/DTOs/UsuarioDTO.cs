using System;
using System.Collections.Generic;

namespace MSVenta.Seguridad.DTOs
{
    public class UsuarioDTO
    {
        public int UserId { get; set; }
        public string Fullname { get; set; }
        public string Username { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Documento_Identidad { get; set; }
        public string Telefono { get; set; }
        public string Estado { get; set; }
        public int? Rol_Id { get; set; }
        public DateTime? Fecha_Creacion { get; set; }
        public TimeSpan? hora { get; set; }
        public string? Direccion { get; set; }
        public List<RolDTO> Roles { get; set; }
    }
}