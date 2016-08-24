using Eventing.EventSourcing;
using SimpleEmailSender.EventStoreQueue.Events;
using System;

namespace SimpleEmailSender.EventStoreQueue
{
    public partial class Outbox : EventSourced
    {
        private int currentPosition = -1;

        public Outbox(string outboxStreamName, DateTime now) : this()
        {
            this.RaiseEvent(new OutboxInitialized(outboxStreamName, now));
        }

        private Outbox()
        {
            this.Register<OutboxInitialized>(e => this.StreamName = e.OutboxStreamName);
            this.Register<EmailSent>(e => this.currentPosition = e.Position);
        }

        public override ISnapshot TakeSnapshot()
        {
            return new OutboxSnapshot(this.StreamName, this.Version, this.currentPosition);
        }

        public override T Rehydrate<T>(ISnapshot snapshot)
        {
            var state = (OutboxSnapshot)snapshot;
            this.currentPosition = state.CurrentPosition;
            return base.Rehydrate<T>(snapshot);
        }
    }

    public partial class Outbox
    {
        public void SendEmail(int position, Guid emailId, Envelope envelope, IEmailSender emailSender)
        {
            emailSender.Send(envelope);
            base.RaiseEvent(new EmailSent(position, emailId, DateTime.Now));
        }
    }

    public partial class Outbox
    {
        public int? CurrentPosition => this.currentPosition < 0 ? default(int?) : this.currentPosition;
    }

    public class OutboxSnapshot : Snapshot
    {
        public OutboxSnapshot(string streamName, int version, int currentPosition) : base(streamName, version)
        {
            this.CurrentPosition = currentPosition;
        }

        public int CurrentPosition { get; }
    }
}
