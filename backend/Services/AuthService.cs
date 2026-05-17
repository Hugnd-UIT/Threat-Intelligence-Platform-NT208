using backend.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace backend.Services
{
    public class AuthService
    {
        private readonly UsersRepository _usersRepository;
        private readonly IConfiguration _configuration;

        public AuthService(UsersRepository usersRepository, IConfiguration configuration)
        {
            _usersRepository = usersRepository;
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(string Username, string Password)
        {
            var MatchedUser = await _usersRepository.GetByUsernameAsync(Username);

            if (MatchedUser == null)
            {
                return null;
            }

            string DatabasePassword = (string)MatchedUser.password ?? "";
            bool IsPasswordValid = BCrypt.Net.BCrypt.Verify(Password, DatabasePassword);

            if (!IsPasswordValid)
            {
                return null;
            }

            bool IsAccountLocked = (bool)(MatchedUser.isLocked ?? false);
            
            if (IsAccountLocked)
            {
                throw new UnauthorizedAccessException("Your account has been locked!");
            }

            string SessionToken = Guid.NewGuid().ToString();
            string UserKey = (string)MatchedUser._key;
            string UserRole = (string)MatchedUser.role ?? "User";

            await _usersRepository.UpdateSessionTokenAsync(UserKey, SessionToken);

            var SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
            var SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            
            var JwtToken = new JwtSecurityToken(
                claims: new[] 
                {
                    new Claim(ClaimTypes.Name, (string)MatchedUser.username),
                    new Claim(ClaimTypes.Role, UserRole),
                    new Claim("SessionToken", SessionToken)
                },
                expires: DateTime.Now.AddHours(2),
                signingCredentials: SigningCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(JwtToken);
        }
    }
}