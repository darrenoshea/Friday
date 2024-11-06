using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Index");
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oauth";
    })
    .AddCookie("Cookies")
    .AddOAuth("oauth", options =>
    {
        options.AuthorizationEndpoint = "https://auth.monday.com/oauth2/authorize";
        options.CallbackPath = "/signin-oauth";
        options.ClientId = "REPLACE";
        options.ClientSecret = "REPLACE";
        options.TokenEndpoint = "https://auth.monday.com/oauth2/token";
        options.Scope.Add("boards:read");

        options.SaveTokens = true;

        // Define how to map returned user data to claims
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "uid");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "uid");

        options.Events = new OAuthEvents
        {
            // After OAuth2 has authenticated the user
            OnCreatingTicket = context =>
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(context.AccessToken);
                var document = JsonDocument.Parse(JsonSerializer.Serialize(jwtSecurityToken.Payload));
                var user = document.RootElement;

                context.RunClaimActions(user);
                context.Identity.AddClaim(new Claim("token", context.AccessToken));
                return Task.CompletedTask;
            },
            OnTicketReceived = context =>
            {
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
                return Task.FromResult(0);
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
