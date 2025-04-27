// backend/Controllers/UserController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using backend.DTO;
using backend.Services;
using backend.BusinessRules;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserBusinessRules _rules;

        public UserController(
            UserService userService,
            UserBusinessRules rules)
        {
            _userService = userService;
            _rules       = rules;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            if (dto == null)
                return BadRequest("Dados inválidos.");

            try
            {
                await _userService.CreateUserAsync(dto);
                return Ok(new { message = "Usuário registrado com sucesso!" });
            }
            catch (ArgumentException ex)        { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message);  }
            catch (Exception ex)                 { return StatusCode(500, $"Erro ao registrar usuário: {ex.Message}"); }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (dto == null)
                return BadRequest("Dados inválidos.");

            try
            {
                var token = await _userService.LoginAsync(dto);
                return Ok(new { token });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (ArgumentException ex)           { return BadRequest(ex.Message); }
            catch (Exception ex)                    { return StatusCode(500, $"Erro ao realizar login: {ex.Message}"); }
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Token inválido.");

            _userService.Logout(dto.Token);
            return Ok(new { message = "Logout realizado com sucesso!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            var userDtos = users.Select(u => new {
                u.Id,
                u.Name,
                u.Email,
                u.PermissionAccount
            });
            return Ok(userDtos);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] RegisterDTO dto)
        {
            if (dto == null)
                return BadRequest("Dados inválidos.");

            try
            {
                await _userService.UpdateUserAsync(id, dto);
                return Ok(new { message = "Usuário atualizado com sucesso." });
            }
            catch (ArgumentException ex)        { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message);  }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message);  }
            catch (Exception ex)                 { return StatusCode(500, $"Erro ao atualizar usuário: {ex.Message}"); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { message = "Usuário deletado com sucesso." });
            }
            catch (ArgumentException ex)   { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex)            { return StatusCode(500, $"Erro ao deletar usuário: {ex.Message}"); }
        }

        [HttpGet("active-tokens")]
        public IActionResult GetActiveTokens()
            => Ok(new { tokens = _userService.ActiveTokens });
    }
}
