namespace MassTransitPoc
{
    public static class AppConfiguration
    {
        public static readonly bool useRetry = false;
        public static readonly int retryCount = 3;
        public static readonly string queueEndpont = "rabbitmq://localhost/my-message-queue";
        public static readonly bool failRandomly = true;
        public static readonly bool infiniteRetryForFaultMessages = false;
        public static readonly bool republishFaultEvents = true;
    }
}
