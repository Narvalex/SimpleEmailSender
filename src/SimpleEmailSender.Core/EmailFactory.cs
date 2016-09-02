using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleEmailSender
{
    public class EmailFactory : INeedRecipient, IReadyForBuild, IHideObjectMembers
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

        private EmailFactory()
        {
            var config = new TemplateServiceConfiguration();
            config.DisableTempFileLocking = true;
            var service = RazorEngineService.Create(config);
            Engine.Razor = service;
        }

        public static INeedRecipient NewFrom(string name, string address)
        {
            var factory = new EmailFactory();
            factory.from = new Contact(name, address);
            return factory;
        }

        public IReadyForBuild To(string name, string address)
        {
            this.to.Add(new Contact(name, address));
            return this;
        }

        public IReadyForBuild Subject(string subject)
        {
            this.subject = subject;
            return this;
        }

        public IReadyForBuild Body(string body)
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

        public IReadyForBuild BodyUsingTemplate<T>(string template, T model)
        {
            var defaultModelType = typeof(T);
            Type resolvedModelType = IsAnonymousType(defaultModelType) ? null : defaultModelType;
            this.body = Engine.Razor.RunCompile(template, model.GetHashCode().ToString(), resolvedModelType, model);
            return this;
        }

        public IReadyForBuild BodyUsingTemplateFromFile<T>(string fileName, T model)
        {
            var path = GetFullFilePath(fileName);
            string template;
            using (var reader = new StreamReader(path))
            {
                template = reader.ReadToEnd();
            }

            return this.BodyUsingTemplate<T>(template, model);
        }

        private static bool IsAnonymousType(Type type)
        {
            // Credits: http://stackoverflow.com/questions/2483023/how-to-test-if-a-type-is-anonymous
            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        private static string GetFullFilePath(string fileName)
        {
            if (fileName.StartsWith("~"))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.GetFullPath(baseDir + fileName.Replace("~", string.Empty));
            }

            return Path.GetFullPath(fileName);
        }

        public IReadyForBuild To(params Contact[] contacts)
        {
            return this.To(contacts.AsEnumerable());
        }

        public IReadyForBuild To(IEnumerable<Contact> contacts)
        {
            EnsureContactListIsNotEmpty(contacts);

            this.to.AddRange(contacts);
            return this;
        }

        private static void EnsureContactListIsNotEmpty(IEnumerable<Contact> contacts)
        {
            if (contacts.Count() == 0)
                throw new ArgumentException("You must provide at least one contact");
        }

        public IReadyForBuild CC(string name, string address)
        {
            return this.CC(new Contact(name, address));
        }

        public IReadyForBuild CC(params Contact[] contacts)
        {
            return this.CC(contacts.AsEnumerable());
        }

        public IReadyForBuild CC(IEnumerable<Contact> contacts)
        {
            this.carbonCopies.AddRange(contacts);
            return this;
        }

        public IReadyForBuild BCC(string name, string address)
        {
            return this.BCC(new Contact(name, address));
        }

        public IReadyForBuild BCC(params Contact[] contacts)
        {
            return this.BCC(contacts.AsEnumerable());
        }

        public IReadyForBuild BCC(IEnumerable<Contact> contacts)
        {
            this.blindCarbonCoppies.AddRange(contacts);
            return this;
        }

        public IReadyForBuild ReplyTo(string name, string address)
        {
            return this.ReplyTo(new Contact(name, address));
        }

        public IReadyForBuild ReplyTo(params Contact[] contacts)
        {
            return this.ReplyTo(contacts.AsEnumerable());
        }

        public IReadyForBuild ReplyTo(IEnumerable<Contact> contacts)
        {
            this.replyToList.AddRange(contacts);
            return this;
        }

        public IReadyForBuild AsHtml()
        {
            this.isBodyHtml = true;
            return this;
        }

        public IReadyForBuild AsText()
        {
            this.isBodyHtml = false;
            return this;
        }

        public IReadyForBuild Priority(MailPriority priority)
        {
            this.priority = priority;
            return this;
        }
    }

    public interface INeedRecipient
    {
        IReadyForBuild To(string name, string address);
        IReadyForBuild To(params Contact[] contacts);
        IReadyForBuild To(IEnumerable<Contact> contacts);
    }

    public interface IReadyForBuild
    {
        IReadyForBuild CC(string name, string address);
        IReadyForBuild CC(params Contact[] contacts);
        IReadyForBuild CC(IEnumerable<Contact> contacts);
        IReadyForBuild BCC(string name, string address);
        IReadyForBuild BCC(params Contact[] contacts);
        IReadyForBuild BCC(IEnumerable<Contact> contacts);
        IReadyForBuild ReplyTo(string name, string address);
        IReadyForBuild ReplyTo(params Contact[] contacts);
        IReadyForBuild ReplyTo(IEnumerable<Contact> contacts);
        IReadyForBuild Subject(string subject);
        IReadyForBuild Body(string body);
        IReadyForBuild BodyUsingTemplate<T>(string template, T model);
        IReadyForBuild BodyUsingTemplateFromFile<T>(string fileName, T model);
        IReadyForBuild AsHtml();
        IReadyForBuild AsText();
        IReadyForBuild Priority(MailPriority priority);
        Envelope Build();
    }
}
