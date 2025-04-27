namespace backend.DTO
{
    public class RegisterDTO
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string PermissionAccount { get; set; } = "user";
    }

    public class LoginDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LogoutDTO
    {
        public required string Token { get; set; }
    }
}
