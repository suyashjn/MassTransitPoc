using System.ComponentModel.DataAnnotations.Schema;

namespace MassTransitPoc.Persistance.Entities
{
    public class FaultMessage
    {
        public int Id { get; set; }
        public string QueueName { get; set; } = null!;
        public string ExceptionType { get; set; } = null!;
        public string ExceptionMessage { get; set; } = null!;
        public string StackTrace { get; set; } = null!;
        [Column(TypeName = "jsonb")]
        public string PayloadJson { get; set; } = null!;
        public DateTime ReceivedAt { get; set; }
        public bool IsReplayable { get; set; } // Indicates if the message can be replayed
    }
}
