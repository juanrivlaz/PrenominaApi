using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina;
using System.Security.Cryptography;

namespace PrenominaApi.Helper
{
    public class CustomPasswordHasher : IPasswordHasher<HasPassword>
    {
        public string HashPassword(HasPassword user, string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password,
                salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            ));

            return $"{Convert.ToBase64String(salt)}:{hashed}";
        }

        public PasswordVerificationResult VerifyHashedPassword(HasPassword user, string hashedPassword, string providedPassword)
        {
            // Dividir la sal y el hash almacenado
            var parts = hashedPassword.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            // Crear el hash usando la sal y la contraseña proporcionada
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Verificar si los hashes coinciden
            return storedHash == hashed ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
    }
}
