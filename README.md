# MassTransitPoc

A .NET 8 Web API sample project demonstrating advanced message processing, fault handling, and replay using [MassTransit](https://masstransit.io/) with RabbitMQ and PostgreSQL.

## Features

- **Message Publishing & Consuming**
  - Publishes and consumes `SampleMessage1` messages via RabbitMQ.
  - Supports batch processing and random consumer failure simulation for testing.
- **Fault Handling**
  - Faulted messages are captured and stored in the database (`FaultMessages` table).
  - Fault messages can be queried, marked as replayable, and replayed in batches.
- **Retry & Redelivery**
  - Configurable retry and redelivery policies for both main and fault queues.
- **Queue Management**
  - Max queue length and overflow strategies.
  - Kill switch for consumer health.
- **API Endpoints**
  - Swagger UI for easy exploration and testing.

## Technologies

- .NET 8 (C# 12)
- MassTransit
- RabbitMQ
- PostgreSQL (via Entity Framework Core)
- ASP.NET Core Web API

## Setup

1. **Clone the repository**
2. **Configure the database**
   - Set your PostgreSQL connection string in `appsettings.json` under `ConnectionStrings:DefaultConnection`.

3. **RabbitMQ**
   - Ensure RabbitMQ is running locally (`localhost:5672`).
   - Default credentials: `guest` / `guest`.

4. **Run database migrations**
   - The app will auto-migrate on startup.

5. **Build and run**
6. **Access Swagger UI**
   - Navigate to `http://localhost:<port>/swagger` for API documentation and testing.

## API Endpoints

### Message Publishing

- `POST /api/messages`
  - Publishes one or more `SampleMessage1` messages to the queue.
  - Body:
- Query: `noOfTimes` (optional, default: 1)

### Fault Message Management

- `GET /api/faultmessages`
- Query fault messages with optional filters:
  - `queueName`, `exceptionType`, `from`, `to`, `sortBy`, `descending`, `limit`, `includeReplayable`
- Example:  
  `/api/faultmessages?queueName=my-message-queue&includeReplayable=true&limit=10`

- `PATCH /api/faultmessages/{id}/replayable`
- Mark a fault message as replayable.

- `POST /api/faultmessages/replay`
- Replay all fault messages marked as replayable in batches of 10.
- Successfully replayed messages are removed from the database.

## Configuration

- **AppConfiguration** (see source for options):
- `useRetry`, `retryCount`, `failRandomly`, `infiniteRetryForFaultMessages`, `queueEndpont`

## Development Notes

- Consumer failures can be simulated for testing fault handling.
- Fault messages are stored with payload and exception details.
- Replay logic sends messages in batches and cleans up the database.
- All key actions and errors are logged.

## License

MIT

---

**For more details, see the source code and Swagger UI.**
