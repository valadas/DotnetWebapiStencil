using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Eraware.StencilExtensions
{
    public static class ServiceCollectionExtensions{
        public static IServiceCollection AddStencil(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var environment = provider.GetRequiredService<IHostEnvironment>();

                if (environment.IsDevelopment())
                {
                    // Remove any existing CORS configurations
                    services.RemoveAll<ICorsPolicyProvider>();

                    // Add a new default CORS policy
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder
                                .AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                    });
                }

                return services;
            });

            return services;
        }
    }
}