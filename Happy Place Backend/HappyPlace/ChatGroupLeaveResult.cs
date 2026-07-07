namespace HappyWorld.HappyPlace;

public record ChatGroupLeaveResult(string Status) {
    // Methods

    public static ChatGroupLeaveResult Left() {
        return new ChatGroupLeaveResult("left");
    }

    public static ChatGroupLeaveResult Transferred() {
        return new ChatGroupLeaveResult("transferred");
    }

    public static ChatGroupLeaveResult Deleted() {
        return new ChatGroupLeaveResult("deleted");
    }

    public static ChatGroupLeaveResult MadePublic() {
        return new ChatGroupLeaveResult("madePublic");
    }

    public static ChatGroupLeaveResult LastOwner() {
        return new ChatGroupLeaveResult("lastOwner");
    }

    public static ChatGroupLeaveResult NotMember() {
        return new ChatGroupLeaveResult("notMember");
    }
}
