namespace DotnetWebapiStencil
{
    /// <summary>
    /// The main program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments passed when started.</param>
        public static void Main(string[] args)
        {
            var app = CreateApp(args);
            app.Run();
        }

        internal static WebApplication CreateApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            Startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            Startup.ConfigureApp(app);
            
            return app;
        }
    }
}
