using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace MSVenta.Seguridad.Models
{
    public class Usuario
    {
        [Key]
        public int UserId { get; set; }
        public string Fullname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Documento_Identidad { get; set; }
        public DateTime? Fecha_Nacimiento { get; set; }
        public string Telefono { get; set; }
        public string Estado { get; set; }
        public int? Rol_Id { get; set; }
        public int Intentos_Fallidos { get; set; }
        public int Veces_Bloqueado { get; set; }
        public DateTime? Bloqueado_Hasta { get; set; }
        public DateTime? Fecha_Creacion { get; set; }
        public string? Direccion { get; set; }
        public Rol Rol { get; set; }
        public TimeSpan? hora { get; set; }
        public ICollection<RolPermisoUsuario> RolPermisoUsuarios { get; set; }
    }
}