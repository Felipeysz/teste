using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using backend.DTO;
using backend.Models;
using backend.BusinessRules;
using backend.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly UserBusinessRules _rules;
        private readonly ApplicationDbContext _db;
        private readonly string _jwtKey;
        private static readonly List<string> _activeTokens = new();

        public UserService(UserBusinessRules rules, IConfiguration config, ApplicationDbContext db)
        {
            _rules  = rules;
            _jwtKey = config["JwtSettings:Key"]
                   ?? throw new ArgumentNullException("JwtSettings:Key");
            _db     = db;
        }

        public IEnumerable<string> ActiveTokens => _activeTokens;

        public async Task CreateUserAsync(RegisterDTO dto)
        {
            _rules.ValidateRegisterRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);
            _rules.ValidatePasswordComplexity(dto.Password);

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email já existente.");
            if (await _db.Users.AnyAsync(u => u.Name == dto.Name))
                throw new InvalidOperationException("Nome já existente.");

            var user = new UserAccount
            {
                Name              = dto.Name,
                Email             = dto.Email,
                PermissionAccount = dto.PermissionAccount,
                Password          = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task<string> LoginAsync(LoginDTO dto)
        {
            _rules.ValidateLoginRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email)
                       ?? throw new UnauthorizedAccessException("Email não encontrado.");

            _rules.ValidatePassword(user.Password, dto.Password);

            if (IsUserLoggedIn(dto.Email))
                throw new UnauthorizedAccessException("Já existe um login ativo.");

            var token = GenerateJwt(user);
            _activeTokens.Add(token);
            return token;
        }

        private bool IsUserLoggedIn(string email)
        {
            var handler = new JwtSecurityTokenHandler();
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var opts    = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = false,
                ClockSkew                = TimeSpan.Zero
            };

            foreach (var t in _activeTokens.ToArray())
            {
                try
                {
                    var p   = handler.ValidateToken(t, opts, out var validated);
                    var jwt = (JwtSecurityToken)validated;
                    var sub = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
                    if (sub.Equals(email, StringComparison.OrdinalIgnoreCase)
                     && jwt.ValidTo > DateTime.UtcNow)
                        return true;
                }
                catch
                {
                    _activeTokens.Remove(t);
                }
            }
            return false;
        }

        public void Logout(string token)
            => _activeTokens.Remove(token);

        public async Task<List<UserDTO>> GetUsersAsync()
            => await _db.Users
                        .Select(u => new UserDTO(u.Name, u.Email, u.PermissionAccount))
                        .ToListAsync();

        public async Task UpdateUserAsync(string name, RegisterDTO dto)
        {
            _rules.ValidateUpdateRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == name)
                       ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.Email != dto.Email && await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email já em uso.");
            if (user.Name  != dto.Name  && await _db.Users.AnyAsync(u => u.Name  == dto.Name))
                throw new InvalidOperationException("Nome já em uso.");

            user.Name              = dto.Name;
            user.Email             = dto.Email;
            user.PermissionAccount = dto.PermissionAccount;
            user.Password          = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(string name)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == name)
                       ?? throw new KeyNotFoundException("Usuário não encontrado.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        private string GenerateJwt(UserAccount u)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tok   = new JwtSecurityToken(
                claims: new[] {
                    new Claim(JwtRegisteredClaimNames.Sub, u.Email),
                    new Claim("role", u.PermissionAccount)
                },
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tok);
        }
    }
}
