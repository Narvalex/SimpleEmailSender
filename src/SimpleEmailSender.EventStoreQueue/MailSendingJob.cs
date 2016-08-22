using Eventing.EventSourcing;
using Eventing.Log;
using Eventing.Serialization;
using Eventing.Utils;
using EventStore.ClientAPI;
using SimpleEmailSender.EventStoreQueue.Events;
using System;
using System.Threading;

namespace SimpleEmailSender.EventStoreQueue
{
    public class MailSendingJob : IDisposable
    {
        private readonly Eventing.Log.ILogger log = LogManager.GetLoggerFor<MailSendingJob>();
        private readonly IEventStoreConnection connection;
        private readonly IEventSourcedRepository repo;
        private readonly IJsonSerializer serializer;
        private readonly IEmailSender sender;
        private EventStoreStreamCatchUpSubscription sub;
        private readonly string queueStreamName, outboxStreamName;

        public MailSendingJob(IEventStoreConnection connection, IEventSourcedRepository repo, IJsonSerializer serializer, IEmailSender sender, string queueStreamName, string outboxStreamName)
        {
            Ensure.NotNull(connection, nameof(connection));
            Ensure.NotNull(repo, nameof(repo));
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.NotNull(sender, nameof(sender));

            Ensure.NotNullOrWhiteSpace(queueStreamName, nameof(queueStreamName));
            Ensure.NotNullOrWhiteSpace(outboxStreamName, nameof(outboxStreamName));

            this.connection = connection;
            this.repo = repo;
            this.queueStreamName = queueStreamName;
            this.outboxStreamName = outboxStreamName;
            this.serializer = serializer;
            this.sender = sender;
        }

        public void Start()
        {
            this.log.Log($"Starting mail sender...");

            var outbox = this.repo.Get<Outbox>(this.outboxStreamName);
            if (outbox == null)
                outbox = new Outbox(this.outboxStreamName, DateTime.Now);

            this.sub = this.connection.SubscribeToStreamFrom(this.queueStreamName, outbox.CurrentPosition, CatchUpSubscriptionSettings.Default,
                (s, rawEvent) =>
                {
                    var e = this.serializer.Deserialize(rawEvent) as NewOutgoingEmail;
                    if (e != null)
                    {
                        var env = e.Envelope;
                        if (env == null)
                            this.log.Trace($"Ignoring null envelope in event #{rawEvent.OriginalEventNumber}");
                        else
                        {
                            outbox.SendEmail(rawEvent.OriginalEventNumber, e.EmailId, e.Envelope, this.sender);
                            this.repo.Save(outbox);
                        }
                    }
                    else
                        this.log.Trace($"Ignoring event #{rawEvent.OriginalEventNumber} {rawEvent.OriginalEvent.EventType}");

                },
                s => this.log.Log($"The mail sending is now processing in real time..."),
                (s, reason, ex) =>
                {
                    this.log.Error($"The subscription was dropeed because of an exception. Reconnecting in 30 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    this.Start();
                });
        }

        public void Dispose()
        {
            if (this.sub != null)
                this.sub.Stop();
        }
    }
}
