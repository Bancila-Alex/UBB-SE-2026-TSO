using System;
using System.Threading.Tasks;
using ChatModule.Models;
using Microsoft.Data.SqlClient;

namespace ChatModule.Repositories
{
    public class MessageRepository
    {
        private readonly DatabaseManager _db;

        public MessageRepository(DatabaseManager db)
        {
            _db = db;
        }

        public async Task CreateAsync(Message m)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
INSERT INTO Messages (Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId)
VALUES (@Id, @ConversationId, @UserId, @Content, @CreatedAt, @ReplyToId, @IsEdited, @IsDeleted, @MessageType, @ParentMessageId);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", m.Id);
            command.Parameters.AddWithValue("@ConversationId", m.ConversationId);
            command.Parameters.AddWithValue("@UserId", (object?)m.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Content", (object?)m.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", m.CreatedAt);
            command.Parameters.AddWithValue("@ReplyToId", (object?)m.ReplyToId ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsEdited", m.IsEdited);
            command.Parameters.AddWithValue("@IsDeleted", m.IsDeleted);
            command.Parameters.AddWithValue("@MessageType", (int)m.MessageType);
            command.Parameters.AddWithValue("@ParentMessageId", (object?)m.ParentMessageId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateContentAsync(Guid id, string newContent)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE Messages
SET Content = @Content
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Content", newContent);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task SetEditedAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE Messages
SET IsEdited = 1
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE Messages
SET IsDeleted = 1
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}
