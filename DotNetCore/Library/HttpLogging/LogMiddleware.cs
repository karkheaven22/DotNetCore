using LogHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace DotNetCore.Library.HttpLogging
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<HttpLoggingOptions> _options;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly List<string> _allowHeader = new() { "Authorization", "Content-Type", "Content-Length", "User-Agent" };

        public LogMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            ContextNullException(httpContext);
            var syncIOFeature = httpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null) syncIOFeature.AllowSynchronousIO = true;

            long startTimestamp = Stopwatch.GetTimestamp();
            LogRequestAsync(httpContext);
            await LogResponseAsync(httpContext, startTimestamp);
        }

        private static double GetElapsedSeconds(long start, long stop)
        {
            return (stop - start) / (double)Stopwatch.Frequency;
        }

        private static void ContextNullException(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));
        }

        internal void FilterHeaders(IHeaderDictionary headers)
        {
            foreach (var (key, value) in headers)
                if (_allowHeader.Contains(key))
                    Log.Info($"[{key}] {value}");
        }

        private async Task LogResponseAsync(HttpContext httpContext, long startTimestamp)
        {
            var ElapsedTime = GetElapsedSeconds(startTimestamp, Stopwatch.GetTimestamp());
            var originalBodyFeature = httpContext.Features.Get<IHttpResponseBodyFeature>()!;
            var ResponseBody = new ResponseBufferingStream(httpContext, originalBodyFeature, ElapsedTime);
            httpContext.Features.Set<IHttpResponseBodyFeature>(ResponseBody);
            try
            {
                httpContext.Response.OnStarting(state =>
                {
                    var responseContext = (HttpContext)state;
                    responseContext.Response.Headers.Remove("expires");
                    responseContext.Response.Headers.Remove("pragma");
                    //remove the header you don't want in the `responseContext`
                    return Task.CompletedTask;
                }, httpContext);
                await _next(httpContext);
                if (ResponseBody.HasLogged)
                    Log.Info(ResponseBody.ResponseText!);
            }
            finally
            {
                httpContext.Features.Set(originalBodyFeature);
            }
        }

        private void LogRequestAsync(HttpContext httpContext)
        {
            var options = _options.CurrentValue;
            if ((HttpLoggingFields.Request & options.LoggingFields) != HttpLoggingFields.None)
            {
                var request = httpContext.Request;
                request.EnableBuffering();
                var requestBufferingStream = new RequestBufferingStream(httpContext, options, Encoding.UTF8);
                request.Body = requestBufferingStream;
            }
        }
    }
}