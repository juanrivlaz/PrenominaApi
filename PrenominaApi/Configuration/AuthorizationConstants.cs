namespace PrenominaApi.Configuration
{
    public static class AuthorizationConstants
    {
        /// <summary>
        /// Obtiene la clave JWT de la variable de entorno.
        /// NUNCA hardcodear secretos en código fuente.
        /// </summary>
        public static string GetJwtSecretKey()
        {
            var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY", EnvironmentVariableTarget.Machine)
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "JWT_SECRET_KEY environment variable is not set. " +
                    "Please set it using: setx JWT_SECRET_KEY \"your-256-bit-secret-key\" /M");
            }

            // Validar longitud mínima de la clave (256 bits = 32 bytes = ~43 chars en base64)
            if (key.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT_SECRET_KEY must be at least 32 characters (256 bits) for security.");
            }

            return key;
        }

        /// <summary>
        /// Clave JWT para fallback en desarrollo únicamente.
        /// Esta constante está deprecada y solo debe usarse en desarrollo local.
        /// </summary>
        [Obsolete("Use GetJwtSecretKey() instead. This constant should only be used in development.")]
        public const string JWT_SECRET_KEY = "SecretKeyOfDoomThatMustBeAMinimumNumberOfBytes";
    }
}
