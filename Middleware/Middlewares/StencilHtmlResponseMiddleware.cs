using Microsoft.AspNetCore.Http;
using System.Text;

namespace Eraware.StencilExtensions.Middlewares
{
    /// <summary>
    /// A middleware to serve the Stencil HTML response.
    /// </summary>
    public class StencilHtmlResponseMiddleware
    {
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="StencilHtmlResponseMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next request delegate to call.</param>
        public StencilHtmlResponseMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Handles request invokation.
        /// </summary>
        /// <param name="context">The current http context.</param>
        /// <returns>An awaitable task.</returns>
        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path) || !(path == "/" || path.Contains("index.html")))
            {
                // We escape early if the request is not for the index.html file.
                await this.next(context);
                return;
            }

            await this.InterceptRequest(context);
        }

        private async Task InterceptRequest(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                using (var newBodyStream = new MemoryStream())
                {
                    context.Response.Body = newBodyStream;

                    await this.next(context);

                    if (this.IsHtmlResponse(context))
                    {
                        await this.ModifyHtmlResponse(
                            context,
                            newBodyStream,
                            originalBodyStream);
                        return;
                    }

                    await this.CopyOriginalResponse(newBodyStream, originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private bool IsHtmlResponse(HttpContext context)
        {
            return context.Response.ContentType != null &&
                context.Response.ContentType.Contains("text/html");
        }

        private async Task ModifyHtmlResponse(
            HttpContext context,
            MemoryStream newBodyStream,
            Stream originalBodyStream)
        {
            newBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(newBodyStream).ReadToEndAsync();

            // Modify the HTML response as needed
            var host = context.Request.Host.Value; // e.g., "localhost:5000"
            var scheme = context.Request.Scheme;  // e.g., "http" or "https"
            var placeholder = "localhost:3333";
            var replacement = $"{scheme}://{host}";
            responseBody = responseBody.Replace(placeholder, replacement);

            var modifiedBytes = Encoding.UTF8.GetBytes(responseBody);
            context.Response.Body = originalBodyStream;
            await context.Response.Body.WriteAsync(modifiedBytes, 0, modifiedBytes.Length);
        }

        private async Task CopyOriginalResponse(
            MemoryStream newBodyStream,
            Stream originalBodyStream)
        {
            newBodyStream.Seek(0, SeekOrigin.Begin);
            await newBodyStream.CopyToAsync(originalBodyStream);
        }
    }
}
