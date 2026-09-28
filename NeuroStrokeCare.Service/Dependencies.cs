using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Service.Auth;

namespace NeuroStrokeCare.Service
{
    public static class Dependencies
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
