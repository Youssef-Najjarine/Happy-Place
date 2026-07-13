namespace HappyWorld.HappyPlace;

public record FriendListPageResult(string Status, int TotalCount, List<FriendListEntry> Friends, string NextCursor) {
    // Methods

    public static FriendListPageResult Ok(int totalCount, List<FriendListEntry> friends, string nextCursor) {
        return new FriendListPageResult("ok", totalCount, friends, nextCursor);
    }

    public static FriendListPageResult NotFound() {
        return new FriendListPageResult("notFound", 0, null, null);
    }
}
