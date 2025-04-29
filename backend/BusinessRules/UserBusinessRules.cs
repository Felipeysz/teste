// backend/BusinessRules/UserBusinessRules.cs
using System;
using System.Linq;
using System.Text.RegularExpressions;
using backend.DTO;

namespace backend.BusinessRules
{
    public class UserBusinessRules
    {
        // Valida formato de nome (apenas letras e espaços)
        public void ValidateNameFormat(string name)
        {
            if (string.IsNullOrWhiteSpace(name) 
             || !Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s]+$"))
            {
                throw new ArgumentException("Nome inválido. O nome deve conter apenas letras e espaços.");
            }
        }

        // Valida formato de e-mail
        public void ValidateEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email) 
             || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("Formato de e-mail inválido.");
            }
        }

        // Campos obrigatórios no registro
        public void ValidateRegisterRequired(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)
             || string.IsNullOrWhiteSpace(dto.Email)
             || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Name, Email e Password são obrigatórios.");
            }
        }

        // Campos obrigatórios no login
        public void ValidateLoginRequired(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Senha é obrigatória.");
        }

        // Campos obrigatórios na atualização
        public void ValidateUpdateRequired(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)
             || string.IsNullOrWhiteSpace(dto.Email)
             || string.IsNullOrWhiteSpace(dto.Password)
             || string.IsNullOrWhiteSpace(dto.PermissionAccount))
            {
                throw new ArgumentException("Name, Email, Password e PermissionAccount são obrigatórios.");
            }
        }

        // Regras de complexidade da senha
        public void ValidatePasswordComplexity(string password)
        {
            if (password == null 
             || password.Length < 8 
             || !password.Any(char.IsUpper) 
             || !password.Any(char.IsDigit))
            {
                throw new ArgumentException("A senha deve ter pelo menos 8 caracteres, incluir uma letra maiúscula e um número.");
            }
        }

        // Verifica se a senha informada bate com o hash armazenado
        public void ValidatePassword(string storedHash, string providedPassword)
        {
            if (!BCrypt.Net.BCrypt.Verify(providedPassword, storedHash))
            {
                throw new UnauthorizedAccessException("Senha inválida.");
            }
        }
    }
}
