namespace SimpleEmailSender
{
    public interface IEmailSender
    {
        void Send(Envelope mail);
    }
}
