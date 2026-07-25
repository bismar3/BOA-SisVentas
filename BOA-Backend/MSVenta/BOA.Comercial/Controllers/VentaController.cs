using Microsoft.AspNetCore.Mvc;
using BOA.Comercial.Models;
using BOA.Comercial.Services;
using System;
using System.Threading.Tasks;

namespace BOA.Comercial.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentaController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly IClienteService _clienteService;
        private readonly RabbitMQPublisher _rabbitPublisher;

        public VentaController(IVentaService ventaService, IClienteService clienteService)
        {
            _ventaService = ventaService;
            _clienteService = clienteService;
            _rabbitPublisher = new RabbitMQPublisher();
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var items = await _ventaService.GetAll();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("detalle")]
        public async Task<ActionResult> GetAllDetalle()
        {
            try
            {
                var items = await _ventaService.GetAllDetalle();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("detalle/cliente/{clienteId}")]
        public async Task<ActionResult> GetDetalleByClienteId(int clienteId)
        {
            try
            {
                var items = await _ventaService.GetDetalleByClienteId(clienteId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult> GetByClienteId(int clienteId)
        {
            try
            {
                var items = await _ventaService.GetByClienteId(clienteId);
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
                var item = await _ventaService.GetById(id);
                if (item == null)
                    return NotFound(new { message = "Venta no encontrada." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Venta venta)
        {
            try
            {
                var result = await _ventaService.Create(venta);
                return Ok(new { message = "Venta creada exitosamente.", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Venta venta)
        {
            if (id != venta.Id)
                return BadRequest(new { message = "El ID no coincide." });
            try
            {
                await _ventaService.Update(venta);
                return Ok(new { message = "Venta actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id, [FromQuery] int? adminId)
        {
            try
            {
                var item = await _ventaService.GetById(id);
                if (item == null)
                    return NotFound(new { message = "Venta no existe." });
                await _ventaService.Delete(id);

                // Bitácora: venta eliminada (DELETE) vía RabbitMQ → consumer en Seguridad.
                // Se registra quién la borró (adminId); si no llega, cae al usuario dueño de la venta.
                var cliente = await _clienteService.GetById(item.Cliente_Id);
                var ahora = DateTime.Now;
                _rabbitPublisher.PublicarBitacora(new
                {
                    Tabla = "ventas",
                    Transaccion = "DELETE",
                    ID_Usuario = adminId ?? cliente?.Usuario_Id,
                    Fecha = ahora,
                    Hora = ahora.ToString("HH:mm:ss"),
                    NroRegistro = item.Id
                });

                return Ok(new { message = "Venta eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}