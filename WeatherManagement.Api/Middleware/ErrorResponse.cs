namespace WeatherManagement.Api.Middleware
{
    internal class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string TraceId { get; set; }
    }
}