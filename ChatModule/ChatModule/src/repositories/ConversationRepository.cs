using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;
using MySqlConnector;

namespace ChatModule.Repositories
{
    public class ConversationRepository
    {
        private readonly DatabaseManager _db;

        public ConversationRepository(DatabaseManager db)
        {
            _db = db;
        }

        public async Task<Conversation?> GetByIdAsync(Guid id)
        {
            await using var connection = new MySqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM conversations WHERE id = @id LIMIT 1";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapConversation(reader);
        }

        public async Task<List<Conversation>> GetAllForUserAsync(Guid userId)
        {
            var conversations = new List<Conversation>();

            await using var connection = new MySqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT c.*
FROM conversations c
INNER JOIN participants p ON p.conversation_id = c.id
WHERE p.user_id = @userId;";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                conversations.Add(MapConversation(reader));
            }

            return conversations;
        }

        public async Task<Conversation?> GetDmBetweenAsync(Guid userId1, Guid userId2)
        {
            await using var connection = new MySqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT c.*
FROM conversations c
INNER JOIN participants p ON p.conversation_id = c.id
WHERE c.type = @dmType
  AND p.user_id IN (@userId1, @userId2)
GROUP BY c.id
HAVING COUNT(DISTINCT p.user_id) = 2
LIMIT 1;";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@dmType", (int)ConversationType.Dm);
            command.Parameters.AddWithValue("@userId1", userId1);
            command.Parameters.AddWithValue("@userId2", userId2);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapConversation(reader);
        }

        private static Conversation MapConversation(MySqlDataReader reader)
        {
            return new Conversation
            {
                Id = reader.GetGuid("id"),
                Type = (ConversationType)reader.GetInt32("type"),
                Title = reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString("title"),
                IconUrl = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString("icon_url"),
                CreatedBy = reader.GetGuid("created_by"),
                PinnedMessageId = reader.IsDBNull(reader.GetOrdinal("pinned_message_id")) ? null : reader.GetGuid("pinned_message_id"),
            };
        }
    }
}
