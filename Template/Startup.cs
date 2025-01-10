namespace DotnetWebapiStencil
{
    using Eraware.StencilExtensions;

    internal static class Startup{
        public static void ConfigureServices(IServiceCollection services){
            services.AddControllers();
            services.AddStencil();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        internal static void ConfigureApp(WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "wwwroot/www";
                    if (app.Environment.IsDevelopment())
                    {
                        spa.UseStencilDevelopmentServer("start");
                    }
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}