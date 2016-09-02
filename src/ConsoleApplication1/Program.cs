using Eventing.EventSourcing;
using Eventing.Log;
using Eventing.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using SimpleEmailSender.EventStoreQueue;
using System;
using System.Configuration;
using System.Net;
using System.Runtime.InteropServices;

namespace SimpleEmailSender.Host
{
    class Program
    {
        private static IEventStoreConnection _connection;
        private static Eventing.Log.ILogger _log;

        #region Hiding close window

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        static Program()
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
        }

        #endregion Hiding close window

        static void Main(string[] args)
        {
            Console.Title = "Simple Email Sender";

            _log = LogManager.GetLoggerFor("MAIN");

            // EventStoreSettings
            var eventStoreIp = ConfigurationManager.AppSettings["EventStoreIp"];
            var eventStoreTcpPort = int.Parse(ConfigurationManager.AppSettings["EventStoreTcpPort"]);
            var eventStoreUser = ConfigurationManager.AppSettings["EventStoreUser"];
            var eventStorePass = ConfigurationManager.AppSettings["EventStorePass"];

            // Conectando el EventStore
            var eventStoreSettings = ConnectionSettings
                                        .Create()
                                        .KeepReconnecting()
                                        .SetDefaultUserCredentials(new UserCredentials(eventStoreUser, eventStorePass))
                                        .Build();

            var ip = new IPEndPoint(IPAddress.Parse(eventStoreIp), eventStoreTcpPort);
            _connection = EventStoreConnection.Create(eventStoreSettings, ip);
            _connection.Closed += (s, e) =>
            {
                var ex = new InvalidOperationException($"The connection was {_connection.ConnectionName} closed. Reason: {e.Reason}");
                _log.Error(ex, "The connection was closed");
                throw ex;
            };
            _connection.Disconnected += (s, e) => _log.Log($"The connection {_connection.ConnectionName} was disconnected. Reconnecting....");
            _connection.Reconnecting += (s, e) => _log.Log($"The connection {_connection.ConnectionName} is now reconnecting");
            _connection.ErrorOccurred += (s, e) =>
            {
                _log.Error(e.Exception, "An error ocurred in connection " + _connection.ConnectionName);
                throw e.Exception;
            };
            _connection.Connected += (s, e) => _log.Log($"The connection {_connection.ConnectionName} is now connected");
            _connection.ConnectAsync();

            var serializer = new JsonSerializer();

            // Main sender settings
            var snapshotInterval = int.Parse(ConfigurationManager.AppSettings["snapshotInterval"]);
            var enablePersistentSnapshotting = snapshotInterval > 0;
            var host = ConfigurationManager.AppSettings["host"];
            var port = int.Parse(ConfigurationManager.AppSettings["port"]);


            var repo = new EventSourcedRepository(() => _connection, serializer, "MailQueueRepo", enablePersistentSnapshotting, snapshotInterval);
            var emailSender = new EmailSender(host, port);
            using (var job = new MailSendingJob(_connection, repo, serializer, emailSender, EventStoreQueueConstants.QueueStreamName, EventStoreQueueConstants.OutboxStreamName))
            {
                job.Start();

                string exitCode;
                do
                {
                    Console.WriteLine("Type [exit] to shut down...");
                    exitCode = Console.ReadLine();
                } while (exitCode.ToUpper().Trim() != "EXIT");
            }
        }
    }
}
