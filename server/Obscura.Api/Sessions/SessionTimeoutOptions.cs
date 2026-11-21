namespace Obscura.Api.Sessions;

// Session timeouts, bound from the "Sessions" section of appsettings.json
// Same values in every environment:
// idle = 15 min
// absolute = 8 h
public sealed class SessionTimeoutOptions
{
    public const string SectionName = "Sessions";

    // Ends a session after this long without a request
    public TimeSpan IdleTimeout { get; set; }

    // Ends a session after this long after a login, however active it is
    public TimeSpan AbsoluteTimeout { get; set; }
}