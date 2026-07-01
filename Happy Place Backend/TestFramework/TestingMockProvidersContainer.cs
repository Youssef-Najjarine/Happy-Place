using HappyWorld.HappyPlace.Data;
using Microsoft.Data.SqlClient;

namespace HappyWorld.HappyPlace;

public class TestingMockProvidersContainer : IDisposable {
    // Fields

    private const string TestConnectionString = "Server=.;Database=HappyPlaceTests;Trusted_Connection=True;MultipleActiveResultSets=true;trustservercertificate=yes";
    private InMemoryEmailSenderProvider _emailProvider;
    private InMemorySmsSenderProvider _smsProvider;
    private InMemoryPushSenderProvider _pushProvider;
    private bool _isDisposed;
    private WebClient _webClient;

    // Constructors

    public TestingMockProvidersContainer() {
        HappyPlaceDbContext.SetConnectionString(TestConnectionString);
        NotificationDispatchManager.BackgroundSweepEnabled = false;
        this._webClient = new WebClient();
        this._emailProvider = new InMemoryEmailSenderProvider();
        this._smsProvider = new InMemorySmsSenderProvider();
        this._pushProvider = new InMemoryPushSenderProvider();
        ResetDatabase();
    }

    ~TestingMockProvidersContainer() {
        Dispose(disposing: false);
    }

    // Properties

    public InMemoryEmailSenderProvider EmailProvider => this._emailProvider;
    public InMemorySmsSenderProvider SmsProvider => this._smsProvider;
    public InMemoryPushSenderProvider PushProvider => this._pushProvider;
    public WebClient WebClient => this._webClient;

    // Methods

    protected virtual void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) { }
            this.DisposeProviders();
            _isDisposed = true;
        }
    }

    private void DisposeProviders() {
        this._emailProvider?.Dispose();
        this._emailProvider = null;
        this._smsProvider?.Dispose();
        this._smsProvider = null;
        this._pushProvider?.Dispose();
        this._pushProvider = null;
        this._webClient?.Dispose();
        this._webClient = null;
        HappyPlaceDbContext.ResetConnectionString();
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private static void ResetDatabase() {
        using var connection = new SqlConnection(TestConnectionString);
        using var command = connection.CreateCommand();
        command.CommandText = @"
DELETE FROM [dbo].[NotificationChannel];
DELETE FROM [dbo].[HelpOffer];
DELETE FROM [dbo].[DeviceToken];
DELETE FROM [dbo].[HelpAvailability];
DELETE FROM [dbo].[ChatGroupMember];
DELETE FROM [dbo].[ChatGroup];
DELETE FROM [dbo].[UserProfilePhoto];
DELETE FROM [dbo].[PasswordResetRequest];
DELETE FROM [dbo].[PendingUserAccount];
DELETE FROM [dbo].[PendingPhoneChange];
DELETE FROM [dbo].[PendingEmailChange];
DELETE FROM [dbo].[ContactChangeAudit];
DELETE FROM [dbo].[UserAccount];";
        connection.Open();
        command.ExecuteNonQuery();
    }
}
