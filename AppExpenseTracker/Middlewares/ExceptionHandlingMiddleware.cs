using Domain.Exceptions;
using Services.ViewModels.ApiViewModels;
using System.Text.Json;

namespace AppExpenseTracker.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex, ILogger logger)
        {
            ApiResponse response;
            int statusCode;

            switch (ex)
            {
                case System.ComponentModel.DataAnnotations.ValidationException ve:
                    statusCode = StatusCodes.Status400BadRequest;
                    response = ApiResponse.Fail(ve.Message);
                    logger.LogWarning(ex, "Validation error: {Message}", ve.Message);
                    break;

                case NotFoundException nfe:
                    statusCode = StatusCodes.Status404NotFound;
                    response = ApiResponse.Fail(nfe.Message);
                    logger.LogWarning(ex, "Not found: {Message}", nfe.Message);
                    break;

                case UnauthorizedException ue:
                    statusCode = StatusCodes.Status401Unauthorized;
                    response = ApiResponse.Fail(ue.Message);
                    logger.LogWarning(ex, "Unauthorized: {Message}", ue.Message);
                    break;

                case ForbiddenException fe:
                    statusCode = StatusCodes.Status403Forbidden;
                    response = ApiResponse.Fail(fe.Message);
                    logger.LogWarning(ex, "Forbidden: {Message}", fe.Message);
                    break;

                case ConflictException ce:
                    statusCode = StatusCodes.Status409Conflict;
                    response =  ApiResponse.Fail(ce.Message);
                    logger.LogWarning(ex, "Conflict: {Message}", ce.Message);
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = ApiResponse.Fail("Database update failed.");
                    logger.LogError(dbEx, "Database update exception");
                    break;

                case Microsoft.Data.SqlClient.SqlException sqlEx:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = ApiResponse.Fail("Database connection error.");
                    logger.LogError(sqlEx, "SQL exception occurred");
                    break;

                case TimeoutException te:
                    statusCode = StatusCodes.Status408RequestTimeout;
                    response = ApiResponse.Fail("The request timed out.");
                    logger.LogError(te, "Timeout exception");
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = ApiResponse.Fail("An unexpected error occurred.");
                    logger.LogError(ex, "Unhandled exception");
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}