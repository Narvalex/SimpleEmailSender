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

            var mainLog = LogManager.GetLoggerFor("MAIN");

            // EventStoreSettings
            var eventStoreIp = ConfigurationManager.AppSettings["EventStoreIp"];
            var eventStoreTcpPort = int.Parse(ConfigurationManager.AppSettings["EventStoreTcpPort"]);
            var eventStoreUser = ConfigurationManager.AppSettings["EventStoreUser"];
            var eventStorePass = ConfigurationManager.AppSettings["EventStorePass"];

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
            connection.Closed += (s, e) => mainLog.Error($"The {connectionSource} ES connection was closed");
            connection.Connected += (s, e) => mainLog.Log($"The connection with {connectionSource} ES was establised");
            connection.Disconnected += (s, e) => mainLog.Log($"The connection with {connectionSource} ES was lost");
            connection.Reconnecting += (s, e) => mainLog.Log($"Reconnecting with {connectionSource} ES");
            connection.ConnectAsync().Wait();

            var serializer = new JsonSerializer();

            var repo = new EventSourcedRepository(connection, serializer, enablePersistentSnapshotting: true, snapshotInterval: 2);
            var emailSender = new EmailSender("mail.fecoprod.com.py", 25);
            using (var job = new MailSendingJob(connection, repo, serializer, emailSender, EventStoreQueueConstants.QueueStreamName, EventStoreQueueConstants.OutboxStreamName))
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
