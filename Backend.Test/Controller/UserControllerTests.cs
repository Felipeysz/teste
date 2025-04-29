using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Controllers;
using backend.DTO;
using backend.Services;
using backend.BusinessRules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _svc;
        private readonly Mock<UserBusinessRules> _rules;
        private readonly Mock<ILogger<UserController>> _logger;
        private readonly UserController _ctrl;

        public UserControllerTests()
        {
            _svc    = new Mock<IUserService>(MockBehavior.Strict);
            _rules  = new Mock<UserBusinessRules>(MockBehavior.Loose);
            _logger = new Mock<ILogger<UserController>>(MockBehavior.Loose);

            _ctrl = new UserController(_svc.Object, _rules.Object, _logger.Object);
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task Register_NullDto_ReturnsBadRequest()
        {
            var result = await _ctrl.Register(null);
            var bad    = Assert.IsType<BadRequestObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(bad.Value);
            Assert.Equal("Dados inválidos.", resp.Message);
        }

        [Fact]
        public async Task Register_Success_ReturnsOk()
        {
            var dto = new RegisterDTO { Name = "Felipe", Email = "e@e.com", Password = "P1aAaaa!", PermissionAccount = "user" };
            _svc.Setup(x => x.CreateUserAsync(dto)).Returns(Task.CompletedTask);

            var result = await _ctrl.Register(dto);
            var ok     = Assert.IsType<OkObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(ok.Value);
            Assert.Equal("Usuário registrado com sucesso!", resp.Message);

            _svc.Verify(x => x.CreateUserAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Login_NullDto_ReturnsBadRequest()
        {
            var result = await _ctrl.Login(null);
            var bad    = Assert.IsType<BadRequestObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(bad.Value);
            Assert.Equal("Dados inválidos.", resp.Message);
        }

        [Fact]
        public async Task Login_Success_ReturnsOk()
        {
            var dto   = new LoginDTO { Email = "test@gmail.com", Password = "P1aAaaa!" };
            var token = "jwt.token.here";
            _svc.Setup(x => x.LoginAsync(dto)).ReturnsAsync(token);

            var result = await _ctrl.Login(dto);
            var ok     = Assert.IsType<OkObjectResult>(result);
            // use reflection to get anonymous type property
            var prop   = ok.Value.GetType().GetProperty("token");
            Assert.NotNull(prop);
            var actual = prop.GetValue(ok.Value) as string;
            Assert.Equal(token, actual);

            _svc.Verify(x => x.LoginAsync(dto), Times.Once);
        }

        [Fact]
        public void Logout_NoHeader_ReturnsBadRequest()
        {
            var result = _ctrl.Logout();
            var bad    = Assert.IsType<BadRequestObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(bad.Value);
            Assert.Equal("Token inválido.", resp.Message);
        }

        [Fact]
        public void Logout_WithBearerHeader_ReturnsOk()
        {
            var token = "abc.def.ghi";
            _ctrl.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";
            _svc.Setup(x => x.Logout(token));

            var result = _ctrl.Logout();
            var ok     = Assert.IsType<OkObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(ok.Value);
            Assert.Equal("Logout realizado com sucesso!", resp.Message);

            _svc.Verify(x => x.Logout(token), Times.Once);
        }

        [Fact]
        public async Task GetUsers_Success_ReturnsOk()
        {
            var users = new List<UserDTO>
            {
                new UserDTO("U1", "u1@x.com", "user"),
                new UserDTO("U2", "u2@x.com", "admin")
            };
            _svc.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);

            var result = await _ctrl.GetUsers();
            var ok     = Assert.IsType<OkObjectResult>(result);
            var value  = ok.Value;
            // reflection for Message
            var mt     = value.GetType().GetProperty("Message");
            Assert.NotNull(mt);
            var msg    = mt.GetValue(value) as string;
            Assert.Equal("Usuários encontrados com sucesso!", msg);
            // reflection for Users
            var ut     = value.GetType().GetProperty("Users");
            Assert.NotNull(ut);
            var list   = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(ut.GetValue(value));
            var arr    = list.ToList();
            Assert.Equal(2, arr.Count);
            Assert.Equal("U1", arr[0].Name);
        }

        [Fact]
        public async Task GetUsers_Empty_ReturnsBadRequest()
        {
            _svc.Setup(x => x.GetUsersAsync()).ReturnsAsync(new List<UserDTO>());

            var result = await _ctrl.GetUsers();
            var bad    = Assert.IsType<BadRequestObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(bad.Value);
            Assert.Equal("Nenhum usuário encontrado.", resp.Message);
        }

        [Fact]
        public async Task UpdateUser_NullDto_ReturnsBadRequest()
        {
            var result = await _ctrl.UpdateUser("UserX", null);
            var bad    = Assert.IsType<BadRequestObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(bad.Value);
            Assert.Equal("Dados inválidos.", resp.Message);
        }

        [Fact]
        public async Task UpdateUser_Success_ReturnsOk()
        {
            var dto = new RegisterDTO { Name = "UserX", Email = "x@x.com", Password = "P1aAaaa!", PermissionAccount = "user" };
            _svc.Setup(x => x.UpdateUserAsync("UserX", dto)).Returns(Task.CompletedTask);

            var result = await _ctrl.UpdateUser("UserX", dto);
            var ok     = Assert.IsType<OkObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(ok.Value);
            Assert.Equal("Usuário atualizado com sucesso.", resp.Message);

            _svc.Verify(x => x.UpdateUserAsync("UserX", dto), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_Success_ReturnsOk()
        {
            _svc.Setup(x => x.DeleteUserAsync("UserX")).Returns(Task.CompletedTask);

            var result = await _ctrl.DeleteUser("UserX");
            var ok     = Assert.IsType<OkObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(ok.Value);
            Assert.Equal("Usuário deletado com sucesso.", resp.Message);

            _svc.Verify(x => x.DeleteUserAsync("UserX"), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_NotFound_ReturnsNotFound()
        {
            _svc.Setup(x => x.DeleteUserAsync("UserX")).ThrowsAsync(new KeyNotFoundException("User not found"));

            var result = await _ctrl.DeleteUser("UserX");
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var resp     = Assert.IsType<UserResponse>(notFound.Value);
            Assert.Equal("User not found", resp.Message);
        }

        [Fact]
        public void GetActiveTokens_ReturnsOk()
        {
            _svc.Setup(x => x.ActiveTokens).Returns(new[] { "t1", "t2" });

            var result = _ctrl.GetActiveTokens();
            var ok     = Assert.IsType<OkObjectResult>(result);
            var resp   = Assert.IsType<UserResponse>(ok.Value);
            Assert.Equal("Tokens ativos: 2", resp.Message);
        }
    }
}
