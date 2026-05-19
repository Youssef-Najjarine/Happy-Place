using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class AvatarColorTest {
    // Tests - Determinism

    [Fact]
    public void SameUserIdAlwaysReturnsSameColor() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        Guid userId = Guid.NewGuid();
        string firstResult = UserAccountRegistrar.GetAvatarColor(userId);
        string secondResult = UserAccountRegistrar.GetAvatarColor(userId);

        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void DifferentUserIdsCanProduceDifferentColors() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        var colors = Enumerable.Range(0, 50)
            .Select(_ => UserAccountRegistrar.GetAvatarColor(Guid.NewGuid()))
            .Distinct()
            .ToList();

        Assert.True(colors.Count > 1);
    }

    // Tests - Format

    [Fact]
    public void ColorIsValidHexFormat() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        string color = UserAccountRegistrar.GetAvatarColor(Guid.NewGuid());

        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", color);
    }

    [Fact]
    public void ColorIsFromPredefinedPalette() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        string color = UserAccountRegistrar.GetAvatarColor(Guid.NewGuid());

        Assert.Contains(color, UserAccountRegistrar.AvatarColorPalette);
    }

    // Tests - Distribution

    [Fact]
    public void AllPaletteColorsAreReachable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        var colorsProduced = Enumerable.Range(0, 1000)
            .Select(_ => UserAccountRegistrar.GetAvatarColor(Guid.NewGuid()))
            .Distinct()
            .ToHashSet();

        foreach (string paletteColor in UserAccountRegistrar.AvatarColorPalette) {
            Assert.Contains(paletteColor, colorsProduced);
        }
    }

    // Tests - Edge Cases

    [Fact]
    public void EmptyGuidProducesValidColor() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        string color = UserAccountRegistrar.GetAvatarColor(Guid.Empty);

        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", color);
        Assert.Contains(color, UserAccountRegistrar.AvatarColorPalette);
    }
}
