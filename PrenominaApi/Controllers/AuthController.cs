using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;
using InputDto = PrenominaApi.Models.Dto.Input;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IBaseServicePrenomina<User> _service;

        public AuthController(IBaseServicePrenomina<User> service)
        {
            _service = service;
        }

        /// <summary>
        /// Endpoint de autenticación con rate limiting para prevenir ataques de fuerza bruta.
        /// Límite: 5 intentos por minuto por IP.
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("login")]
        public ActionResult<ResultLogin> Login([FromBody] InputDto.AuthLogin credentials)
        {
            // Validar que las credenciales no estén vacías
            if (credentials == null)
            {
                return BadRequest(new { Message = "Las credenciales son requeridas." });
            }

            if (string.IsNullOrWhiteSpace(credentials.Email))
            {
                return BadRequest(new { Message = "El correo electrónico es requerido." });
            }

            if (string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest(new { Message = "La contraseña es requerida." });
            }

            // Validar formato de email básico
            if (!IsValidEmail(credentials.Email))
            {
                return BadRequest(new { Message = "El formato del correo electrónico no es válido." });
            }

            var result = _service.ExecuteProcess<InputDto.AuthLogin, ResultLogin>(credentials);

            return Ok(result);
        }

        /// <summary>
        /// Valida el formato básico de un email.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Validación básica de formato
                var trimmedEmail = email.Trim();
                if (trimmedEmail.Length > 254) // RFC 5321
                    return false;

                var atIndex = trimmedEmail.LastIndexOf('@');
                if (atIndex <= 0 || atIndex >= trimmedEmail.Length - 1)
                    return false;

                var localPart = trimmedEmail[..atIndex];
                var domainPart = trimmedEmail[(atIndex + 1)..];

                // Validaciones básicas
                if (localPart.Length > 64 || domainPart.Length > 253)
                    return false;

                if (!domainPart.Contains('.'))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
