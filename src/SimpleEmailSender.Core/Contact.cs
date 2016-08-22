namespace SimpleEmailSender
{
    public class Contact
    {
        public Contact(string name, string address)
        {
            this.Name = name;
            this.Address = address;
        }

        public string Name { get; }
        public string Address { get; }
    }
}
