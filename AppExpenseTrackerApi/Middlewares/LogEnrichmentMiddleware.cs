using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions; // Added for Regex
using Serilog;

namespace AppExpenseTrackerApi.Middlewares
{
    public class LogEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public LogEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser, IDiagnosticContext diagnosticContext)
        {
            var sw = Stopwatch.StartNew();
            var userId = currentUser?.UserName;

            // 1. Try to get RoomId from Query String (Common for GET)
            string roomIdStr = context.Request.Query["roomId"].ToString();

            // 2. Capture Request Data
            string requestData = context.Request.Method == "GET"
                ? context.Request.QueryString.Value ?? string.Empty
                : await ReadRequestBody(context);

            // 3. If RoomId not in Query, try to find it in the Body (Common for POST/PUT)
            if (string.IsNullOrEmpty(roomIdStr) && !string.IsNullOrEmpty(requestData))
            {
                // Regex looks for "roomId": 123 or "RoomId": 123
                var match = Regex.Match(requestData, @"""roomId""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    roomIdStr = match.Groups[1].Value;
                }
            }
            if (context.Request.Path.Value.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                requestData = MaskSensitiveData(requestData);
            }
            // 4. Setup Response Body Capture
            var originalBodyStream = context.Response.Body;
            using var responseBodyMemoryStream = new MemoryStream();
            context.Response.Body = responseBodyMemoryStream;

            try
            {
                await _next(context); // Execute API

                // 5. Capture Response Data
                responseBodyMemoryStream.Position = 0;
                string responseData = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();

                // Reset position and copy back to original stream
                responseBodyMemoryStream.Position = 0;
                await responseBodyMemoryStream.CopyToAsync(originalBodyStream);

                sw.Stop();

                // 6. Push to DiagnosticContext for Serilog
                diagnosticContext.Set("UserId", userId);
                diagnosticContext.Set("RequestUrl", context.Request.Path.Value);
                diagnosticContext.Set("RequestData", requestData);
                diagnosticContext.Set("ResponseData", responseData);
                diagnosticContext.Set("StatusCode", context.Response.StatusCode);
                diagnosticContext.Set("TimeTakenMs", sw.Elapsed.TotalMilliseconds);

                // Safely set RoomId as an integer if possible
                if (int.TryParse(roomIdStr, out int roomIdInt))
                {
                    diagnosticContext.Set("RoomId", roomIdInt);
                }
                else
                {
                    diagnosticContext.Set("RoomId", null);
                }
            }
            catch (Exception)
            {
                sw.Stop();
                throw;
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
        private string MaskSensitiveData(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            // Regex to find "password": "..." and replace the value
            return Regex.Replace(json, @"(""password""\s*:\s*"")(.*?)("")", "$1*******$3", RegexOptions.IgnoreCase);
        }
    }
}