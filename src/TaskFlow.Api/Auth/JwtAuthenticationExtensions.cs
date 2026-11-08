using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Api.Auth;

public static class JwtAuthenticationExtensions
{
    private const string MissingKeyMessage =
        "Jwt:SigningKey is missing or shorter than 32 bytes. For a local run, set it with "
        + "'dotnet user-secrets set \"Jwt:SigningKey\" \"<random 32+ byte value>\" --project src/TaskFlow.Api'; "
        + "elsewhere use the Jwt__SigningKey environment variable.";

    /// <summary>
    /// JWT bearer authentication for the tokens issued by <see cref="JwtTokenService"/>.
    /// Settings are validated at startup, so a missing or weak key stops the app from starting.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey ?? string.Empty) >= JwtOptions.MinSigningKeyBytes, MissingKeyMessage)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Issuer and Jwt:Audience are required.")
            .Validate(o => o.ExpirationMinutes > 0, "Jwt:ExpirationMinutes must be positive.")
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names as issued ("sub", "email") instead of mapping them to long URIs.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                };
                bearer.Events = new JwtBearerEvents
                {
                    // A correctly signed token that does not say whose request it is gets 401 here,
                    // instead of reaching a controller that cannot scope data to a user.
                    OnTokenValidated = context =>
                    {
                        if (context.Principal is null || !context.Principal.TryGetUserId(out _))
                        {
                            context.Fail("The token has no valid subject.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }
}
