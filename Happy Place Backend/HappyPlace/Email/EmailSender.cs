namespace HappyWorld.HappyPlace.Email;

public abstract class EmailSender
{
    private static Func<EmailSender> _initializer;

    public static void ResetInitializer() => SetInitializer(null);

    public static void SetInitializer(Func<EmailSender> initializer)
    {
        _initializer = initializer;
    }

    internal static EmailSender Create()
    {
        if (_initializer != null) return _initializer();
        return new MailKitEmailSender();
    }

    public abstract MailMessage NewMailMessage();

    public abstract void Send(MailMessage message);
}