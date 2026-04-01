using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.src.domain.Enums;
using Microsoft.Data.SqlClient;

namespace ChatModule.Repositories
{
    public class UserRepository
    {
        private readonly DatabaseManager _db;

        public UserRepository(DatabaseManager db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 1 * FROM Users WHERE Id = @id";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 1 * FROM Users WHERE Username = @username";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@username", username);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 1 * FROM Users WHERE Email = @email";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetGuid("id"),
                Username = reader.GetString("username"),
                Email = reader.GetString("email"),
                PasswordHash = reader.GetString("password_hash"),
                AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString("avatar_url"),
                Bio = reader.IsDBNull(reader.GetOrdinal("bio")) ? null : reader.GetString("bio"),
                Status = (UserStatus)reader.GetInt32("status"),
                Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? null : reader.GetDateTime("birthday"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
            };
        }
    }
}
