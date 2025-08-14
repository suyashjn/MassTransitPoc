using MassTransit;

namespace MassTransitPoc.Observers
{
    public class SendObserver : ISendObserver
    {
        private readonly ILogger<SendObserver> _logger;

        public SendObserver(ILogger<SendObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task PostSend<T>(SendContext<T> context) where T : class
        {
            _logger.LogDebug("Sent message {MessageId} of type {0} to {1}", context.MessageId, typeof(T).Name, context.DestinationAddress);
            return Task.CompletedTask;
        }

        public Task PreSend<T>(SendContext<T> context) where T : class
        {
            _logger.LogDebug("Sending message {MessageId} of type {0} to {1}", context.MessageId, typeof(T).Name, context.DestinationAddress);
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
           _logger.LogWarning(exception, "Send fault for message {MessageId} of type {0}", context.MessageId, typeof(T).Name);
            return Task.CompletedTask;
        }
    }
}
