using System.Text.Json;
using MassTransit;
using MassTransit.Batching;
using MassTransit.Internals;
using MassTransitPoc.Utilites;

namespace MassTransitPoc.Filters
{
    public class ConsumeFilter<T> : IFilter<ConsumeContext<T>>
        where T : class
    {
        private readonly ILogger<ConsumeFilter<T>> _logger;

        public ConsumeFilter(ILogger<ConsumeFilter<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Probe(ProbeContext context) { }

        public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            if (context.Message.GetType().IsGenericType && context.Message.GetType().GetGenericTypeDefinition() == typeof(MessageBatch<>))
            {
                var batchType = typeof(MessageBatch<>).MakeGenericType(context.Message.GetType().GetGenericArguments()[0]);
                int messageCount = (int)batchType.GetProperty("Length")!.GetValue(context.Message)!;
                Type messageType = batchType.GetGenericArguments()[0];

                _logger.LogDebug("Consuming batch of {MessageCount} messages of type {MessageType}", messageCount, messageType.GetTypeName());
                try
                {
                    await next.Send(context);

                    _logger.LogDebug("Batch of {MessageCount} messages of type {MessageType} consumed successfully",
                        messageCount, messageType.GetTypeName());
                    MessagesTracker.IncrementSuccessCounter(messageType, messageCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error consuming batch of {MessageCount} messages of type {MessageType}",
                        messageCount, messageType.GetTypeName());
                    MessagesTracker.IncrementFaliureCounter(messageType, messageCount);
                    // Optionally, you can rethrow or handle the exception as needed
                    throw;
                }
                finally
                {
                   _logger.LogInformation("Finished processing batch of {MessageCount} messages of type {MessageType}",
                        messageCount, messageType.GetTypeName());
                    _logger.LogInformation("For type {MessageType} - Success {SuccessCount}, Faliure {FaliureCount}", messageType.GetTypeName(),
                        MessagesTracker.SuccessCount(messageType), MessagesTracker.FaliureCount(messageType));
                }
            }
            else
            {
                _logger.LogDebug("Consuming message {MessageId} of type {MessageType} with payload {Message}",
                    context.MessageId, context.Message.GetType(), JsonSerializer.Serialize(context.Message));

                try
                {
                    await next.Send(context);

                    _logger.LogDebug("Message {MessageId} consumed successfully", context.MessageId);
                    MessagesTracker.IncrementSuccessCounter(context.Message.GetType());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error consuming message {MessageId} of type {MessageType}",
                        context.MessageId, context.Message.GetType().Name);

                    if (!AppConfiguration.useRetry || context.GetRedeliveryCount() == AppConfiguration.retryCount)
                    {
                        MessagesTracker.IncrementFaliureCounter(context.Message.GetType());
                    }

                    // Optionally, you can rethrow or handle the exception as needed
                    throw;
                }
                finally
                {
                    _logger.LogInformation("Finished processing message {MessageId} of type {MessageType}",
                        context.MessageId, context.Message.GetType());
                    _logger.LogInformation("For type {MessageType} - Success {SuccessCount}, Faliure {FaliureCount}", context.Message.GetType().Name,
                        MessagesTracker.SuccessCount(context.Message.GetType()), MessagesTracker.FaliureCount(context.Message.GetType()));
                }
            }
        }
    }
}
