using System.Net;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace PrenominaApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, IHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Siempre loguear el error completo en el servidor
            Log.Error(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

            context.Response.ContentType = "application/json";

            var (statusCode, userMessage) = GetStatusCodeAndMessage(ex);
            context.Response.StatusCode = statusCode;

            // Crear respuesta segura
            var response = new ProblemDetails
            {
                Status = statusCode,
                Title = GetSafeTitle(statusCode),
                // Solo incluir el mensaje detallado si es un error de negocio conocido
                Detail = IsBusinessException(ex) ? ex.Message : userMessage,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };

            // Solo en desarrollo incluir información adicional de debug
            if (_environment.IsDevelopment())
            {
                response.Extensions["debugInfo"] = new
                {
                    exceptionType = ex.GetType().Name,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                };
            }

            await context.Response.WriteAsJsonAsync(response);
        }

        private static (int statusCode, string message) GetStatusCodeAndMessage(Exception ex)
        {
            return ex switch
            {
                BadHttpRequestException badRequest => (
                    StatusCodes.Status400BadRequest,
                    badRequest.Message
                ),
                UnauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    "No autorizado para realizar esta acción."
                ),
                KeyNotFoundException => (
                    StatusCodes.Status404NotFound,
                    "El recurso solicitado no fue encontrado."
                ),
                ArgumentException argEx => (
                    StatusCodes.Status400BadRequest,
                    argEx.Message
                ),
                InvalidOperationException invalidOp when IsBusinessException(invalidOp) => (
                    StatusCodes.Status400BadRequest,
                    invalidOp.Message
                ),
                OperationCanceledException => (
                    StatusCodes.Status408RequestTimeout,
                    "La operación fue cancelada o excedió el tiempo límite."
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Ocurrió un error interno. Por favor, inténtelo más tarde."
                )
            };
        }

        private static string GetSafeTitle(int statusCode)
        {
            return statusCode switch
            {
                400 => "Solicitud inválida",
                401 => "No autorizado",
                403 => "Acceso denegado",
                404 => "No encontrado",
                408 => "Tiempo de espera agotado",
                409 => "Conflicto",
                422 => "Entidad no procesable",
                429 => "Demasiadas solicitudes",
                500 => "Error interno del servidor",
                503 => "Servicio no disponible",
                _ => "Error"
            };
        }

        /// <summary>
        /// Determina si la excepción es un error de negocio conocido
        /// cuyo mensaje es seguro para mostrar al usuario.
        /// </summary>
        private static bool IsBusinessException(Exception ex)
        {
            // BadHttpRequestException generalmente contiene mensajes seguros para el usuario
            if (ex is BadHttpRequestException)
                return true;

            // Excepciones con mensajes específicos de validación de negocio
            var safeMessagePrefixes = new[]
            {
                "El código de incidencia",
                "No se encontró",
                "Ya existe",
                "No es posible",
                "El periodo",
                "La empresa",
                "El empleado",
                "Es necesario"
            };

            return safeMessagePrefixes.Any(prefix =>
                ex.Message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
