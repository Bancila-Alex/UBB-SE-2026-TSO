using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.src.domain.Enums;
using Microsoft.Data.SqlClient;

namespace ChatModule.Repositories
{
    public class FriendRepository
    {
        private readonly DatabaseManager _db;

        public FriendRepository(DatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Friend?> GetAsync(Guid userId1, Guid userId2)
        {
            const string sql = @"
SELECT id, user_id_1, user_id_2, status, is_match, created_at
FROM friends
WHERE (user_id_1 = @U1 AND user_id_2 = @U2)
   OR (user_id_1 = @U2 AND user_id_2 = @U1);";

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapFriend(reader);
        }

        public async Task<List<Friend>> GetAllForUserAsync(Guid userId)
        {
            const string sql = @"
SELECT id, user_id_1, user_id_2, status, is_match, created_at
FROM friends
WHERE user_id_1 = @id OR user_id_2 = @id;";

            var result = new List<Friend>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapFriend(reader));
            }

            return result;
        }

        private static Friend MapFriend(SqlDataReader reader)
        {
            return new Friend
            {
                Id = reader.GetGuid("id"),
                UserId1 = reader.GetGuid("user_id_1"),
                UserId2 = reader.GetGuid("user_id_2"),
                Status = (FriendStatus)reader.GetInt32("status"),
                IsMatch = reader.GetBoolean("is_match"),
                CreatedAt = reader.GetDateTime("created_at")
            };
        }
    }
}
