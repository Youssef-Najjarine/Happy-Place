namespace HappyWorld.HappyPlace;

public record ChatGroupLeaveResult(string Status) {
    // Methods

    public static ChatGroupLeaveResult Left() {
        return new ChatGroupLeaveResult("left");
    }

    public static ChatGroupLeaveResult NotMember() {
        return new ChatGroupLeaveResult("notMember");
    }

    public static ChatGroupLeaveResult OwnerCannotLeave() {
        return new ChatGroupLeaveResult("ownerCannotLeave");
    }
}
