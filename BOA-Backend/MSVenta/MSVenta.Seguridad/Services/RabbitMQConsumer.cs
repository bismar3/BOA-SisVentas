
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MSVenta.Seguridad.Models;
using MSVenta.Seguridad.Repositories;

namespace MSVenta.Seguridad.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private static readonly string HOSTNAME = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        private const string QUEUE_BITACORA = "bitacora.registro";
        private readonly IServiceProvider _serviceProvider;

        public RabbitMQConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = HOSTNAME };
            var connection = await factory.CreateConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: QUEUE_BITACORA,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var mensaje = Encoding.UTF8.GetString(body);

                try
                {
                    var evento = JsonSerializer.Deserialize<JsonElement>(mensaje);

                    var bitacora = new Bitacora
                    {
                        Tabla = LeerString(evento, "Tabla"),
                        Transaccion = LeerString(evento, "Transaccion"),
                        ID_Usuario = LeerInt(evento, "ID_Usuario"),
                        fecha = LeerFecha(evento, "Fecha"),
                        hora = LeerHora(evento, "Hora"),
                        NroRegistro = LeerInt(evento, "NroRegistro")
                    };

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ContextDatabase>();
                    context.Bitacoras.Add(bitacora);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"[Bitacora] Registro guardado: {bitacora.Tabla} - {bitacora.Transaccion}");

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Bitacora] Error al procesar: {ex.Message}");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QUEUE_BITACORA,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        // Un campo ausente o mal formado nunca debe impedir guardar el resto del registro.
        private static string LeerString(JsonElement evento, string campo)
        {
            if (evento.TryGetProperty(campo, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();

            Console.WriteLine($"[Bitacora] Campo '{campo}' ausente o no es texto; se guarda null.");
            return null;
        }

        private static int? LeerInt(JsonElement evento, string campo)
        {
            if (evento.TryGetProperty(campo, out var prop) &&
                prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetInt32(out var valor))
                return valor;

            Console.WriteLine($"[Bitacora] Campo '{campo}' ausente o no es numérico; se guarda null.");
            return null;
        }

        private static DateTime? LeerFecha(JsonElement evento, string campo)
        {
            if (evento.TryGetProperty(campo, out var prop) &&
                prop.ValueKind == JsonValueKind.String &&
                prop.TryGetDateTime(out var valor))
                return valor;

            Console.WriteLine($"[Bitacora] Campo '{campo}' ausente o no es una fecha válida; se guarda null.");
            return null;
        }

        private static TimeSpan? LeerHora(JsonElement evento, string campo)
        {
            if (!evento.TryGetProperty(campo, out var prop))
            {
                Console.WriteLine($"[Bitacora] Campo '{campo}' ausente; se guarda null.");
                return null;
            }

            // Formato esperado: "HH:mm:ss".
            if (prop.ValueKind == JsonValueKind.String && TimeSpan.TryParse(prop.GetString(), out var valor))
                return valor;

            // Tolera los mensajes viejos, donde un TimeSpan quedó serializado como objeto.
            if (prop.ValueKind == JsonValueKind.Object &&
                prop.TryGetProperty("Ticks", out var ticks) &&
                ticks.TryGetInt64(out var valorTicks))
                return TimeSpan.FromTicks(valorTicks);

            Console.WriteLine($"[Bitacora] Campo '{campo}' con formato no reconocido; se guarda null.");
            return null;
        }
    }
}