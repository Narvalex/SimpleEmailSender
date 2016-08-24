using System.Collections.Generic;
using System.Net.Mail;

namespace SimpleEmailSender
{
    public class EmailFactory : INeedRecipient, IReadyToBuild
    {
        private Contact from;
        private List<Contact> to = new List<Contact>();
        private List<Contact> carbonCopies = new List<Contact>();
        private List<Contact> blindCarbonCoppies = new List<Contact>();
        private List<Contact> replyToList = new List<Contact>();
        private string subject = "";
        private string body = "";
        private bool isBodyHtml = true;
        private MailPriority priority = MailPriority.Normal;

        private EmailFactory() { }

        public static INeedRecipient NewFrom(string name, string address)
        {
            var factory = new EmailFactory();
            factory.from = new Contact(name, address);
            return factory;
        }

        public IReadyToBuild To(string name, string address)
        {
            this.to.Add(new Contact(name, address));
            return this;
        }

        public IReadyToBuild Subject(string subject)
        {
            this.subject = subject;
            return this;
        }

        public IReadyToBuild Body(string body)
        {
            this.body = body;
            return this;
        }

        public Envelope Build()
        {
            return new Envelope(
                this.from,
                this.to.ToArray(),
                this.carbonCopies.ToArray(),
                this.blindCarbonCoppies.ToArray(),
                this.replyToList.ToArray(),
                this.subject,
                this.body,
                this.isBodyHtml,
                this.priority);
        }
    }

    public interface INeedRecipient
    {
        IReadyToBuild To(string name, string address);
    }

    public interface IReadyToBuild
    {
        IReadyToBuild Subject(string subject);
        IReadyToBuild Body(string body);
        Envelope Build();
    }
}
