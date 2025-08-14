using System.Text.Json;
using MassTransit;
using MassTransitPoc.Models;
using MassTransitPoc.Persistance;
using MassTransitPoc.Persistance.Entities;

namespace MassTransitPoc.Consumers
{
    public class FaultConsumer : IConsumer<Batch<Fault<SampleMessage1>>>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<FaultConsumer> _logger;

        public FaultConsumer(AppDbContext db, ILogger<FaultConsumer> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<Batch<Fault<SampleMessage1>>> context)
        {
            try
            {
                _logger.LogDebug("Consuming batch of faults for type {Type}, Count: {Count}", nameof(SampleMessage1), context.Message.Length);

                var toAddToDb = new List<FaultMessage>();
                for (int i = 0; i < context.Message.Length; i++)
                {
                    ConsumeContext<Fault<SampleMessage1>> message = context.Message[i];
                    var entity = new FaultMessage
                    {
                        QueueName = message.SourceAddress!.ToString(),
                        ExceptionMessage = message.Message.Exceptions.FirstOrDefault()?.Message ?? "Unknown",
                        ExceptionType = message.Message.Exceptions.FirstOrDefault()?.ExceptionType ?? "Unknown",
                        StackTrace = message.Message.Exceptions.FirstOrDefault()?.StackTrace ?? "Unknown",
                        PayloadJson = JsonSerializer.Serialize(message.Message.Message),
                        ReceivedAt = DateTime.UtcNow,
                        IsReplayable = false   // Set your logic here
                    };
                    toAddToDb.Add(entity);
                }

                _logger.LogDebug("Adding {Count} fault messages to the database", toAddToDb.Count);

                await _db.FaultMessages.AddRangeAsync(toAddToDb);
                await _db.SaveChangesAsync();

                _logger.LogDebug("Saved {Count} fault messages to the database", toAddToDb.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consuming batch of faults for {Type}", nameof(SampleMessage1));
                // Optionally, you can rethrow or handle the exception as needed
                throw;
            }
        }
    }
}
