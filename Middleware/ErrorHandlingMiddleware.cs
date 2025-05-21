using System.Net;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Collaborative_Task_Management_System.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred. Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // For API requests, return JSON error
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "An error occurred while processing your request."
                });
                return;
            }

            // For regular requests, redirect to error page
            context.Response.Redirect("/Home/Error");
        }
    }
}