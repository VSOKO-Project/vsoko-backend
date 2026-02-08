using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using coo.Domain.Common.Exceptions;
using coo.Application.Common.Interfaces;
using coo.Presentation.Services;

namespace coo.Infrastructure.SecurityManager.Tokens;

public static class DI
{
    public static IServiceCollection ApplyUserContext(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, UserContext>();

        return services;
    } 
}