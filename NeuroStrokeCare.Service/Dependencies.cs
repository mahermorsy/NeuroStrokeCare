using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Service.Auth;
using NeuroStrokeCare.Service.Email;

namespace NeuroStrokeCare.Service
{
    public static class Dependencies
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            return services;
        }
    }
}
