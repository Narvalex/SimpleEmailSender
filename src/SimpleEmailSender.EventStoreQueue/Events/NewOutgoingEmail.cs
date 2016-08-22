using System;

namespace SimpleEmailSender.EventStoreQueue.Events
{
    public class NewOutgoingEmail
    {
        public NewOutgoingEmail(Guid emailId, Envelope envelope, DateTime postDateTime)
        {
            this.EmailId = emailId;
            this.Envelope = envelope;
            this.PostDateTime = postDateTime;
        }

        public Guid EmailId { get; }
        public Envelope Envelope { get; }
        public DateTime PostDateTime { get; }
    }
}
