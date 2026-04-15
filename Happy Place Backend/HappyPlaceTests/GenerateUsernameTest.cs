using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GenerateUsernameTest
{
    // Tests - Username Length

    [Fact]
    public void LongNameProducesUsernameUnder20Characters()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine Youssef Najjarine Youssef Najjarine", dbContext);

        Assert.True(username.Length <= 20);
    }

    [Fact]
    public void ShortNameProducesUsernameWithAtLeastFiveCharacters()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Y", dbContext);

        Assert.True(username.Length >= 5);
    }

    // Tests - Username Format

    [Fact]
    public void UsernameIsAllLowercase()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.Equal(username, username.ToLower());
    }

    [Fact]
    public void UsernameContainsNoSpaces()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.DoesNotContain(" ", username);
    }

    [Fact]
    public void UsernameEndsWithNumber()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.True(char.IsDigit(username[username.Length - 1]));
    }

    // Tests - Username Uniqueness

    [Fact]
    public void TwoGeneratedUsernamesForSameNameAreDifferent()
    {
        using var dbContext = HappyPlaceDbContext.Create();

        string firstUsername = UserAccountRegistrar.GenerateUsername("user1@gmail.com", "John Smith", dbContext);
        dbContext.PendingUserAccounts.Add(new PendingUserAccount
        {
            EmailAddress = "user1@gmail.com",
            DisplayName = "John Smith",
            Username = firstUsername,
            HashedPassword = "hashed",
            VerificationCode = "123456"
        });
        dbContext.SaveChanges();
        string secondUsername = UserAccountRegistrar.GenerateUsername("user2@gmail.com", "John Smith", dbContext);

        Assert.NotEqual(firstUsername, secondUsername);
    }
}