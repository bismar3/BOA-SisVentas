using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSVenta.Seguridad.Models;
using MSVenta.Seguridad.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly RabbitMQPublisher _rabbitPublisher;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            _rabbitPublisher = new RabbitMQPublisher();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            var usuarios = await _usuarioService.GetAllUsuarios();
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _usuarioService.GetUsuarioById(id);
            return usuario == null ? NotFound() : Ok(usuario);
        }

        [HttpPost]
        public async Task<ActionResult> CreateUsuario(Usuario usuario,[FromQuery] int? adminId)
        {
            try
            {
                // Creación por admin: el rol debe ser explícito (nada de default silencioso).
                if (usuario.Rol_Id == null)
                    return BadRequest(new { message = "Debe asignar un rol al usuario." });

                var createdUsuario = await _usuarioService.CreateUsuario(usuario, adminId);
                return CreatedAtAction(nameof(GetUsuario), new { id = createdUsuario.UserId }, createdUsuario);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("registro")]
        public async Task<ActionResult> Registro([FromBody] Usuario usuario)
        {
            try
            {
                if (usuario.Rol_Id == null)
                    usuario.Rol_Id = 3;
                usuario.Estado = "Activo";
                usuario.Intentos_Fallidos = 0;
                usuario.Veces_Bloqueado = 0;
                var creado = await _usuarioService.CreateUsuario(usuario);

                // Publicar evento autosuficiente para que Comercial cree el Cliente asociado
                _rabbitPublisher.PublicarUsuarioRegistrado(new
                {
                    UsuarioId = creado.UserId,
                    Nombre = creado.Nombre,
                    Apellido = creado.Apellido,
                    Email = creado.Email,
                    Documento_Identidad = creado.Documento_Identidad,
                    Telefono = creado.Telefono,
                    Fecha_Nacimiento = creado.Fecha_Nacimiento
                });

                return Ok(new { message = "Usuario registrado exitosamente.", data = creado });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, Usuario usuario, [FromQuery] int? adminId)
        {
            if (id != usuario.UserId) return BadRequest();
            try
            {
                await _usuarioService.UpdateUsuario(usuario, adminId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UsuarioExists(id)) return NotFound();
                throw;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id, [FromQuery] int? adminId)
        {
            await _usuarioService.DeleteUsuario(id, adminId);
            return NoContent();
        }

        private async Task<bool> UsuarioExists(int id)
        {
            return await _usuarioService.GetUsuarioById(id) != null;
        }

        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateCustomer(int id)
        {
            var customer = await _usuarioService.GetUsuarioById(id);
            if (customer == null)
                return NotFound(new { Message = "Usuario no encontrado." });
            if (customer.UserId != id)
                return BadRequest(new { Message = "El usuario no es válido." });
            return Ok(new { Message = "El usuario es válido." });

            
        }
        
       
    }
}