namespace HappyWorld.HappyPlace;

public class TestingMockProvidersContainer : IDisposable
{
    // Fields
    private InMemoryEmailSenderProvider _emailProvider;
    private InMemorySmsSenderProvider _smsProvider;
    private bool _isDisposed;
    private WebClient _webClient;

    // Constructors
    public TestingMockProvidersContainer()
    {
        this._webClient = new WebClient();
        this._emailProvider = new InMemoryEmailSenderProvider();
        this._smsProvider = new InMemorySmsSenderProvider();
    }
    ~TestingMockProvidersContainer()
    {
        Dispose(disposing: false);
    }

    // Properties
    public InMemoryEmailSenderProvider EmailProvider => this._emailProvider;
    public InMemorySmsSenderProvider SmsProvider => this._smsProvider;
    public WebClient WebClient => this._webClient;

    // Methods
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing) { }
            this.DisposeProviders();
            _isDisposed = true;
        }
    }

    private void DisposeProviders()
    {
        this._emailProvider?.Dispose();
        this._emailProvider = null;
        this._smsProvider?.Dispose();
        this._smsProvider = null;
        this._webClient?.Dispose();
        this._webClient = null;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}