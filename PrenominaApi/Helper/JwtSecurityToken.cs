using TokenJWT = System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using PrenominaApi.Models.Prenomina;
using System.Text;
using System.Security.Claims;

namespace PrenominaApi.Helper
{
    public class JwtSecurityToken
    {
        public static string CreateJwt(User user, string jwtKey, string jwtIssuer, int durationExpires)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(TokenJWT.JwtRegisteredClaimNames.GivenName, user.Name),
                new Claim(TokenJWT.JwtRegisteredClaimNames.Email, user.Email),
                new Claim(TokenJWT.JwtRegisteredClaimNames.Aud, jwtIssuer),
                new Claim("UserId", user.Id.ToString()),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim("RoleCode", user.Role?.Code ?? ""),
                new Claim(TokenJWT.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }.ToList();

            var token = new TokenJWT.JwtSecurityToken(jwtIssuer, null, claims, expires: DateTime.Now.AddMinutes(durationExpires), signingCredentials: credentials);

            return new TokenJWT.JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
