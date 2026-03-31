using System;
using ChatModule.src.domain.Enums;

namespace ChatModule.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserStatus Status { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Phone { get; set; }
    }
}
