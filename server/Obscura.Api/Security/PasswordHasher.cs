using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Obscura.Api.Security;

// Hashes passwords with Argon2id and stores the result as a PHC string:
// $argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>
public sealed class PasswordHasher
{
    private const int SaltSize = 16; // bytes: a fresh random salt for every hash
    private const int HashSize = 32; // bytes of Argon2id output (256 bits)

    private readonly Argon2Parameters _current;

    public PasswordHasher(Argon2Parameters current)
    {
        _current = current;

        // Hash of a random password nobody knows, with the current parameters
        // Login verifies unknown emails against it, so both cases cost one Argon2id run
        DummyHash = Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public string DummyHash { get; }

    // Returns a PHC string for a new password (registration, password change, rehash)
    public string Hash(string password)
    {
        // An empty password is never stored - registration validation rejects it before this point
        // Discovered via PasswordHasherTests.cs v1
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Compute(password, salt, _current);
        return Encode(_current, salt, hash);
    }

    // Check a password against a stored PHC string, using the salt and parameters stored in it
    public bool Verify(string password, string phc)
    {
        var stored = Parse(phc);

        // Argon2 cannot hash empty input and an empty password can never be the right one
        // Discovered via PasswordHasherTests.cs v1
        if (password.Length == 0)
            return false;

        var actual = Compute(password, stored.Salt, stored.Parameters);
        return CryptographicOperations.FixedTimeEquals(actual, stored.Hash);
    }

    // True when the stored hash was made with other parameters than the current ones
    // Only call after a successful Verify - that is the only moment the plaintext is there to refresh
    public bool NeedsRehash(string phc) => Parse(phc).Parameters != _current;

    // Runs Argon2id - CPU bound work, so synchronous on purpose
    private static byte[] Compute(string password, byte[] salt, Argon2Parameters p)
    {
        var normalized = password.Normalize(NormalizationForm.FormKC);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(normalized))
        {
            Salt = salt,
            MemorySize = p.MemoryKiB,
            Iterations = p.Iterations,
            DegreeOfParallelism = p.Parallelism,
        };
        return argon2.GetBytes(HashSize);
    }

    private static string Encode(Argon2Parameters p, byte[] salt, byte[] hash) =>
        $"$argon2id$v=19$m={p.MemoryKiB},t={p.Iterations},p={p.Parallelism}" +
        $"${ToBase64(salt)}${ToBase64(hash)}";

    // PHC strings use standard Base64 without "=" padding
    private static string ToBase64(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=');

    // The pieces of a parsed PHC string
    private sealed record StoredHash(Argon2Parameters Parameters, byte[] Salt, byte[] Hash);

    // Parses "$argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>". Stored hashes come from our own
    // Hash(), so a malformed one means corrupted data: fail loudly instead of returning false
    private static StoredHash Parse(string phc)
    {
        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[0] != "" || parts[1] != "argon2id" || parts[2] != "v=19")
            throw new FormatException("Not an Argon2id v19 PHC string.");

        var costs = parts[3].Split(',');
        if (costs.Length != 3)
            throw new FormatException("Expected m, t and p parameters.");

        var parameters = new Argon2Parameters(
            MemoryKiB:   ParseCost(costs[0], "m"),
            Iterations:  ParseCost(costs[1], "t"),
            Parallelism: ParseCost(costs[2], "p"));
        
        var salt = FromBase64(parts[4]);
        var hash = FromBase64(parts[5]);
        if (hash.Length != HashSize)
            throw new FormatException("Unexpected hash length.");

        return new StoredHash(parameters, salt, hash);
    }

    // Parses one "name=value" pair such as "m=19456": digits only, positive
    private static int ParseCost(string pair, string name)
    {
        var prefix = name + "=";
        if (!pair.StartsWith(prefix, StringComparison.Ordinal)
            || !int.TryParse(pair[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value <= 0)
            throw new FormatException($"Invalid cost parameter '{name}'.");
        return value;
    }

    // re-adds the '=' padding that PHC strings leave out, then decodes
    private static byte[] FromBase64(string s) =>
        Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
}