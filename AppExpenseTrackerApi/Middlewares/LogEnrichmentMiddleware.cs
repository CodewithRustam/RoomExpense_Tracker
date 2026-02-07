using System.Diagnostics;
using System.Text;
using Serilog; // Added for IDiagnosticContext

namespace AppExpenseTrackerApi.Middlewares
{
    public class LogEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public LogEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Added IDiagnosticContext to the parameters
        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser, IDiagnosticContext diagnosticContext)
        {
            var sw = Stopwatch.StartNew();
            var userId = currentUser?.UserName;

            // 1. Capture Request Data
            string requestData = context.Request.Method == "GET"
                ? context.Request.QueryString.Value ?? string.Empty
                : await ReadRequestBody(context);

            // 2. Setup Response Body Capture
            var originalBodyStream = context.Response.Body;
            using var responseBodyMemoryStream = new MemoryStream();
            context.Response.Body = responseBodyMemoryStream;

            try
            {
                await _next(context); // Execute API

                // 3. Capture Response Data
                responseBodyMemoryStream.Position = 0;
                string responseData = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();

                // Reset position and copy back to original stream so user gets the data
                responseBodyMemoryStream.Position = 0;
                await responseBodyMemoryStream.CopyToAsync(originalBodyStream);

                sw.Stop();

                // 4. Push to DiagnosticContext (This fixes the empty column issue)
                diagnosticContext.Set("UserId", userId);
                diagnosticContext.Set("RequestUrl", context.Request.Path.Value);
                diagnosticContext.Set("RequestData", requestData);
                diagnosticContext.Set("ResponseData", responseData);
                diagnosticContext.Set("StatusCode", context.Response.StatusCode);
                diagnosticContext.Set("TimeTakenMs", sw.Elapsed.TotalMilliseconds);

                // If you have a RoomId variable, set it here too
                // diagnosticContext.Set("RoomId", roomId);
            }
            catch (Exception)
            {
                sw.Stop();
                throw; // Let the global error handler deal with the exception
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }
    }
}