using Microsoft.EntityFrameworkCore;
using MSVenta.Seguridad.DTOs;
using MSVenta.Seguridad.Models;
using MSVenta.Seguridad.Repositories;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ContextDatabase _context;
        private readonly IHistorialService _historialService;

        private const int MAX_INTENTOS_FALLIDOS = 5;
        private const int MINUTOS_BLOQUEO = 5;

        public UsuarioService(ContextDatabase context, IHistorialService historialService)
        {
            _context = context;
            _historialService = historialService;  
        }

        public async Task<IEnumerable<UsuarioDTO>> GetAllUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
                .ToListAsync();
            return usuarios.Select(u => MapToDTO(u));
        }

        public async Task<UsuarioDTO> GetUsuarioById(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
                .FirstOrDefaultAsync(u => u.UserId == id);
            if (usuario == null) return null;
            return MapToDTO(usuario);
        }

        private UsuarioDTO MapToDTO(Usuario u)
        {
            var roles = new List<RolDTO>();
            if (u.Rol != null)
            {
                roles.Add(new RolDTO
                {
                    ID_Rol = u.Rol.ID_Rol,
                    Nombre_Rol = u.Rol.Nombre_Rol,
                    Permisos = u.Rol.RolPermisos?
                        .Where(rp => rp.Acceso)
                        .Select(rp => new PermisoDTO
                        {
                            ID_Permiso = rp.Permiso.ID_Permiso,
                            Nombre_Permiso = rp.Permiso.Nombre_Permiso
                        }).ToList() ?? new List<PermisoDTO>()
                });
            }
            return new UsuarioDTO
            {
                UserId = u.UserId,
                Fullname = u.Fullname,
                Username = u.Username,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Documento_Identidad = u.Documento_Identidad,
                Telefono = u.Telefono,
                Estado = u.Estado,
                Rol_Id = u.Rol_Id,
                Fecha_Creacion = u.Fecha_Creacion,
                Direccion = u.Direccion,
                Roles = roles,
                hora = u.hora
            };
        }

        public async Task<Usuario> CreateUsuario(Usuario usuario, int? adminId =null)
        {
            ValidarComplejidadPassword(usuario.Password);

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password, workFactor: 11);
            usuario.Fecha_Creacion = DateTime.Now;
            usuario.hora = DateTime.Now.TimeOfDay;
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            var ahoraIns = DateTime.Now;
            var ahora = DateTime.Now;

            _context.Bitacoras.Add(new Bitacora
            {
                Tabla = "usuario",
                Transaccion="INSERT",
                ID_Usuario= adminId,
                NroRegistro=usuario.UserId,
                fecha=ahoraIns,
                hora = ahora.TimeOfDay,
            });
 
            await _context.SaveChangesAsync();
            return usuario;

        }
       
        private void ValidarComplejidadPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

            bool tieneLetra = password.Any(char.IsLetter);
            bool tieneNumero = password.Any(char.IsDigit);

            if (!tieneLetra || !tieneNumero)
                throw new ArgumentException("La contraseña debe contener al menos una letra y un número.");
        }

        public async Task UpdateUsuario(Usuario usuario, int? adminId = null)
        {
            // Entidad TRACKEADA: actualizamos campo por campo para no pisar columnas
            // que el cliente no envía (rol, contadores de bloqueo, fecha de creación, etc.).
            var existente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UserId == usuario.UserId);

            if (existente == null)
                throw new ArgumentException("Usuario no encontrado.");

            // Password: solo se cambia si llega una nueva en claro; si viene vacía o ya
            // es un hash BCrypt, se conserva la actual.
            if (!string.IsNullOrWhiteSpace(usuario.Password) && !EsHashBCrypt(usuario.Password))
            {
                ValidarComplejidadPassword(usuario.Password);
                existente.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password, workFactor: 11);
            }

            // Campos de texto: solo se sobrescriben si vienen con valor (null/vacío => se conserva).
            if (!string.IsNullOrWhiteSpace(usuario.Username)) existente.Username = usuario.Username;
            if (!string.IsNullOrWhiteSpace(usuario.Fullname)) existente.Fullname = usuario.Fullname;
            if (!string.IsNullOrWhiteSpace(usuario.Nombre)) existente.Nombre = usuario.Nombre;
            if (!string.IsNullOrWhiteSpace(usuario.Apellido)) existente.Apellido = usuario.Apellido;
            if (!string.IsNullOrWhiteSpace(usuario.Email)) existente.Email = usuario.Email;
            if (!string.IsNullOrWhiteSpace(usuario.Documento_Identidad)) existente.Documento_Identidad = usuario.Documento_Identidad;
            if (!string.IsNullOrWhiteSpace(usuario.Telefono)) existente.Telefono = usuario.Telefono;
            if (!string.IsNullOrWhiteSpace(usuario.Estado)) existente.Estado = usuario.Estado;
            if (!string.IsNullOrWhiteSpace(usuario.Direccion)) existente.Direccion = usuario.Direccion;

            // Rol y fecha de nacimiento: solo se tocan si vienen explícitos.
            if (usuario.Rol_Id.HasValue) existente.Rol_Id = usuario.Rol_Id;
            if (usuario.Fecha_Nacimiento.HasValue) existente.Fecha_Nacimiento = usuario.Fecha_Nacimiento;

            await _context.SaveChangesAsync();

            // Bitácora: quién (adminId) actualizó a qué usuario (NroRegistro).
            var ahoraUpd = DateTime.Now;
            _context.Bitacoras.Add(new Bitacora
            {
                Tabla = "usuario",
                Transaccion = "UPDATE",
                ID_Usuario = adminId,
                NroRegistro = existente.UserId,
                fecha = ahoraUpd,
                hora = ahoraUpd.TimeOfDay
            });
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUsuario(int id, int? adminId = null)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                // Bitácora: quién (adminId) borró a qué usuario (NroRegistro).
                // Si no llega adminId, se registra el propio usuario borrado como referencia.
                var ahora = DateTime.Now;
                _context.Bitacoras.Add(new Bitacora
                {
                    Tabla = "usuario",
                    Transaccion = "DELETE",
                    ID_Usuario = adminId ?? id,
                    NroRegistro = id,
                    fecha = ahora,
                    hora = ahora.TimeOfDay
                });
                await _context.SaveChangesAsync();

            }
        }

        private bool EsHashBCrypt(string valor)
        {
            return !string.IsNullOrEmpty(valor) &&
                   (valor.StartsWith("$2a$") || valor.StartsWith("$2b$") || valor.StartsWith("$2y$"));
        }

        public async Task<LoginResult> ValidateAsync(string userName, string password)
        {
            
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x => x.Username == userName);
           

            if (usuario == null)
            {
                return new LoginResult { Exitoso = false, Mensaje = "Usuario o contraseña incorrectos." };
            }

            if (!string.IsNullOrEmpty(usuario.Estado) && usuario.Estado != "Activo")
            {
                return new LoginResult { Exitoso = false, Mensaje = "Tu cuenta está deshabilitada. Contacta a un administrador." };
            }

            if (usuario.Bloqueado_Hasta.HasValue && usuario.Bloqueado_Hasta.Value > DateTime.Now)
            {
                var minutosRestantes = (int)Math.Ceiling((usuario.Bloqueado_Hasta.Value - DateTime.Now).TotalMinutes);
                return new LoginResult
                {
                    Exitoso = false,
                    Mensaje = $"Cuenta bloqueada temporalmente por intentos fallidos. Intenta de nuevo en {minutosRestantes} minuto(s)."
                };
            }
            

            bool passwordCorrecta;

            if (EsHashBCrypt(usuario.Password))
            {
                passwordCorrecta = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
            }
            else
            {
                passwordCorrecta = usuario.Password == password;

                if (passwordCorrecta)
                {
                    usuario.Password = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
                }
            }

            if (!passwordCorrecta)
            {
                usuario.Intentos_Fallidos = usuario.Intentos_Fallidos + 1;

                if (usuario.Intentos_Fallidos >= MAX_INTENTOS_FALLIDOS)
                {
                    usuario.Bloqueado_Hasta = DateTime.Now.AddMinutes(MINUTOS_BLOQUEO);
                    usuario.Veces_Bloqueado = usuario.Veces_Bloqueado + 1;
                    usuario.Intentos_Fallidos = 0;

                    await _context.SaveChangesAsync();
                    await _historialService.CrearHistorial(usuario.UserId, "LOGIN", false);
                    return new LoginResult
                    {
                        Exitoso = false,
                        Mensaje = $"Cuenta bloqueada temporalmente por {MINUTOS_BLOQUEO} minutos debido a múltiples intentos fallidos."
                    };
                }

                await _context.SaveChangesAsync();
                await _historialService.CrearHistorial(usuario.UserId, "LOGIN", false);

                return new LoginResult { Exitoso = false, Mensaje = "Usuario o contraseña incorrectos." };
            }

            usuario.Intentos_Fallidos = 0;
            await _context.SaveChangesAsync();
            await _historialService.CrearHistorial(usuario.UserId, "LOGIN", true);
            return new LoginResult { Exitoso = true, Usuario = usuario };
            
        }
    }
}