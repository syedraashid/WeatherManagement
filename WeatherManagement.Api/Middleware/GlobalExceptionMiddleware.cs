using System.Net;
using System.Text.Json;
namespace WeatherManagement.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var statusCode = ex switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = GetSafeMessage(ex, statusCode),
                TraceId = context.TraceIdentifier
            };

            var json = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(json);
        }

        private static string GetSafeMessage(Exception ex, HttpStatusCode statusCode)
        {
            // Avoid exposing internal details in production
            return statusCode == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : ex.Message;
        }
    }
}
