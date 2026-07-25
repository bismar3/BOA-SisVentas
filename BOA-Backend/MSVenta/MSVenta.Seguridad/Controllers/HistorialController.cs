using Microsoft.AspNetCore.Mvc;
using MSVenta.Seguridad.Services;
using System;
using System.Threading.Tasks;

namespace MSVenta.Seguridad.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistorialController:ControllerBase
    {
        private readonly IHistorialService _historialService;
        public HistorialController(IHistorialService historialService)
        {
            _historialService = historialService;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllHistorial()
        {
            try
            {
                var items = await _historialService.GetAllHistorial();
                    return Ok(items);
            }
            catch (Exception ex) { 
            return StatusCode(500, new {message  = ex.Message});
            }

        }
        [HttpGet("usuario/{id}")]
        public async Task<ActionResult> GetHistorialByUsuario(int id)
        {
            try
            {
                var items = await _historialService.GetHistorialByUsuario(id);
                return Ok(items);
            }
            catch (Exception ex) 
            { 
             return StatusCode(500, new { message = ex.Message});   
            }

        }
       
    }
}
