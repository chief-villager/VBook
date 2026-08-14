namespace Bookkeeping.Application.Identity;

// The pair returned on sign-in and refresh. The access token authorises API calls
// until AccessTokenExpiresAt; the refresh token is exchanged (and rotated) for a new
// pair after that, until RefreshTokenExpiresAt or the session is logged out.
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
