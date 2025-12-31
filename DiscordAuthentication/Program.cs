using AspNet.Security.OAuth.Discord;
using DiscordAuthentication.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
})
    .AddDiscord(options =>
    {
        //Not ideal need null checking
        var discordOptions = builder.Configuration.GetSection("OAuthProviders").Get<OAuthProviders>().Providers["Discord"];
        options.ClientId = discordOptions.ClientId;
        options.ClientSecret = discordOptions.ClientSecret;
        options.CallbackPath = discordOptions.CallbackUrl;
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.EventsType = typeof(DiscordAuthEventsType);
        //Makes it so users don't need to reauthorize each time
        options.Prompt = "none";

        options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
            string.Format(
            CultureInfo.InvariantCulture,
            "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
            user.GetString("id"),
            user.GetString("avatar"),
            user.GetString("avatar")!.StartsWith("a_") ? "gif" : "png"));
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "DiscordAuth";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddScoped<DiscordAuthEventsType>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/login", () =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/get-token", // The URL to redirect to after successful authentication
        IsPersistent = true // Ensures the session persists across requests (the authentication cookie will be stored)
    };

    // Triggers the OAuth challenge and redirects the user to Discord's authorization page
    return Results.Challenge(properties, [DiscordAuthenticationDefaults.AuthenticationScheme]);
});

app.MapGet("/weatherforecast", (HttpContext context) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
}).RequireAuthorization()
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class DiscordAuthEventsType(ILogger<DiscordAuthEventsType> logger) : OAuthEvents
{
    public override Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        //Save user to database
        return base.CreatingTicket(context);
    }
}
