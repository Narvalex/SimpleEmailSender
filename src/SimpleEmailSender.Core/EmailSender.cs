using Eventing.Utils;
using System;
using System.Linq;
using System.Net.Mail;

namespace SimpleEmailSender
{
    public class EmailSender : IEmailSender
    {
        private readonly Func<SmtpClient> smtpClientFactory;

        public EmailSender(string host, int port, bool enableSsl = false)
            : this(() => new SmtpClient
            {
                Host = host,
                Port = port,
                UseDefaultCredentials = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = enableSsl
            })
        { }

        public EmailSender(Func<SmtpClient> smtpClientFactory)
        {
            Ensure.NotNull(smtpClientFactory, nameof(smtpClientFactory));

            this.smtpClientFactory = smtpClientFactory;
        }

        public void Send(Envelope mail)
        {
            using (var smpt = this.smtpClientFactory.Invoke())
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(mail.From.Address, mail.From.Name),
                    Subject = mail.Subject,
                    Body = mail.Body,
                    IsBodyHtml = mail.IsBodyHtml,
                    Priority = mail.Priority
                };

                mailMessage.To.AddRange(mail.To.Select(c => new MailAddress(c.Address, c.Name)));
                mailMessage.CC.AddRange(mail.CarbonCopies.Select(c => new MailAddress(c.Address, c.Name)));
                mailMessage.Bcc.AddRange(mail.BlindCarbonCopies.Select(c => new MailAddress(c.Address, c.Name)));
                mailMessage.ReplyToList.AddRange(mail.ReplyToList.Select(c => new MailAddress(c.Address, c.Name)));

                smpt.Send(mailMessage);
            }
        }
    }
}
