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

    // Methods
    public static void SetConnectionString(string connectionString) => _connectionStringOverride = connectionString;

    public static void ResetConnectionString() => _connectionStringOverride = null;

    public static HappyPlaceDbContext Create() {
        string connectionString = _connectionStringOverride ?? "Server=.;Database=HappyPlace;Trusted_Connection=True;MultipleActiveResultSets=true;trustservercertificate=yes";
        var optionsBuilder = new DbContextOptionsBuilder<HappyPlaceDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new HappyPlaceDbContext(optionsBuilder.Options);
    }
}
