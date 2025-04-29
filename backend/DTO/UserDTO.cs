namespace backend.DTO
{
    public record UserResponse(string Message);

    public record RegisterDTO(
        string Name,
        string Email,
        string Password,
        string PermissionAccount = "user"
    )
    {
        public RegisterDTO() : this(default!, default!, default!, "user") { }
    }

    public record LoginDTO(
        string Email,
        string Password
    )
    {
        public LoginDTO() : this(default!, default!) { }
    }

    public record UserDTO(
        string Name,
        string Email,
        string PermissionAccount
    );
}
