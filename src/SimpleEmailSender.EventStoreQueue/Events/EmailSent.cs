using System;

namespace SimpleEmailSender.EventStoreQueue.Events
{
    public class EmailSent
    {
        public EmailSent(int position, Guid emailId, DateTime timestamp)
        {
            this.Position = position;
            this.EmailId = emailId;
            this.Timestamp = timestamp;
        }

        public int Position { get; }
        public Guid EmailId { get; }
        public DateTime Timestamp { get; }
    }
}
