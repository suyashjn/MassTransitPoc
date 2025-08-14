using MassTransit;
using MassTransitPoc;
using MassTransitPoc.Consumers;
using MassTransitPoc.Filters;
using MassTransitPoc.Models;
using MassTransitPoc.Observers;
using MassTransitPoc.Persistance;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SampleMessage1Consumer>();
    x.AddConsumer<FaultConsumer>(cfg =>
    {
        cfg.Options<BatchOptions>(options => options
            .SetMessageLimit(10)
            .SetTimeLimit(s: 20)
            .SetTimeLimitStart(BatchTimeLimitStart.FromLast)
            .SetConcurrencyLimit(10)
        );
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure endpoints and dead letter queue
        cfg.ReceiveEndpoint("my-message-queue", e =>
        {
            // Configure Max Queue Length
            e.SetQueueArgument("x-max-length", 100); // max 100 messages
            e.SetQueueArgument("x-overflow", "reject-publish");
            // or "drop-head" if you prefer dropping oldest instead of rejecting new

            e.DiscardFaultedMessages(); // Message that faults should not be moved to _error queue

            if (AppConfiguration.useRetry)
            {
                e.UseDelayedRedelivery(r => r.Exponential(
                    retryLimit: AppConfiguration.retryCount,
                    minInterval: TimeSpan.FromMilliseconds(100),
                    maxInterval: TimeSpan.FromMilliseconds(1000),
                    intervalDelta: TimeSpan.FromMilliseconds(200)
                ));
            }
            else
            {
                e.UseMessageRetry(e => e.None());
            }

            e.UseKillSwitch(options => options
            .SetTrackingPeriod(m: 5)            // look back 5 minute
            .SetActivationThreshold(50)         // after 10 messages processed
            .SetTripThreshold(5)                // trip if >5% messages fail
            .SetRestartTimeout(m: 5));          // wait 5 min before trying again

            e.UseConsumeFilter(typeof(ConsumeFilter<>), context);

            e.ConfigureConsumer<SampleMessage1Consumer>(context);
        });

        cfg.ReceiveEndpoint("my-message-queue-fault", e =>
        {
            // Bind fault message events to this queue
            e.Bind<Fault<SampleMessage1>>();

            e.DiscardFaultedMessages();   // Message that faults should not be moved to _error queue

            if (AppConfiguration.infiniteRetryForFaultMessages)
            {
                e.UseDelayedRedelivery(r => r.Exponential(
                    retryLimit: int.MaxValue,
                    minInterval: TimeSpan.FromMilliseconds(100),
                    maxInterval: TimeSpan.FromMilliseconds(1000),
                    intervalDelta: TimeSpan.FromMilliseconds(200)
                ));
            }
            else
            { 
                e.UseMessageRetry(e => e.None()); 
            }

            e.UseKillSwitch(options => options
                .SetTrackingPeriod(m: 1)        // look back 1 minute
                .SetActivationThreshold(1)      // after 2 messages processed
                .SetTripThreshold(0.15)         // trip if 50% messages fail
                .SetRestartTimeout(m: 5));      // wait 5 min before trying again

            e.UseConsumeFilter(typeof(ConsumeFilter<>), context);

            e.ConfigureConsumer<FaultConsumer>(context);
        });
    });
});

builder.Services.AddSendObserver<SendObserver>();
builder.Services.AddConsumeObserver<ConsumeObserver>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database.");
    }
}


app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

await app.RunAsync();

