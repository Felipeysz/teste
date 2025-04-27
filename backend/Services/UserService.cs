// backend/Services/UserService.cs
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
    public class UserService
    {
        private readonly UserBusinessRules _rules;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _jwtSecret;
        private static readonly List<string> _activeTokens = new();

        public UserService(
            UserBusinessRules rules,
            IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _rules     = rules;
            _jwtSecret = configuration["JwtSettings:Key"]
                         ?? throw new ArgumentNullException("JwtSettings:Key", "Chave JWT não configurada.");
            _dbContext = dbContext;
        }

        public IEnumerable<string> ActiveTokens => _activeTokens;

        // Criar usuário a partir de DTO
        public async Task CreateUserAsync(RegisterDTO dto)
        {
            _rules.ValidateRegisterRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);
            _rules.ValidatePasswordComplexity(dto.Password);

            // Verifica unicidade
            if (await _dbContext.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email já existente.");
            if (await _dbContext.Users.AnyAsync(u => u.Name == dto.Name))
                throw new InvalidOperationException("Nome já existente.");

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PermissionAccount = dto.PermissionAccount,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }

        // Login a partir de DTO
        public async Task<string> LoginAsync(LoginDTO dto)
        {
            _rules.ValidateLoginRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Email não encontrado.");

            _rules.ValidatePassword(user.Password, dto.Password);

            if (IsUserLoggedIn(dto.Email))
                throw new UnauthorizedAccessException("Já existe um login ativo para esta conta.");

            var token = GenerateJwtToken(user);
            _activeTokens.Add(token);
            return token;
        }

        private bool IsUserLoggedIn(string email)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = false,
                ClockSkew                = TimeSpan.Zero
            };

            foreach (var token in _activeTokens.ToList())
            {
                try
                {
                    var principal = handler.ValidateToken(token, parameters, out var validated);
                    var jwt = (JwtSecurityToken)validated;
                    var tokenEmail = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;

                    if (string.Equals(tokenEmail, email, StringComparison.OrdinalIgnoreCase)
                        && jwt.ValidTo > DateTime.UtcNow)
                        return true;
                }
                catch
                {
                    _activeTokens.Remove(token);
                }
            }
            return false;
        }

        public void Logout(string token)
            => _activeTokens.Remove(token);

        public async Task<List<UserAccount>> GetUsersAsync()
            => await _dbContext.Users.ToListAsync();

        // Atualização a partir de DTO
        public async Task UpdateUserAsync(string id, RegisterDTO dto)
        {
            if (!Guid.TryParse(id, out var uid))
                throw new ArgumentException("ID inválido.");

            _rules.ValidateUpdateRequired(dto);
            _rules.ValidateEmailFormat(dto.Email);

            var user = await _dbContext.Users.FindAsync(uid);
            if (user == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.Email != dto.Email && await _dbContext.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email já em uso.");
            if (user.Name != dto.Name && await _dbContext.Users.AnyAsync(u => u.Name == dto.Name))
                throw new InvalidOperationException("Nome já em uso.");

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.PermissionAccount = dto.PermissionAccount;
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(string id)
        {
            if (!Guid.TryParse(id, out var uid))
                throw new ArgumentException("ID inválido.");

            var user = await _dbContext.Users.FindAsync(uid);
            if (user == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("role", user.PermissionAccount)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}