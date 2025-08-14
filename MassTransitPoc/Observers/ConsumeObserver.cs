using MassTransit.Events;
using MassTransit;

namespace MassTransitPoc.Observers
{
    public class ConsumeObserver : IConsumeObserver
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsumeObserver> _logger;

        public ConsumeObserver(IServiceProvider serviceProvider, ILogger<ConsumeObserver> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (AppConfiguration.republishFaultEvents)
            {
                if (context.Message.GetType().IsGenericType && context.Message.GetType().GetGenericTypeDefinition() == typeof(FaultEvent<>))
                {
                    var messageType = context.Message.GetType().GetGenericArguments()[0].FullName;
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var publishService = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                        _logger.LogInformation("Re-publishing fault event for message {MessageId} of type {MessageType}",
                            context.MessageId, messageType);
                        await publishService.Publish(context.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to re-publish fault event for message {MessageId} of type {MessageType}",
                            context.MessageId, messageType);
                    }
                }
                else
                {
                    // Do not re-publish fault events for non-FaultEvent messages
                }
            }
        }

        public Task PostConsume<T>(ConsumeContext<T> context) where T : class
        {
            return Task.CompletedTask;
        }

        public Task PreConsume<T>(ConsumeContext<T> context) where T : class
        {
            return Task.CompletedTask;
        }
    }
}
