using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Mail;

namespace SimpleEmailSender.Core.Tests
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void Can_send_mail()
        {
            var smtpClient = new SmtpClient("mail.fecoprod.comERCIAL.PARAG", 25);
            smtpClient.UseDefaultCredentials = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = false;

            var mail = new MailMessage();
            mail.From = new MailAddress("chuchonavarro@fecoprod.com.py", "Test");
            mail.To.Add(new MailAddress("chayala@fecoprod.com.py"));
            mail.Subject = "hola";
            mail.Body = "hola este es sin credenciales";

            smtpClient.Send(mail);
        }
    }
}
