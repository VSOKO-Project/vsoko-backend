using System.Net;
using System.Text;
using System.Xml.Serialization;
using Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.SecurityManager.Tokens;

public static class DI
{
    public static IServiceCollection ApplyTokenManager(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<TokenSettings>(configuration.GetSection("Jwt"));

        services
            .AddAuthentication(options =>
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme
            )
            .AddJwtBearer(options =>
            {
                var tokenSettings = configuration.GetSection("Jwt").Get<TokenSettings>();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = tokenSettings?.Issuer,
                    ValidAudience = tokenSettings?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            tokenSettings?.SecretKey ?? throw new DomainException()
                        )
                    ),
                    ClockSkew = TimeSpan.FromMinutes(tokenSettings?.ExpireInMinute ?? 60),
                };
            });

        services.AddTransient<TokenService>();

        services.AddTransient<ClaimService>();

        return services;
    }
}
