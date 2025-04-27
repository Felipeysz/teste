using System;
using System.Linq;
using System.Text.RegularExpressions;
using backend.DTO;

namespace backend.BusinessRules
{
    public class UserBusinessRules
    {
        public UserBusinessRules() { }

        public void ValidateEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Formato de e-mail inválido.");
        }

        public void ValidateRegisterRequired(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Name, Email e Password são obrigatórios.");
        }

        public void ValidateLoginRequired(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Senha é obrigatória.");
        }

        public void ValidateUpdateRequired(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.PermissionAccount))
            {
                throw new ArgumentException("Name, Email e PermissionAccount são obrigatórios.");
            }
        }

        public void ValidatePasswordComplexity(string password)
        {
            if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
                throw new ArgumentException("A senha deve ter pelo menos 8 caracteres, incluir uma letra maiúscula e um número.");
        }

        // Valida se a senha fornecida corresponde ao hash armazenado
        public void ValidatePassword(string storedHash, string providedPassword)
        {
            if (!BCrypt.Net.BCrypt.Verify(providedPassword, storedHash))
                throw new UnauthorizedAccessException("Senha inválida.");
        }
    }
}
