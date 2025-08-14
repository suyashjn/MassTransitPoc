using System;
using MassTransit;
using MassTransitPoc.Models;

namespace MassTransitPoc.Consumers
{
    public class SampleMessage1Consumer : IConsumer<SampleMessage1>
    {
        private readonly ILogger<SampleMessage1Consumer> _logger;

        public SampleMessage1Consumer(ILogger<SampleMessage1Consumer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<SampleMessage1> context)
        {
            if (context.Message.Text?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true && 
                (!AppConfiguration.failRandomly || new Random().NextDouble() < 0.3))
            {
                // This exception will cause MassTransit to retry and eventually raise fault event
                _logger.LogDebug("Simulating consumer failure for message {MessageId}", context.MessageId);
                throw new InvalidOperationException("Simulated consumer failure.");
            }
            //await Task.Delay(TimeSpan.FromMinutes(2));    // Do something
            await Task.Delay(100);  // Do something
        }
    }
}
