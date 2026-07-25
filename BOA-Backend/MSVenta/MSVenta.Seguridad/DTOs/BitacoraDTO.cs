using System;

namespace MSVenta.Seguridad.DTOs
{
    // Vista de solo lectura de la bitácora. No expone datos sensibles del usuario
    // (p. ej. el hash de la contraseña), solo lo necesario para auditar.
    public class BitacoraDTO
    {
        public int ID_Bitacora { get; set; }
        public DateTime? fecha { get; set; }
        public string Tabla { get; set; }
        public string Transaccion { get; set; }
        public int? ID_Usuario { get; set; }
        public string hora { get; set; }   // "HH:mm:ss" (más cómodo para el frontend que un TimeSpan)
        public int? NroRegistro { get; set; }

        // Datos mínimos del usuario que originó el registro (null si ID_Usuario no existe).
        public string Usuario_Username { get; set; }
        public string Usuario_Nombre { get; set; }
    }
}
