using System;

namespace SimpleEmailSender.EventStoreQueue.Events
{
    public class OutboxInitialized
    {
        public OutboxInitialized(string outboxStreamName, DateTime timestamp)
        {
            this.OutboxStreamName = outboxStreamName;
            this.Timestamp = timestamp;
        }

        public string OutboxStreamName { get; }
        public DateTime Timestamp { get; }
    }
}
