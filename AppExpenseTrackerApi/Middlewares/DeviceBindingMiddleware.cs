namespace AppExpenseTrackerApi.Middlewares
{
    public class DeviceBindingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DeviceBindingMiddleware> _logger;

        public DeviceBindingMiddleware(RequestDelegate next, ILogger<DeviceBindingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            // Only validate if user is authenticated via JWT Bearer
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var dfpClaim = context.User.FindFirst("dfp")?.Value;

                // If token contains a device fingerprint claim, it is strictly bound to that client
                if (!string.IsNullOrEmpty(dfpClaim))
                {
                    var clientFingerprint = context.Request.Headers["X-Device-Fingerprint"].FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(clientFingerprint) ||
                        !tokenService.ValidateDeviceFingerprint(dfpClaim, clientFingerprint))
                    {
                        _logger.LogWarning("Device binding mismatch detected for User '{User}' from IP '{IP}'. Missing or unauthorized device fingerprint.",
                            context.User.Identity.Name, context.Connection.RemoteIpAddress);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var failResponse = Services.ViewModels.ApiViewModels.ApiResponse.Fail(
                            "Unauthorized device: This token is cryptographically bound to a different client device and cannot be used here."
                        );

                        await context.Response.WriteAsJsonAsync(failResponse);
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
