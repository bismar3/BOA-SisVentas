using Microsoft.AspNetCore.Mvc;
using BOA.Comercial.Models;
using BOA.Comercial.Services;
using System;
using System.Threading.Tasks;

namespace BOA.Comercial.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var items = await _clienteService.GetAll();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            try
            {
                var item = await _clienteService.GetById(id);
                if (item == null)
                    return NotFound(new { message = "Cliente no encontrado." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpGet("por-usuario/{usuarioId}")]
        public async Task<ActionResult> GetByUsuarioId(int usuarioId)
        {
            try
            {
                var item = await _clienteService.GetByUsuarioId(usuarioId);
                if (item == null)
                    return NotFound(new { message = "No existe un cliente asociado a este usuario." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Cliente cliente)
        {
            try
            {
                await _clienteService.Create(cliente);
                return Ok(new { message = "Cliente creado exitosamente.", data = cliente });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Cliente cliente)
        {
            if (id != cliente.Id)
                return BadRequest(new { message = "El ID no coincide." });
            try
            {
                await _clienteService.Update(cliente);
                return Ok(new { message = "Cliente actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var item = await _clienteService.GetById(id);
                if (item == null)
                    return NotFound(new { message = "Cliente no existe." });
                await _clienteService.Delete(id);
                return Ok(new { message = "Cliente eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}