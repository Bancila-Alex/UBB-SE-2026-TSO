using BCrypt.Net;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
                return null;

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
                Status = UserStatus.Offline
            };

            await _userRepository.CreateAsync(user);

            return user;
        }
    }
}
