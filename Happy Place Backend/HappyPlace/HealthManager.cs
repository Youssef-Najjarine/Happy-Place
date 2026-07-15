using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public static class HealthManager {
    // Methods - Checks

    public static bool IsDatabaseAvailable() {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            return dbContext.Database.CanConnect();
        }
        catch {
            return false;
        }
    }
}
