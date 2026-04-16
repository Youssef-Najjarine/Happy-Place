using MimeKit;

namespace HappyWorld.HappyPlace.Email {
    internal class MailKitMailMessage : MailMessage {
        private readonly MimeMessage _mimeMessage = new();
        public string Subject { get => this._mimeMessage.Subject; set => this._mimeMessage.Subject = value; }

        public void AddFromAddress(string emailAddress) {
            this._mimeMessage.From.Add(MailboxAddress.Parse(emailAddress));
        }

        public void AddToAddress(string emailAddress) {
            this._mimeMessage.To.Add(MailboxAddress.Parse(emailAddress));
        }

        public void SetHtmlBody(string text) {
            this._mimeMessage.Body = new TextPart("html") {
                Text = text
            };
        }

        internal MimeMessage GetActualMailMessage() {
            return this._mimeMessage;
        }
    }
}
