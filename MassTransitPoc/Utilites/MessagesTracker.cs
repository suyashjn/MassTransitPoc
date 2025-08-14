using System.Collections.Concurrent;

namespace MassTransitPoc.Utilites
{
    public static class MessagesTracker
    {
        static ConcurrentDictionary<Type, int> successCounters = new();
        static ConcurrentDictionary<Type, int> faliureCounters = new();

        public static int SuccessCount(Type type)
        {
            return successCounters.TryGetValue(type, out var counter) ? counter : 0;
        }

        public static int FaliureCount(Type type)
        {
            return faliureCounters.TryGetValue(type, out var counter) ? counter : 0;
        }

        public static void IncrementSuccessCounter(Type type, int incrementBy = 1)
        {
            successCounters.AddOrUpdate(type, 1, (key, oldValue) => oldValue + incrementBy);
        }

        public static void IncrementFaliureCounter(Type type, int incrementBy = 1)
        {
            faliureCounters.AddOrUpdate(type, 1, (key, oldValue) => oldValue + incrementBy);
        }
    }
}
