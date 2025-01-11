using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eraware.StencilExtensions
{
    public static class SpaBuilderExtensions
    {
        private static bool isStencilDevServerStarted = false; // Static flag to ensure only one instance starts
        private static bool isStencilDevServerReady = false; // Flag to avoid redundant readiness checks
        private static readonly object lockObj = new object(); // Lock for thread safety

        /// <summary>
        /// Uses the stencil development server.
        /// </summary>
        /// <param name="spa">The spa pipeline to extend.</param>
        /// <param name="npmScript">The NPM script to start (watch) the dev server.</param>
        /// <param name="sourcePath">The source path where to run the npm script.</param>
        public static void UseStencilDevelopmentServer(this ISpaBuilder spa, string npmScript = "start", string sourcePath = "wwwroot")
        {
            var app = spa.ApplicationBuilder;
            var serviceProvider = app.ApplicationServices;
            var env = serviceProvider.GetRequiredService<IHostEnvironment>();

            if (env.IsDevelopment())
            {
                spa.Options.SourcePath = sourcePath;

                spa.UseProxyToSpaDevelopmentServer(async () =>
                {
                    var stencilDevServerUrl = "http://localhost:3333";

                    lock (lockObj)
                    {
                        if (!isStencilDevServerStarted)
                        {
                            // Start the Stencil dev server in a new terminal
                            var processStartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c start npm run {npmScript}",
                                WorkingDirectory = spa.Options.SourcePath,
                                UseShellExecute = true,
                                CreateNoWindow = false
                            };

                            var process = Process.Start(processStartInfo);
                            if (process == null)
                            {
                                throw new InvalidOperationException("Failed to start the Stencil dev server.");
                            }

                            Console.WriteLine("Started Stencil dev server in a new terminal.");
                            isStencilDevServerStarted = true; // Mark the server as started
                        }
                    }

                    // Poll the server until it's ready
                    // Check readiness only if not already confirmed
                    if (!isStencilDevServerReady)
                    {
                        Console.WriteLine($"Waiting for Stencil dev server at {stencilDevServerUrl}...");
                        await WaitForDevServer(stencilDevServerUrl);
                        isStencilDevServerReady = true; // Mark the server as ready
                        Console.WriteLine("Stencil dev server is ready.");
                    }

                    return new Uri(stencilDevServerUrl);
                });
            }
        }

        private static async Task WaitForDevServer(string url, int timeout = 30000, int pollInterval = 500)
        {
            using var httpClient = new HttpClient();
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                try
                {
                    // Send a GET request to the dev server
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return; // Server is ready
                    }
                }
                catch
                {
                    // Ignore exceptions (server is not ready yet)
                }

                await Task.Delay(pollInterval); // Wait before retrying
            }

            throw new TimeoutException($"Stencil dev server did not start within the timeout period ({timeout / 1000} seconds).");
        }
    }

}
