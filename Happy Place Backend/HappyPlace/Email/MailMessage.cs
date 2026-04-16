namespace HappyWorld.HappyPlace.Email {
    public interface MailMessage {
        String Subject { get; set; }

        void AddFromAddress(string emailAddress);
        void AddToAddress(string emailAddress);
        void SetHtmlBody(string text);
    }
}
