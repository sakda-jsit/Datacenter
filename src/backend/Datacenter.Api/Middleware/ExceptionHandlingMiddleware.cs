using System.Net;
using System.Text.Json;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Domain.Exceptions;

namespace Datacenter.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title, errors) = exception switch
        {
            ValidationException ve => (HttpStatusCode.UnprocessableEntity, "ข้อมูลไม่ถูกต้อง", ve.Errors),
            UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message, (IDictionary<string, string[]>?)null),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, (IDictionary<string, string[]>?)null),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message, null),
            DomainException => (HttpStatusCode.BadRequest, exception.Message, null),
            _ => (HttpStatusCode.InternalServerError, "เกิดข้อผิดพลาดภายในระบบ", null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var response = new { title, status = (int)status, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
