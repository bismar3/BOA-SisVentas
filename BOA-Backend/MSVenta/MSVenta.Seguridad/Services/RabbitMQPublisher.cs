using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace MSVenta.Seguridad.Services
{
    public class RabbitMQPublisher
    {
        private const string HOSTNAME = "localhost";
        private const string QUEUE_USUARIO = "usuario.registrado";

        public void PublicarUsuarioRegistrado(object evento)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = HOSTNAME };
                using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.QueueDeclareAsync(
                    queue: QUEUE_USUARIO,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                ).GetAwaiter().GetResult();

                var mensaje = JsonSerializer.Serialize(evento);
                var body = Encoding.UTF8.GetBytes(mensaje);

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QUEUE_USUARIO,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();

                Console.WriteLine($"[RabbitMQ] Evento publicado: {mensaje}");
            }
            catch (Exception ex)
            {
                // Si RabbitMQ falla, Seguridad sigue funcionando
                Console.WriteLine($"[RabbitMQ] Error al publicar (no crítico): {ex.Message}");
            }
        }
    }
}
