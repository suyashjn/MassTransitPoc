using System.Text.Json;
using MassTransit;
using MassTransitPoc.Models;
using MassTransitPoc.Persistance;
using MassTransitPoc.Persistance.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassTransitPoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaultMessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly ILogger<FaultMessagesController> _logger;

        public FaultMessagesController(
            AppDbContext db,
            ISendEndpointProvider sendEndpointProvider,
            ILogger<FaultMessagesController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get fault messages with optional filters and limit.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            string? queueName = null,
            string? exceptionType = null,
            DateTime? from = null,
            DateTime? to = null,
            string? sortBy = "receivedAt",
            bool descending = true,
            bool includeReplayable = false,
            int limit = 50)
        {
            _logger.LogInformation("Fetching fault messages with filters: queueName={QueueName}, exceptionType={ExceptionType}, " +
                "from={From}, to={To}, sortBy={SortBy}, descending={Descending}, limit={Limit}, includeReplayable={IncludeReplayable}",
                queueName, exceptionType, from, to, sortBy, descending, limit, includeReplayable);

            IQueryable<FaultMessage> query = _db.FaultMessages;

            if (!string.IsNullOrWhiteSpace(queueName))
                query = query.Where(f => f.QueueName == queueName);

            if (!string.IsNullOrWhiteSpace(exceptionType))
                query = query.Where(f => f.ExceptionType == exceptionType);

            if (from.HasValue)
                query = query.Where(f => f.ReceivedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(f => f.ReceivedAt <= to.Value);

            // Only include replayable messages if requested
            if (!includeReplayable)
                query = query.Where(f => !f.IsReplayable);

            query = sortBy?.ToLower() switch
            {
                "id" => descending ? query.OrderByDescending(f => f.Id) : query.OrderBy(f => f.Id),
                "queuename" => descending ? query.OrderByDescending(f => f.QueueName) : query.OrderBy(f => f.QueueName),
                "exceptiontype" => descending ? query.OrderByDescending(f => f.ExceptionType) : query.OrderBy(f => f.ExceptionType),
                "receivedat" or _ => descending ? query.OrderByDescending(f => f.ReceivedAt) : query.OrderBy(f => f.ReceivedAt)
            };

            var result = await query.Take(limit).ToListAsync();

            _logger.LogInformation("Fetched {Count} fault messages", result.Count);

            return Ok(result);
        }

        /// <summary>
        /// Mark a fault message as replayable by its ID.
        /// </summary>
        [HttpPatch("{id}/replayable")]
        public async Task<IActionResult> MarkAsReplayable(int id)
        {
            _logger.LogInformation("Marking FaultMessage Id={Id} as replayable", id);

            var faultMessage = await _db.FaultMessages.FindAsync(id);

            if (faultMessage == null)
            {
                _logger.LogWarning("FaultMessage Id={Id} not found", id);
                return NotFound($"FaultMessage with Id={id} not found");
            }

            if (faultMessage.IsReplayable)
            {
                _logger.LogInformation("FaultMessage Id={Id} is already marked as replayable", id);
                return Ok($"FaultMessage Id={id} is already marked as replayable");
            }

            faultMessage.IsReplayable = true;
            await _db.SaveChangesAsync();

            _logger.LogInformation("FaultMessage Id={Id} marked as replayable", id);
            return Ok($"FaultMessage Id={id} marked as replayable");
        }

        /// <summary>
        /// Replay all fault messages with IsReplayable == true in batches of 10 to my-message-queue.
        /// </summary>
        [HttpPost("replay")]
        public async Task<IActionResult> ReplayFaultMessages()
        {
            _logger.LogInformation("Starting replay of fault messages with IsReplayable == true");

            var replayableMessages = await _db.FaultMessages
                .Where(f => f.IsReplayable)
                .ToListAsync();

            if (!replayableMessages.Any())
            {
                _logger.LogWarning("No replayable messages found");
                return Ok("No replayable messages found");
            }

            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri(AppConfiguration.queueEndpont));

            int sentCount = 0;
            try
            {
                // Prepare batches of FaultMessage entities
                var batches = replayableMessages
                    .Chunk(10);

                foreach (var batchEntities in batches)
                {
                    // Deserialize payloads for this batch
                    var payloads = batchEntities
                        .Select(fault => JsonSerializer.Deserialize<SampleMessage1>(fault.PayloadJson)!)
                        .Where(payload => payload != null)
                        .ToArray();

                    if (payloads.Length == 0)
                        continue;

                    await endpoint.SendBatch(payloads!);
                    sentCount += payloads.Length;
                    _logger.LogInformation("Sent batch of {BatchSize} messages to queue", payloads.Length);

                    // Log only Id and PayloadJson before removal
                    foreach (var fault in batchEntities)
                    {
                        _logger.LogInformation("Removing FaultMessage Id={Id}, Payload={PayloadJson} from database", fault.Id, fault.PayloadJson);
                    }

                    // Remove corresponding FaultMessage entries from DB
                    _db.FaultMessages.RemoveRange(batchEntities);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} FaultMessage entries from database", batchEntities.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while replaying fault messages");
                return StatusCode(500, "Error occurred while replaying fault messages");
            }

            _logger.LogInformation("Successfully replayed {Count} messages to my-message-queue in batches of 10", sentCount);

            return Ok($"{sentCount} messages replayed to my-message-queue in batches of 10 and removed from database");
        }
    }
}
