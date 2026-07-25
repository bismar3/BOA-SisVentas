using Microsoft.AspNetCore.Mvc;
using MSVenta.Seguridad.Services;
using System;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraService _bitacoraService;

        public BitacoraController(IBitacoraService bitacoraService)
        {
            _bitacoraService = bitacoraService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var items = await _bitacoraService.GetAll();
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
                var item = await _bitacoraService.GetById(id);
                if (item == null)
                    return NotFound(new { message = "Registro de bitácora no encontrado." });
                return Ok(item);
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
                var eliminado = await _bitacoraService.Delete(id);
                if (!eliminado)
                    return NotFound(new { message = "Registro de bitácora no encontrado." });
                return Ok(new { message = "Registro de bitácora eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
