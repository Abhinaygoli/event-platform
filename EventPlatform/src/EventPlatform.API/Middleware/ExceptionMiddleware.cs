using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Net;
using System.Text.Json;

namespace EventPlatform.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        // This method is responsible for invoking the next middleware in the pipeline and catching any unhandled exceptions that occur during the request processing.
        // If an exception is caught, it logs the error and returns a standardized JSON response with an appropriate HTTP status code based on the type of exception.
        //middleware acts as a global exception handler for all API requests.
        //Request comes into ASP.NET Core
        //It enters ExceptionMiddleware
        //Middleware calls _next(context)
        //Your controller/service code runs
        //throw new UnauthorizedAccessException(...) happens
        //Exception bubbles back up
        //ExceptionMiddleware catches it and returns a response
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (status, message) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                InvalidOperationException => (HttpStatusCode.Conflict, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var json = JsonSerializer.Serialize(new { message, status = (int)status });
            return context.Response.WriteAsync(json);
        }
    }
}
