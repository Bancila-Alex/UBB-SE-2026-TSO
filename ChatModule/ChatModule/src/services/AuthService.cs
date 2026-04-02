using BCrypt.Net;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatModule.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository) {  _userRepository = userRepository; }
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            if (user.Status == UserStatus.Online)
            {
                user.Status = UserStatus.Online;
                await _userRepository.UpdateAsync(user);
            }

            return user;
        }

        public async Task<User> RegisterAsync(
            string username,
            string email,
            string password,
            string phone,
            DateTime? birthday,
            string? avatarUrl)
        {
            if (await _userRepository.GetByUsernameAsync(username) != null)
                throw new InvalidOperationException("Username is already taken.");

            if (await _userRepository.GetByEmailAsync(email) != null)
                throw new InvalidOperationException("Email is already taken.");

            ValidateRegistrationInput(username, email, password, phone, birthday);

            var passwordHash = HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Phone = phone,
                Birthday = birthday,
                AvatarUrl = avatarUrl,
                Status = UserStatus.Online
            };

            await _userRepository.CreateAsync(user);

            return user;
        }

        public async Task ChangePasswordAsync(string email, string newPassword)
        {
            if (!IsValidEmail(email))
            {
                throw new InvalidOperationException("Invalid email format.");
            }

            if (!IsValidPassword(newPassword))
            {
                throw new InvalidOperationException("Password must be 8-32 chars and include uppercase, number, and special character.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var passwordHash = HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(user.Id, passwordHash);
        }

        private static void ValidateRegistrationInput(string username, string email, string password, string phone, DateTime? birthday)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 5 || username.Length > 16)
            {
                throw new InvalidOperationException("Username must be between 5 and 16 characters.");
            }

            if (!IsValidPassword(password))
            {
                throw new InvalidOperationException("Password must be 8-32 chars and include uppercase, number, and special character.");
            }

            if (!IsValidEmail(email))
            {
                throw new InvalidOperationException("Invalid email format.");
            }

            if (!IsValidPhone(phone))
            {
                throw new InvalidOperationException("Phone must be 7-15 chars, digits with optional leading +.");
            }

            if (!birthday.HasValue || birthday.Value.Date >= DateTime.Today)
            {
                throw new InvalidOperationException("Birthday must be a past date.");
            }
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 32)
            {
                return false;
            }

            var hasUpper = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
            return hasUpper && hasDigit && hasSpecial;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Any(char.IsWhiteSpace))
            {
                return false;
            }

            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            return parts[1].Contains('.', StringComparison.Ordinal);
        }

        private static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            return Regex.IsMatch(phone, "^\\+?[0-9]{7,15}$");
        }
    }
}
