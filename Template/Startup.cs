namespace DotnetWebapiStencil
{
    using Eraware.StencilExtensions;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Writers;
    using NSwag.CodeGeneration.TypeScript;
    using Swashbuckle.AspNetCore.Swagger;
    using System.Reflection;

    internal static class Startup{
        public static void ConfigureServices(IServiceCollection services){
            services.AddControllers();
            services.AddStencil();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "DotnetWebapiStencil API", Version = "v1"
                    });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
            services.AddSwaggerDocument();
        }

        internal static void ConfigureApp(WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                GenerateTypescriptClient(app);
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
#pragma warning disable ASP0014 // Suggest using top level route registrations
            app.UseEndpoints(c => c.MapDefaultControllerRoute());
#pragma warning restore ASP0014 // Suggest using top level route registrations

            if (app.Environment.IsDevelopment())
            {
                app.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "wwwroot/www";
                    spa.UseStencilDevelopmentServer();
                });
            }
            else
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot/www")),
                    RequestPath = "",
                });
            }
        }

        private static void GenerateTypescriptClient(WebApplication app)
        {
            var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
            var swaggerDoc = swaggerProvider.GetSwagger("v1");
            var stringWriter = new StringWriter();
            var openApiWriter = new OpenApiJsonWriter(stringWriter);
            swaggerDoc.SerializeAsV3(openApiWriter);
            var swaggerJson = stringWriter.ToString();
            var nswagDocument = NSwag.OpenApiDocument.FromJsonAsync(swaggerJson).Result;

            foreach (var controller in nswagDocument.Operations.GroupBy(o => o.Operation.Tags.FirstOrDefault()))
            {
                if (controller.Key is null)
                {
                    continue;
                }

                var generatorSettings = new TypeScriptClientGeneratorSettings
                {
                    ClassName = $"{controller.Key}Client", // Name the client based on the controller
                    Template = TypeScriptTemplate.Fetch,
                };

                var generator = new TypeScriptClientGenerator(nswagDocument, generatorSettings);
                var clientCode = generator.GenerateFile();

                var sourceCodePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));
                var fileName = Path.Combine($"{controller.Key}Client.ts");
                var outputDir = Path.Combine(sourceCodePath, "wwwroot", "src", "services", "api-clients");
                var path = Path.Combine(outputDir, fileName);
                Directory.CreateDirectory(outputDir);
                File.WriteAllText(path, clientCode);
            }


        }
    }
}