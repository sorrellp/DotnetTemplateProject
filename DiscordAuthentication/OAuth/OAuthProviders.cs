namespace DiscordAuthentication.OAuth;

public class OAuthProviders
{
    public Dictionary<string, OAuthProvider> Providers { get; set; } = [];
}

public class OAuthProvider
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set;  } = string.Empty;
}
