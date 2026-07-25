
using System;
using System.ComponentModel.DataAnnotations;

namespace MSVenta.Seguridad.Models
{
    public class Bitacora
    {
        [Key]
        public int ID_Bitacora { get; set; }
        public DateTime? fecha { get; set; }
        public string Tabla { get; set; }
        public string Transaccion { get; set; }
        public int? ID_Usuario { get; set; }
        public TimeSpan? hora { get; set; }
        public int? NroRegistro { get; set; }

        // Navegación al usuario que originó el registro (relación opcional, sin FK en la DB).
        public Usuario Usuario { get; set; }
    }
}