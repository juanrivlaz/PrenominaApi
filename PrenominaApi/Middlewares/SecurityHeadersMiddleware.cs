namespace PrenominaApi.Middlewares
{
    /// <summary>
    /// Middleware que agrega headers de seguridad HTTP a todas las respuestas.
    /// Implementa las mejores prácticas de OWASP para headers de seguridad.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Agregar headers de seguridad antes de procesar la solicitud
            AddSecurityHeaders(context.Response.Headers);

            await _next(context);
        }

        private static void AddSecurityHeaders(IHeaderDictionary headers)
        {
            // Prevenir ataques de clickjacking
            headers.Append("X-Frame-Options", "DENY");

            // Prevenir MIME type sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // Habilitar protección XSS del navegador
            headers.Append("X-XSS-Protection", "1; mode=block");

            // Política de referrer restrictiva
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content Security Policy para APIs
            headers.Append("Content-Security-Policy",
                "default-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'none'");

            // Prevenir caché de respuestas sensibles
            headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");

            // Permissions Policy (anteriormente Feature-Policy)
            headers.Append("Permissions-Policy",
                "accelerometer=(), " +
                "camera=(), " +
                "geolocation=(), " +
                "gyroscope=(), " +
                "magnetometer=(), " +
                "microphone=(), " +
                "payment=(), " +
                "usb=()");

            // Remover headers que exponen información del servidor
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
        }
    }

    /// <summary>
    /// Extension methods para registrar el middleware de headers de seguridad.
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
