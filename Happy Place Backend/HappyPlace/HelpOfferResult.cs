namespace HappyWorld.HappyPlace;

public record HelpOfferResult(string Status) {
    // Methods

    public static HelpOfferResult None() {
        return new HelpOfferResult("none");
    }

    public static HelpOfferResult Offered() {
        return new HelpOfferResult("offered");
    }

    public static HelpOfferResult Declined() {
        return new HelpOfferResult("declined");
    }

    public static HelpOfferResult RequestClosed() {
        return new HelpOfferResult("requestClosed");
    }

    public static HelpOfferResult RegistrationRequired() {
        return new HelpOfferResult("registrationRequired");
    }
}
