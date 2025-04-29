using System;
using System.Linq;
using System.Threading.Tasks;
using backend.DTO;
using backend.Services;
using backend.BusinessRules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _svc;
        private readonly UserBusinessRules _rules;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService svc,
            UserBusinessRules rules,
            ILogger<UserController> logger)
        {
            _svc = svc;
            _rules = rules;
            _logger = logger;
        }

        // Central handler for operations that return UserResponse
        private async Task<IActionResult> HandleAsync(
            Func<Task> operation,
            string successMessage,
            string actionName,
            string? subject = null)
        {
            try
            {
                await operation();
                _logger.LogInformation("{Action} for {Subject} succeeded.", actionName, subject);
                return Ok(new UserResponse(successMessage));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "{Action} failed: invalid argument for {Subject}.", actionName, subject);
                return BadRequest(new UserResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "{Action} failed: conflict for {Subject}.", actionName, subject);
                return Conflict(new UserResponse(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "{Action} failed: not found {Subject}.", actionName, subject);
                return NotFound(new UserResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Action} failed unexpectedly for {Subject}.", actionName, subject);
                return StatusCode(500, new UserResponse($"Erro ao {actionName.ToLower()} usuário: {ex.Message}"));
            }
        }

        [HttpPost("register")]
        public Task<IActionResult> Register([FromBody] RegisterDTO? dto)
        {
            if (dto is null)
                return Task.FromResult<IActionResult>(BadRequest(new UserResponse("Dados inválidos.")));

            return HandleAsync(
                () => _svc.CreateUserAsync(dto),
                "Usuário registrado com sucesso!",
                nameof(Register),
                dto.Name);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO? dto)
        {
            if (dto is null)
                return BadRequest(new UserResponse("Dados inválidos."));

            try
            {
                var token = await _svc.LoginAsync(dto);
                _logger.LogInformation("Login successful for {Email}.", dto.Email);
                return Ok(new { token = token });
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                var code = ex is UnauthorizedAccessException ? 401 : 400;
                _logger.LogWarning(ex, "Login failed for {Email}.", dto.Email);
                return StatusCode(code, new UserResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}.", dto.Email);
                return StatusCode(500, new UserResponse($"Erro ao realizar login: {ex.Message}"));
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var hdr) ||
                string.IsNullOrWhiteSpace(hdr) ||
                !hdr.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new UserResponse("Token inválido."));
            }

            var token = hdr.ToString()["Bearer ".Length..].Trim();
            _svc.Logout(token);
            _logger.LogInformation("Logout succeeded for token.");
            return Ok(new UserResponse("Logout realizado com sucesso!"));
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _svc.GetUsersAsync();
            if (users == null || !users.Any())
                return BadRequest(new UserResponse("Nenhum usuário encontrado."));

            return Ok(new
            {
                Message = "Usuários encontrados com sucesso!",
                Users = users
            });
        }

        [HttpPut("{name}")]
        public Task<IActionResult> UpdateUser(string name, [FromBody] RegisterDTO? dto)
        {
            if (dto is null)
                return Task.FromResult<IActionResult>(BadRequest(new UserResponse("Dados inválidos.")));

            return HandleAsync(
                () => _svc.UpdateUserAsync(name, dto),
                "Usuário atualizado com sucesso.",
                nameof(UpdateUser),
                name);
        }

        [HttpDelete("{name}")]
        public Task<IActionResult> DeleteUser(string name)
            => HandleAsync(
                () => _svc.DeleteUserAsync(name),
                "Usuário deletado com sucesso.",
                nameof(DeleteUser),
                name);

        [HttpGet("active-tokens")]
        public IActionResult GetActiveTokens()
            => Ok(new UserResponse($"Tokens ativos: {_svc.ActiveTokens.Count()}"));
    }
}
