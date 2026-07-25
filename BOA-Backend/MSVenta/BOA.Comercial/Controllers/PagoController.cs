using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using BOA.Comercial.Models;
using BOA.Comercial.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BOA.Comercial.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagoController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly ITicketService _ticketService;
        private readonly ITransaccionService _transaccionService;
        private readonly IClienteService _clienteService;
        private readonly IBitacoraService _bitacoraService;
        private readonly HttpClient _httpClient;
        private readonly RabbitMQPublisher _rabbitPublisher;

        private readonly string _libelulaAppKey;
        private readonly string _libelulaUrl;
        private readonly string _libelulaVerificarUrl;
        private readonly string _callbackUrl;
        private readonly string _urlRetorno;
        private readonly string _operacionesUrl;

        public PagoController(
            IVentaService ventaService,
            ITicketService ticketService,
            ITransaccionService transaccionService,
            IClienteService clienteService,
            IBitacoraService bitacoraService,
            IConfiguration configuration)
        {
            _ventaService = ventaService;
            _ticketService = ticketService;
            _transaccionService = transaccionService;
            _clienteService = clienteService;
            _bitacoraService = bitacoraService;
            _httpClient = new HttpClient();
            _rabbitPublisher = new RabbitMQPublisher();

            _libelulaAppKey = configuration["Libelula:AppKey"];
            _libelulaUrl = configuration["Libelula:UrlRegistrar"];
            _libelulaVerificarUrl = configuration["Libelula:UrlVerificar"];
            _urlRetorno = configuration["Libelula:UrlRetorno"];
            _operacionesUrl = configuration["ServiciosUrls:Operaciones"];

            var callbackBaseUrl = configuration["Libelula:CallbackBaseUrl"];
            _callbackUrl = callbackBaseUrl.TrimEnd('/') + "/api/pago/callback";
        }

        [HttpPost("registrar")]
        public async Task<ActionResult> RegistrarPago([FromBody] PagoRequest request)
        {
            try
            {
                var cliente = await _clienteService.GetByUsuarioId(request.ClienteId);
                if (cliente == null)
                    return NotFound(new { message = "No existe un cliente asociado a este usuario." });

                var codigoVenta = "VTA-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                var venta = new Venta
                {
                    Codigo_Venta = codigoVenta,
                    Programacion_Vuelo_Id = request.ProgramacionVueloId,
                    Cliente_Id = cliente.Id,
                    Monto_Total = request.MontoTotal,
                    Estado = "Pendiente"
                };
                var ventaCreada = await _ventaService.Create(venta);


                
                var ahora = DateTime.Now;
                _rabbitPublisher.PublicarBitacora(new
                {
                    Tabla = "ventas",
                    Transaccion = "INSERT",
                    ID_Usuario = request.ClienteId,
                    Fecha = ahora,
                    // Como string: System.Text.Json serializa TimeSpan como objeto, no como "HH:mm:ss".
                    Hora = ahora.ToString("HH:mm:ss"),
                    NroRegistro = ventaCreada.Id
                });

        



                var numeroTicket = "TKT-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var ticket = new Ticket
                {
                    Numero_Ticket = numeroTicket,
                    Venta_Id = ventaCreada.Id,
                    Asiento_Id = request.AsientoId,
                    Estado = "Emitido",
                    Pasajero_Nombre = request.PasajeroNombre,
                    Pasajero_Apellido = request.PasajeroApellido
                };
                await _ticketService.Create(ticket);

                var payload = new
                {
                    appkey = _libelulaAppKey,
                    email_cliente = cliente.Email,
                    identificador = codigoVenta,
                    callback_url = _callbackUrl,
                    url_retorno = _urlRetorno,
                    descripcion = $"Compra de ticket BOA - Vuelo {request.ProgramacionVueloId}",
                    nombre_cliente = cliente.Nombre,
                    apellido_cliente = cliente.Apellido,
                    ci = cliente.Documento_Identidad,
                    fecha_vencimiento = DateTime.Now.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss"),
                    lineas_detalle_deuda = request.Asientos
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync(_libelulaUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var libelulaResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

                    if (libelulaResponse.GetProperty("error").GetInt32() == 0)
                    {
                        var idTransaccion = libelulaResponse.GetProperty("id_transaccion").GetString();

                        var transaccion = new Transaccion
                        {
                            Venta_Id = ventaCreada.Id,
                            Referencia = idTransaccion,
                            Monto = request.MontoTotal,
                            Metodo_Pago = "QR",
                            Estado = "Pendiente"
                        };
                        await _transaccionService.Create(transaccion);

                        return Ok(new
                        {
                            ventaId = ventaCreada.Id,
                            codigoVenta = codigoVenta,
                            idTransaccion = idTransaccion,
                            urlPasarela = libelulaResponse.GetProperty("url_pasarela_pagos").GetString(),
                            qrUrl = libelulaResponse.GetProperty("qr_simple_url").GetString(),
                            modo = "libelula"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pago] Libélula no disponible ({ex.Message}); se usa modo simulación.");
                }

                // Fallback: si Libélula falla (excepción o error != 0), la venta queda en modo
                // simulación. Se confirma luego con "Verificar pago manualmente" → callback.
                var simId = "SIM-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

                var transaccionSim = new Transaccion
                {
                    Venta_Id = ventaCreada.Id,
                    Referencia = simId,
                    Monto = request.MontoTotal,
                    Metodo_Pago = "QR",
                    Estado = "Pendiente"
                };
                await _transaccionService.Create(transaccionSim);

                return Ok(new
                {
                    ventaId = ventaCreada.Id,
                    codigoVenta = codigoVenta,
                    idTransaccion = simId,
                    urlPasarela = (string)null,
                    qrUrl = (string)null,
                    modo = "simulacion"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback(
            [FromQuery] string transaction_id,
            [FromQuery] string invoice_id,
            [FromQuery] string invoice_url)
        {
            try
            {
                var ventas = await _ventaService.GetAll();
                var venta = ventas.FirstOrDefault(v => v.Codigo_Venta == transaction_id);

                if (venta == null)
                    return NotFound(new { message = "Venta no encontrada para: " + transaction_id });

                // Actualizar venta a Confirmada
                venta.Estado = "Confirmada";
                await _ventaService.Update(venta);

                // Bitácora: venta confirmada (UPDATE) — misma vía RabbitMQ que el INSERT.
                // ID_Usuario se resuelve al id de usuario (no el de cliente), igual que el INSERT.
                var clienteVenta = await _clienteService.GetById(venta.Cliente_Id);
                var ahoraConfirmada = DateTime.Now;
                _rabbitPublisher.PublicarBitacora(new
                {
                    Tabla = "ventas",
                    Transaccion = "UPDATE",
                    ID_Usuario = clienteVenta?.Usuario_Id,
                    Fecha = ahoraConfirmada,
                    Hora = ahoraConfirmada.ToString("HH:mm:ss"),
                    NroRegistro = venta.Id
                });

                // Actualizar transacción a Aprobado
                var transacciones = await _transaccionService.GetAll();
                var transaccion = transacciones.FirstOrDefault(t => t.Venta_Id == venta.Id);
                if (transaccion != null)
                {
                    transaccion.Estado = "Aprobado";
                    await _transaccionService.Update(transaccion);
                }

                // Ticket ya fue creado en RegistrarPago — marcar asiento como ocupado
                var tickets = await _ticketService.GetByVentaId(venta.Id);
                var ticket = tickets.FirstOrDefault();
                if (ticket != null && ticket.Asiento_Id.HasValue)
                {
                    try
                    {
                        await _httpClient.PutAsync(
                            $"{_operacionesUrl}/api/asiento/{ticket.Asiento_Id}/ocupar",
                            null
                        );
                    }
                    catch
                    {
                        Console.WriteLine("[Asiento] No se pudo marcar como ocupado");
                    }
                }

                // Publicar evento en RabbitMQ
                _rabbitPublisher.PublicarPagoConfirmado(new
                {
                    CodigoVenta = venta.Codigo_Venta,
                    ClienteId = venta.Cliente_Id,
                    ProgramacionVueloId = venta.Programacion_Vuelo_Id,
                    Monto = venta.Monto_Total,
                    Fecha = DateTime.Now,
                    InvoiceId = invoice_id
                });

                return Ok(new
                {
                    message = "Pago confirmado correctamente.",
                    codigoVenta = venta.Codigo_Venta,
                    invoiceId = invoice_id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("verificar/{transactionId}")]
        public async Task<ActionResult> VerificarPago(string transactionId)
        {
            try
            {
                if (transactionId.StartsWith("SIM-"))
                    return Ok(new { pagado = true, modo = "simulacion" });

                var payload = new
                {
                    appkey = _libelulaAppKey,
                    fecha_inicial = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    fecha_final = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss")
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_libelulaVerificarUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                var libelulaResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

                if (libelulaResponse.GetProperty("error").GetInt32() == 0)
                {
                    var datos = libelulaResponse.GetProperty("datos");
                    foreach (var pago in datos.EnumerateArray())
                    {
                        if (pago.GetProperty("id_transaccion").GetString() == transactionId)
                            return Ok(new { pagado = true, modo = "libelula" });
                    }
                }

                return Ok(new { pagado = false, modo = "libelula" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class PagoRequest
    {
        public int ClienteId { get; set; }
        public int ProgramacionVueloId { get; set; }
        public decimal MontoTotal { get; set; }
        public int? AsientoId { get; set; }
        public string PasajeroNombre { get; set; }
        public string PasajeroApellido { get; set; }
        public List<AsientoDetalle> Asientos { get; set; }
    }

    public class AsientoDetalle
    {
        public string concepto { get; set; }
        public int cantidad { get; set; }
        public decimal costo_unitario { get; set; }
        public decimal descuento_unitario { get; set; }
    }
}