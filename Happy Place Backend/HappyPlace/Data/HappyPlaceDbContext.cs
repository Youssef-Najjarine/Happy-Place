using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace.Data;

public class HappyPlaceDbContext : DbContext {
    // Fields
    private static string _connectionStringOverride;

    // Constructors
    private HappyPlaceDbContext(DbContextOptions<HappyPlaceDbContext> options) : base(options) {
    }

    // Properties
    public DbSet<PendingUserAccount> PendingUserAccounts { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
    public DbSet<UserProfilePhoto> UserProfilePhotos { get; set; }
    public DbSet<PendingPhoneChange> PendingPhoneChanges { get; set; }
    public DbSet<PendingEmailChange> PendingEmailChanges { get; set; }
    public DbSet<ContactChangeAudit> ContactChangeAudits { get; set; }
    public DbSet<ChatGroup> ChatGroups { get; set; }
    public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatMessageReaction> ChatMessageReactions { get; set; }
    public DbSet<ChatMessageReport> ChatMessageReports { get; set; }
    public DbSet<ChatMediaAsset> ChatMediaAssets { get; set; }
    public DbSet<HelpAvailability> HelpAvailabilities { get; set; }
    public DbSet<HelpOffer> HelpOffers { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<NotificationChannel> NotificationChannels { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<UserBlock> UserBlocks { get; set; }
    public DbSet<FriendRequestAudit> FriendRequestAudits { get; set; }

    // Methods
    public static void SetConnectionString(string connectionString) => _connectionStringOverride = connectionString;

    public static void ResetConnectionString() => _connectionStringOverride = null;

    public static HappyPlaceDbContext Create() {
        string connectionString = _connectionStringOverride ?? "Server=.;Database=HappyPlace;Trusted_Connection=True;MultipleActiveResultSets=true;trustservercertificate=yes;ConnectRetryCount=3;ConnectRetryInterval=5";
        var optionsBuilder = new DbContextOptionsBuilder<HappyPlaceDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new HappyPlaceDbContext(optionsBuilder.Options);
    }
}
