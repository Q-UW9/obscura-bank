using Obscura.Api.Security;

namespace Obscura.Api.Tests.Security;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("fifteen chars!!", true)]             // 15 ASCII code points: the minimum
    [InlineData("fourteen char!", false)]             // 14: one too short
    [InlineData("\U0001F512fourteen chars", true)]    // emoji + 14 = 15 code points (Length is 16)
    [InlineData("\U0001F512thirteen char", false)]    // emoji + 13 = 14 code points, although Length is 15
    public void HasValidLength_CountsCodePoints(string password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.HasValidLength(password));
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void HasValidLength_EnforcesMaximum(int length, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.HasValidLength(new string('a', length)));
    }

    // 15 x ("e" + combining accents) = 30 code points as typed, but 15 after NKFC
    [Fact]
    public void HasValidLength_CountsAfterNormalization()
    {
        var decomposed = string.Concat(Enumerable.Repeat("e\u0301", 15));

        Assert.True(PasswordPolicy.HasValidLength(decomposed));
    }

    [Fact]
    public void HasValidLength_HugeInput_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.HasValidLength(new string('a', 1_000_000)));
    }
}