using System.ComponentModel.DataAnnotations;

namespace BOA.Comercial.Models
{
    public class Bitacora
    {
        [Key]
        public int ID_Bitacora { get; set; }
        public int fecha { get; set; }
        public string Tabla { get; set; }
        public string Transaccion { get; set; }
        public int? ID_Usuario { get; set; }
        public System.TimeSpan hora { get; set; }
        public int? NroRegistro { get; set; }
    }
}
