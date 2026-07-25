using BOA.Comercial.Models;
using BOA.Comercial.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BOA.Comercial.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private const string QUEUE_USUARIO = "usuario.registrado";
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMQConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = "localhost" };
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: QUEUE_USUARIO,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var mensaje = Encoding.UTF8.GetString(body);
                        var evento = JsonSerializer.Deserialize<JsonElement>(mensaje);

                        Console.WriteLine($"[RabbitMQ] Evento recibido: {mensaje}");

                        var usuarioId = evento.GetProperty("UsuarioId").GetInt32();

                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ContextDatabase>();

                        // Evitar duplicados si el mensaje se reprocesa
                        var existente = await context.Clientes.FirstOrDefaultAsync(c => c.Usuario_Id == usuarioId);
                        if (existente != null)
                        {
                            Console.WriteLine($"[Cliente] Ya existe un cliente para el usuario {usuarioId}. Se omite.");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            return;
                        }

                        // Fecha_Nacimiento es obligatoria en Cliente: si el evento no la trae, no inventamos un valor.
                        DateTime fechaNacimiento;
                        if (!evento.TryGetProperty("Fecha_Nacimiento", out var fechaProp) ||
                            fechaProp.ValueKind == JsonValueKind.Null ||
                            !fechaProp.TryGetDateTime(out fechaNacimiento))
                        {
                            Console.WriteLine($"[Cliente] No se pudo crear: el usuario {usuarioId} no tiene fecha de nacimiento.");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            return;
                        }

                        var cliente = new Cliente
                        {
                            Nombre = evento.GetProperty("Nombre").GetString(),
                            Apellido = evento.GetProperty("Apellido").GetString(),
                            Email = evento.GetProperty("Email").GetString(),
                            Documento_Identidad = evento.GetProperty("Documento_Identidad").GetString(),
                            Telefono = evento.GetProperty("Telefono").GetString(),
                            Fecha_Nacimiento = fechaNacimiento,
                            Usuario_Id = usuarioId,
                            Estado = "Activo"
                        };

                        context.Clientes.Add(cliente);
                        await context.SaveChangesAsync();

                        Console.WriteLine($"[Comercial] Cliente creado para usuario {usuarioId}: {cliente.Nombre} {cliente.Apellido}");

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Comercial] Error al procesar mensaje: {ex.Message}");
                    }
                };

                await _channel.BasicConsumeAsync(QUEUE_USUARIO, false, consumer);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Error al conectar: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }
}
