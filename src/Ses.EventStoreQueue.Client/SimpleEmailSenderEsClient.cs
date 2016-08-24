using Eventing.Serialization;
using Eventing.Utils;
using EventStore.ClientAPI;
using SimpleEmailSender;
using SimpleEmailSender.EventStoreQueue;
using SimpleEmailSender.EventStoreQueue.Events;
using System;

namespace Ses.EventStoreQueue.Client
{
    public class SimpleEmailSenderEsClient : ISimpleEmailSenderClient
    {
        private readonly string queueStreamName;
        private readonly IEventStoreConnection connection;
        private readonly IJsonSerializer serializer;

        public SimpleEmailSenderEsClient(IEventStoreConnection connection, IJsonSerializer serializer, string queueStreamName = EventStoreQueueConstants.QueueStreamName)
        {
            Ensure.NotNull(connection, nameof(connection));
            Ensure.NotNullOrWhiteSpace(queueStreamName, nameof(queueStreamName));
            Ensure.NotNull(serializer, nameof(serializer));

            this.connection = connection;
            this.queueStreamName = queueStreamName;
            this.serializer = serializer;
        }

        public void Send(Envelope email)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));

            Ensure.NotNullOrWhiteSpace(email.From.Name, nameof(email.From.Name));
            Ensure.NotNullOrWhiteSpace(email.From.Address, nameof(email.From.Address));
            Ensure.NotNullOrWhiteSpace(email.To[0].Name, nameof(email.To));
            Ensure.NotNullOrWhiteSpace(email.To[0].Address, nameof(email.To));

            var e = new NewOutgoingEmail(GuidManager.NewGuid(), email, DateTime.Now);
            this.connection.AppendToStreamAsync(this.queueStreamName, ExpectedVersion.Any, this.serializer.Serialize(e.EmailId, e.EmailId, e)).Wait();
        }
    }
}

