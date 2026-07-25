using System;
using System.ComponentModel.DataAnnotations;

namespace MSVenta.Seguridad.Models
{
    public class Historial
    {
        [Key]
        public int id_historial { get; set; }
        public DateTime? fecha { get; set; }
        public string tipo_evento { get; set; }
        public Boolean exitoso { get; set; }
        public TimeSpan? hora { get; set; }
        public int? UserId { get; set; }
        public Usuario Usuario { get; set; }

    }
}
