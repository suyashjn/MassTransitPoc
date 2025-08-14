using System.Text.Json;
using MassTransit;
using MassTransitPoc.Models;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitPoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(ISendEndpointProvider sendEndpointProvider, ILogger<MessagesController> logger)
        {
            _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SampleMessage1 message, int noOfTimes = 1)
        {
            try
            {
                var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri(AppConfiguration.queueEndpont));
                await endpoint.SendBatch(Enumerable.Range(0, noOfTimes).Select(_ => DeepClone(message)));
            }
            catch(RabbitMQ.Client.Exceptions.PublishException ex)
            {
                _logger.LogCritical(ex, "Failed to publish message of type {MessageType}", message.GetType().Name);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { 
                        Error = "Failed to publish messages", 
                        Details = ex.Message 
                    });
            }
            return Accepted();
        }

        private static T DeepClone<T>(T source)
        {
            string jsonString = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(jsonString)!;
        }
    }
}
