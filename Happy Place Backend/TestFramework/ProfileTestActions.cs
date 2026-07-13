namespace HappyWorld.HappyPlace;

public static class ProfileTestActions {
    // Methods

    public static HttpResponseMessage GetPublicUserProfile(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage GetMyProfile(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
    }
}
