using System.Text;
using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;

        public IdempotencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, EscrowContext dbContext)
        {
            if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues))
            {
                // No idempotency key provided, proceed normally
                await _next(context);
                return;
            }

            var idempotencyKey = idempotencyKeyValues.ToString();
            
            // Limit to POST for now
            if (context.Request.Method != HttpMethods.Post)
            {
                await _next(context);
                return;
            }

            var existingRecord = await dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);

            if (existingRecord != null)
            {
                // Return cached response
                context.Response.StatusCode = existingRecord.StatusCode;
                context.Response.ContentType = "application/json";
                if (!string.IsNullOrEmpty(existingRecord.ResponseBody))
                {
                    await context.Response.WriteAsync(existingRecord.ResponseBody);
                }
                return;
            }

            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                // Read and save response body
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var record = new IdempotencyRecord
                {
                    IdempotencyKey = idempotencyKey,
                    StatusCode = context.Response.StatusCode,
                    ResponseBody = responseBodyText,
                    RequestPath = context.Request.Path.ToString()
                };

                dbContext.IdempotencyRecords.Add(record);
                await dbContext.SaveChangesAsync();

                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
