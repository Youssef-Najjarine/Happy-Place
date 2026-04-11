using HappyWorld.HappyPlace.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace
{
    internal class InMemoryMailMessage : MailMessage
    {
        public IList<String> FromAddresses { get; } = [];
        public IList<String> ToAddresses { get; } = [];

        public Boolean IsBodyHtml { get; private set; }
        public String BodyText { get; private set; }
        public String Subject { get; set; }

        public void AddFromAddress(string emailAddress)
        {
            this.FromAddresses.Add(emailAddress);
        }

        public void AddToAddress(string emailAddress)
        {
            this.ToAddresses.Add(emailAddress);
        }

        public void SetHtmlBody(string text)
        {
            this.IsBodyHtml = true;
            this.BodyText = text;
        }
    }
}
