using System.Net.Mail;

namespace SimpleEmailSender
{
    public class Envelope
    {
        public Envelope(
            Contact from,
            Contact[] to,
            Contact[] carbonCopies,
            Contact[] blindCarbonCopies,
            Contact[] replyToList,
            string subject,
            string body,
            bool isBodyHtml,
            MailPriority priority)
        {
            this.From = from;
            this.To = to;
            this.CarbonCopies = carbonCopies;
            this.BlindCarbonCopies = blindCarbonCopies;
            this.ReplyToList = replyToList;
            this.Subject = subject;
            this.Body = body;
            this.IsBodyHtml = isBodyHtml;
        }
        public Contact From { get; }
        public Contact[] To { get; }
        public Contact[] CarbonCopies { get; }
        public Contact[] BlindCarbonCopies { get; }
        public Contact[] ReplyToList { get; }
        public string Subject { get; }
        public string Body { get; }
        public bool IsBodyHtml { get; }
        public MailPriority Priority { get; }
    }
}
