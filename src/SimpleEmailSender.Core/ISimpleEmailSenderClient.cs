namespace SimpleEmailSender
{
    public interface ISimpleEmailSenderClient
    {
        void Send(Envelope email);
    }
}
