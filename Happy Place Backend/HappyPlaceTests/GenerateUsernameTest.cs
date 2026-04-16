using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GenerateUsernameTest {
    // Tests - Username Length

    [Fact]
    public void LongNameProducesUsernameUnder20Characters() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine Youssef Najjarine Youssef Najjarine", dbContext);

        Assert.True(username.Length <= 20);
    }

    [Fact]
    public void ShortNameProducesUsernameWithAtLeastFiveCharacters() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Y", dbContext);

        Assert.True(username.Length >= 5);
    }

    // Tests - Username Format

    [Fact]
    public void UsernameIsAllLowercase() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.Equal(username, username.ToLower());
    }

    [Fact]
    public void UsernameContainsNoSpaces() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.DoesNotContain(" ", username);
    }

    [Fact]
    public void UsernameEndsWithNumber() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Youssef Najjarine", dbContext);

        Assert.True(char.IsDigit(username[username.Length - 1]));
    }

    // Tests - Username Uniqueness

    [Fact]
    public void TwoGeneratedUsernamesForSameNameAreDifferent() {
        using var dbContext = HappyPlaceDbContext.Create();

        string firstUsername = UserAccountRegistrar.GenerateUsername("user1@gmail.com", "John Smith", dbContext);
        dbContext.PendingUserAccounts.Add(new PendingUserAccount {
            EmailAddress = "user1@gmail.com",
            DisplayName = "John Smith",
            Username = firstUsername,
            HashedPassword = "hashed",
            VerificationCode = "123456",
            CreatedAtUtc = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        string secondUsername = UserAccountRegistrar.GenerateUsername("user2@gmail.com", "John Smith", dbContext);

        Assert.NotEqual(firstUsername, secondUsername);
    }

    // Tests - Name Character Handling

    [Fact]
    public void NameWithSpecialCharactersKeepsThemInUsername() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("obrien@gmail.com", "O'Brien", dbContext);

        Assert.Contains("'", username);
    }

    [Fact]
    public void NameWithNumbersKeepsThemInUsername() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("john3@gmail.com", "John3", dbContext);

        Assert.StartsWith("john3", username);
    }

    // Tests - Name Length Boundaries

    [Fact]
    public void NameAtExactly3CharsGetsPaddedToAtLeast4() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("joe@gmail.com", "Joe", dbContext);
        string baseWithoutNumber = username.TrimEnd("0123456789".ToCharArray());

        Assert.True(baseWithoutNumber.Length >= 4);
    }

    [Fact]
    public void NameAtExactly4CharsDoesNotGetPadded() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("john@gmail.com", "John", dbContext);

        Assert.StartsWith("john", username);
    }

    [Fact]
    public void NameAtExactly18CharsGetsTruncated() {
        using var dbContext = HappyPlaceDbContext.Create();
        string eighteenCharName = new string('a', 18);

        string username = UserAccountRegistrar.GenerateUsername("test@gmail.com", eighteenCharName, dbContext);
        string baseWithoutNumber = username.TrimEnd("0123456789".ToCharArray());

        Assert.Equal(18, baseWithoutNumber.Length);
    }

    [Fact]
    public void NameThatIsAllSpacesProducesValidUsername() {
        using var dbContext = HappyPlaceDbContext.Create();

        string username = UserAccountRegistrar.GenerateUsername("spaces@gmail.com", "     ", dbContext);

        Assert.True(username.Length >= 5);
        Assert.True(char.IsDigit(username[username.Length - 1]));
    }
}