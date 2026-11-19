namespace Obscura.Api.Security;

// Argon2id cost parameters - stored inside every PHC string, so they can be raised later
// and old hashes still verify with the parameters they were created with
public sealed record Argon2Parameters(int MemoryKiB, int Iterations, int Parallelism)
{
    // OWASP minimum for Argon2id: 19 MiB memory, 2 passes, 1 lane
    public static readonly Argon2Parameters Default = new(19_456, 2, 1);
}