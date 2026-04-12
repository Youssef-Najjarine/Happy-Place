using DotNetEnv;
using MailKit.Net.Smtp;
using MailKit.Security;
namespace HappyWorld.HappyPlace.Email
{
    internal class MailKitEmailSender : EmailSender
    {
        static MailKitEmailSender()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
                directory = directory.Parent;
            if (directory != null) Env.Load(Path.Combine(directory.FullName, ".env"));
        }

        public override MailMessage NewMailMessage()
        {
            return new MailKitMailMessage();
        }

        public override void Send(MailMessage message)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            using var client = new SmtpClient();
            client.ConnectAsync(host, port, SecureSocketOptions.StartTls).GetAwaiter().GetResult();
            client.AuthenticateAsync(username, password).GetAwaiter().GetResult();
            client.SendAsync(((MailKitMailMessage)message).GetActualMailMessage()).GetAwaiter().GetResult();
            client.DisconnectAsync(true).GetAwaiter().GetResult();
        }
    }
}