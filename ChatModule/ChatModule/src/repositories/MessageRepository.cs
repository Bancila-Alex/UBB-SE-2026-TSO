using ChatModule.Models;
using ChatModule.src.domain.Enums;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<List<Message>> GetReactionsForMessageAsync(Guid parentMessageId)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId
FROM Messages
WHERE MessageType = @ReactionType AND ParentMessageId = @ParentId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);
            command.Parameters.AddWithValue("@ParentId", parentMessageId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var message = new Message
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    ConversationId = reader.GetGuid(reader.GetOrdinal("ConversationId")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetGuid(reader.GetOrdinal("UserId")),
                    Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    ReplyToId = reader.IsDBNull(reader.GetOrdinal("ReplyToId")) ? null : reader.GetGuid(reader.GetOrdinal("ReplyToId")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                    MessageType = (MessageType)reader.GetInt32(reader.GetOrdinal("MessageType")),
                    ParentMessageId = reader.IsDBNull(reader.GetOrdinal("ParentMessageId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentMessageId"))
                };
                result.Add(message);
            }

            return result;
        }

        public async Task<List<Message>> GetSystemMessagesAsync(Guid conversationId)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId
FROM Messages
WHERE ConversationId = @ConversationId AND MessageType = @SystemType;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@SystemType", (int)MessageType.System);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var message = new Message
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    ConversationId = reader.GetGuid(reader.GetOrdinal("ConversationId")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetGuid(reader.GetOrdinal("UserId")),
                    Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    ReplyToId = reader.IsDBNull(reader.GetOrdinal("ReplyToId")) ? null : reader.GetGuid(reader.GetOrdinal("ReplyToId")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                    MessageType = (MessageType)reader.GetInt32(reader.GetOrdinal("MessageType")),
                    ParentMessageId = reader.IsDBNull(reader.GetOrdinal("ParentMessageId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentMessageId"))
                };
                result.Add(message);
            }

            return result;
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
