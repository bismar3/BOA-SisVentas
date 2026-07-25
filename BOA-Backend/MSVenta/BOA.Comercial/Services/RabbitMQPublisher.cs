using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace BOA.Comercial.Services
{
    public class RabbitMQPublisher
    {
        private const string HOSTNAME = "localhost";
        private const string QUEUE_PAGO = "pago.confirmado";
        private const string QUEUE_BITACORA = "bitacora.registro";

        public void PublicarPagoConfirmado(object evento)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = HOSTNAME };
                using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.QueueDeclareAsync(
                    queue: QUEUE_PAGO,
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
                    routingKey: QUEUE_PAGO,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();

                Console.WriteLine($"[RabbitMQ] Evento publicado: {mensaje}");
            }
            catch (Exception ex)
            {
                // Si RabbitMQ falla, Comercial sigue funcionando
                Console.WriteLine($"[RabbitMQ] Error al publicar (no crítico): {ex.Message}");
            }
        }


        public void PublicarBitacora(object evento)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = HOSTNAME };
                using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.QueueDeclareAsync(
                    queue: QUEUE_BITACORA,
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
                    routingKey: QUEUE_BITACORA,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();

                Console.WriteLine($"[RabbitMQ] Evento publicado: {mensaje}");
            }
            catch (Exception ex)
            {
                // Si RabbitMQ falla, Comercial sigue funcionando
                Console.WriteLine($"[RabbitMQ] Error al publicar (no crítico): {ex.Message}");
            }
        }


        public void EliminarBitacora(object evento)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = HOSTNAME };
                using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.QueueDeclareAsync(
                    queue: QUEUE_BITACORA,
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
                    routingKey: QUEUE_BITACORA,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();

                Console.WriteLine($"[RabbitMQ] Evento publicado: {mensaje}");
            }
            catch (Exception ex)
            {
                // Si RabbitMQ falla, Comercial sigue funcionando
                Console.WriteLine($"[RabbitMQ] Error al publicar (no crítico): {ex.Message}");
            }
        }
    }
}