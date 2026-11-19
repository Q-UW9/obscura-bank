using Obscura.Api.Security;

namespace Obscura.Api.Tests.Security;

public class PasswordHasherTests
{
    // xUnit creates a new instance of this class for every test, so this is fresh each time
    private readonly PasswordHasher _hasher = new(Argon2Parameters.Default);

    [Fact]
    public void Hash_WritesPhcStringWithCurrentParameters()
    {
        var phc = _hasher.Hash("correct horse battery staple");

        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", phc);
    }

    // Attack: identical passwords must not produce identical hashes (salt per hashes solution)
    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentStrings()
    {
        var first  = _hasher.Hash("hunter2");
        var second = _hasher.Hash("hunter2");

        Assert.NotEqual(first, second); 
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var phc = _hasher.Hash("hunter2");

        Assert.True(_hasher.Verify("hunter2", phc));
    }

    // No trimming, no case folding - these are all different passwords
    [Theory]
    [InlineData("Hunter2")]
    [InlineData("hunter2 ")]
    [InlineData("hunter")]
    [InlineData("")]
    public void Verify_DifferentPassword_ReturnsFalse(string attempt)
    {
        var phc = _hasher.Hash("hunter2");

        Assert.False(_hasher.Verify(attempt, phc));
    }

    // NFKC - precomposed "é" and "e" + combining accent are the same password
    [Fact]
    public void Verify_OtherUnicodeFormOfSamePassword_ReturnsTrue()
    {
        var phc = _hasher.Hash("caf\u00E9");

        Assert.True(_hasher.Verify("cafe\u0301", phc));
    }

    // An old hash keeps working, because Verify uses its stored parameters and asks to be upgraded
    [Fact]
    public void OldHash_StillVerifies_AndNeedsRehash()
    {
        var oldHasher = new PasswordHasher(new Argon2Parameters(8_192, 1, 1));
        var phc = oldHasher.Hash("hunter2");

        Assert.True(_hasher.Verify("hunter2", phc));
        Assert.True(_hasher.NeedsRehash(phc));
    }

    [Fact]
    public void CurrentHash_DoesNotNeedRehash()
    {
        var phc = _hasher.Hash("hunter2");

        Assert.False(_hasher.NeedsRehash(phc));
    }

    // Stored hashes are our own data, so anything malformed must fail loudly, never return false!
    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$argon2i$v=19$m=19456,t=2,p=1$AAAA$AAAA")]                      // wrong variant
    [InlineData("$argon2id$v=19$m=-1,t=2,p=1$AAAA$AAAA")]                        // negative cost
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$!!!!$AAAA")]                     // invalid Base64
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$AAAAAAAAAAAAAAAAAAAAAA$AAAA")]   // hash too short
    public void Verify_MalformedStoredHash_Throws(string phc)
    {
        Assert.Throws<FormatException>(() => _hasher.Verify("hunter2", phc));
    }

    // An empty password must never be stored!
    [Fact]
    public void Hash_EmptyPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(""));
    }
}