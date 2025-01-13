# Dotnet WebAPI with Stencil.js SPA Middleware Template

This project template provides a starting point for building a .NET WebAPI application integrated with a Stencil.js-based Single Page Application (SPA). It includes middleware for running the Stencil development server during development and serving the static files during production.

## Features

- **API and SPA Integration**: Combines WebAPI endpoints with a modern Stencil.js frontend.
- **Development Server Support**: Seamlessly proxies requests to the Stencil.js development server during development.
- **Production-Ready Setup**: Serves static files from a specified `wwwroot` directory in production.
- **TypeScript Client Generation**: Automatically generates TypeScript clients for the API controllers.

---

## Using the template

To install the template, run `dotnet new install Eraware.StencilWebApiTemplate`

To create a project using the template, create a folder using whatever project name you would like, then inside that folder run `dotnet new webapi-stencil-starter`. Alternatively you can use -n to name your project and -o to set the output folder. Ex `dotnet new webapi-stencil-starter -n MyProject -o ./MyProject`
Then inside the folder simply run `dotnet watch` and you can start coding both the backend and frontent while enjoying live-reload and HMR. If you prefer a Visual Studio workflow you can simply open the project file and Visual Studio will propose making it a solution.

## Using the Stencil SPA Middleware

The middleware makes it easy to integrate a Stencil.js SPA into your application. It proxies requests during development to the Stencil.js development server and serves static files in production.

### Configuration

To use the middleware:

1. Add the services and the middleware to your app's startup logic :

   ```csharp
   services.AddStencil();
   ...
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
        app.UseSpaStaticFiles("wwwroot/www");
    }
    ...
    ```
2. Create your stencil app at `wwwroot/www`.
3. Run `dotnet watch` and enjoy live-reload.
4. Modify your APIs and get automatic client classes in typescript generated upon build.
