using Eventing.EventSourcing;
using Eventing.Log;
using Eventing.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ses.EventStoreQueue.Client;
using SimpleEmailSender.EventStoreQueue;
using System;
using System.Net;
using System.Threading;

namespace SimpleEmailSender.Core.Tests
{
    [TestClass]
    public class ClientTest
    {
        [TestMethod]
        public void CanSendEmail()
        {
            var queueStreamName = "Test" + EventStoreQueueConstants.QueueStreamName;
            var outboxStreamName = "Test" + EventStoreQueueConstants.OutboxStreamName;

            var mainLog = LogManager.GetLoggerFor("TEST");

            // EventStoreSettings
            var eventStoreIp = "127.0.0.1";
            var eventStoreTcpPort = 1113;
            var eventStoreUser = "admin";
            var eventStorePass = "changeit";

            // Conectando el EventStore
            var eventStoreSettings = ConnectionSettings
                                        .Create()
                                        .KeepReconnecting()
                                        .SetHeartbeatTimeout(TimeSpan.FromSeconds(30))
                                        .SetDefaultUserCredentials(new UserCredentials(eventStoreUser, eventStorePass))
                                        .Build();

            var tcp = new IPEndPoint(IPAddress.Parse(eventStoreIp), eventStoreTcpPort);
            var connection = EventStoreConnection.Create(eventStoreSettings, tcp);
            var connectionSource = "local";
            connection.Closed += (s, e) =>
                mainLog.Error($"The {connectionSource} ES connection was closed");
            connection.Connected += (s, e) =>
                mainLog.Log($"The connection with {connectionSource} ES was establised");
            connection.Disconnected += (s, e) =>
                mainLog.Log($"The connection with {connectionSource} ES was lost");
            connection.Reconnecting += (s, e) =>
                mainLog.Log($"Reconnecting with {connectionSource} ES");
            connection.ConnectAsync().Wait();

            var serializer = new JsonSerializer();

            var repo = new EventSourcedRepository(connection, serializer, enablePersistentSnapshotting: true, snapshotInterval: 1);

            var client = new SimpleEmailSenderEsClient(connection, serializer, queueStreamName);

            var mail = EmailFactory.NewFrom("Alexis", "anarvaez@fecoprod.com.py")
                                   .To("Alexis", "anarvaez@fecoprod.com.py")
                                   .Subject("Test")
                                   .Body("Body")
                                   .Build();

            client.Send(mail);

            // Consuming
            var sender = new FakeEmailSender("mail.fecoprod.com.py", 25);
            using (var job = new MailSendingJob(connection, repo, serializer, sender, queueStreamName, outboxStreamName))
            {
                job.Start();

                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }
    }

    public class FakeEmailSender : IEmailSender
    {
        public FakeEmailSender(string host, int port) { }

        public void Send(Envelope mail) { }
    }
}
