using MailKit.Net.Smtp;
using MailKit.Security;

namespace HappyWorld.HappyPlace.Email
{
    internal class MailKitEmailSender : EmailSender
    {
        public override MailMessage NewMailMessage()
        {
            return new MailKitMailMessage();
        }

        public override void Send(MailMessage message)
        {
            using var client = new SmtpClient();
            client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls).GetAwaiter().GetResult();
            client.AuthenticateAsync("youssef@happy.place", "shaerbgqufbujzor").GetAwaiter().GetResult();
            client.SendAsync(((MailKitMailMessage)message).GetActualMailMessage()).GetAwaiter().GetResult();
            client.DisconnectAsync(true).GetAwaiter().GetResult();
        }
    }
}
