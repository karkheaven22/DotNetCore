using Microsoft.AspNetCore.Builder;
using System;

namespace DotNetCore.Library.HttpLogging
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.UseMiddleware<LogMiddleware>();
            return app;
        }
    }
}