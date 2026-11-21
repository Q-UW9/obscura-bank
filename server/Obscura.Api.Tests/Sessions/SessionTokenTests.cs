using Obscura.Api.Sessions;

namespace Obscura.Api.Tests.Sessions;

public class SessionTokenTests
{
    [Fact]
    public void Create_Returns43CharacterBase64UrlToken()
    {
        var token = SessionToken.Create();

        Assert.Equal(43, token.Length);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void Create_TwoTokens_AreDifferent()
    {
        Assert.NotEqual(SessionToken.Create(), SessionToken.Create());
    }

    [Fact]
    public void TryHash_ValidToken_Gives32Bytes_AndTheSameHashEveryTime()
    {
        var token = SessionToken.Create();

        var first = SessionToken.TryHash(token);
        var second = SessionToken.TryHash(token);
       
        Assert.NotNull(first);
        Assert.Equal(32, first.Length);
        Assert.Equal(first, second);
    }

    // Tampered or junk cookies never reach the db
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("browser-check")]                                  // the dummy from the browser test
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]       // 42 chars: one too short
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]     // 44 chars: decodes to 33 bytes
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+A")]      // '+' is not base64url
    public void TryHash_MalformedToken_ReturnsNull(string? token)
    {
        Assert.Null(SessionToken.TryHash(token));
    }
}