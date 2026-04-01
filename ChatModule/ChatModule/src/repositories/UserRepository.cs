using System;
using System.Collections.Generic;
using System.Data;
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
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 1 * FROM Users WHERE Id = @Id";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

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

            const string sql = "SELECT TOP 1 * FROM Users WHERE Username = @Username";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

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

            const string sql = "SELECT TOP 1 * FROM Users WHERE Email = @Email";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }

        public async Task<List<User>> GetAllAsync()
        {
            var users = new List<User>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM Users";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task<List<User>> SearchByUsernameAsync(string query)
        {
            var users = new List<User>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM Users WHERE Username LIKE @Query";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Query", $"%{query}%");

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task CreateAsync(User user)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
INSERT INTO Users
    (Id, Username, Email, PasswordHash, AvatarUrl, Bio, Status, Birthday, Phone)
VALUES
    (@Id, @Username, @Email, @PasswordHash, @AvatarUrl, @Bio, @Status, @Birthday, @Phone);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bio", (object?)user.Bio ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)user.Status);
            command.Parameters.AddWithValue("@Birthday", (object?)user.Birthday ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(User user)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE Users
SET Username = @Username,
    Email = @Email,
    AvatarUrl = @AvatarUrl,
    Bio = @Bio,
    Status = @Status,
    Phone = @Phone,
    Birthday = @Birthday
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bio", (object?)user.Bio ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)user.Status);
            command.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Birthday", (object?)user.Birthday ?? DBNull.Value);
            command.Parameters.AddWithValue("@Id", user.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdatePasswordAsync(Guid id, string passwordHash)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE Users
SET PasswordHash = @PasswordHash
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(User user)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
DELETE FROM Users
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", user.Id);

            await command.ExecuteNonQueryAsync();
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetGuid("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString("AvatarUrl"),
                Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? null : reader.GetString("Bio"),
                Status = (UserStatus)reader.GetInt32("Status"),
                Birthday = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? null : reader.GetDateTime("Birthday"),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString("Phone"),
            };
        }
    }
}
