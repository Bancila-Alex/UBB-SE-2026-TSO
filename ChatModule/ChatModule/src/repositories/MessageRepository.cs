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

        public async Task<Message?> GetByIdAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT TOP 1 Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
FROM Messages
WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapMessage(reader);
        }

        public async Task<List<Message>> GetAllForConversationAsync(Guid conversationId)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
FROM Messages
WHERE ConversationId = @ConversationId
ORDER BY CreatedAt;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapMessage(reader));
            }

            return result;
        }

        public async Task<List<Message>> GetByConversationAsync(Guid conversationId, int skip, int take)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
FROM Messages
WHERE ConversationId = @ConversationId
  AND MessageType <> @ReactionType
ORDER BY CreatedAt ASC
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);
            command.Parameters.AddWithValue("@Skip", skip);
            command.Parameters.AddWithValue("@Take", take);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapMessage(reader));
            }

            return result;
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

        public async Task SetPinExpiresAtAsync(Guid id, DateTime? expiresAt)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
            UPDATE Messages
            SET PinExpiresAt = @ExpiresAt
            WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ExpiresAt", (object?)expiresAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Message>> GetReactionsForMessageAsync(Guid parentMessageId)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
            SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
            FROM Messages
            WHERE MessageType = @ReactionType AND ParentMessageId = @ParentId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);
            command.Parameters.AddWithValue("@ParentId", parentMessageId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapMessage(reader));
            }

            return result;
        }

        public async Task<List<Message>> SearchInConversationAsync(Guid conversationId, string query)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
FROM Messages
WHERE ConversationId = @ConversationId AND Content LIKE @Query
ORDER BY CreatedAt;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@Query", $"%{query}%");

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapMessage(reader));
            }

            return result;
        }

        public async Task<List<Message>> GetSystemMessagesAsync(Guid conversationId)
        {
            var result = new List<Message>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
            SELECT Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
            FROM Messages
            WHERE ConversationId = @ConversationId AND MessageType = @SystemType;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@SystemType", (int)MessageType.System);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapMessage(reader));
            }

            return result;
        }

        public async Task<Message?> GetLastMessageAsync(Guid conversationId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT TOP 1 Id, ConversationId, UserId, Content, CreatedAt, ReplyToId, IsEdited, IsDeleted, MessageType, ParentMessageId, PinExpiresAt
FROM Messages
WHERE ConversationId = @ConversationId
  AND MessageType <> @ReactionType
ORDER BY CreatedAt DESC;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapMessage(reader);
        }

        public async Task<int> CountUnreadAsync(Guid conversationId, Guid lastReadMessageId, Guid userId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT COUNT(*)
FROM Messages MessageToCount
INNER JOIN Messages LastReadMessage ON LastReadMessage.Id = @LastReadMessageId
WHERE MessageToCount.ConversationId = @ConversationId
  AND LastReadMessage.ConversationId = @ConversationId
  AND MessageToCount.CreatedAt > LastReadMessage.CreatedAt
  AND MessageToCount.MessageType <> @ReactionType
  AND (MessageToCount.UserId IS NULL OR MessageToCount.UserId <> @UserId);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);
            command.Parameters.AddWithValue("@LastReadMessageId", lastReadMessageId);
            command.Parameters.AddWithValue("@UserId", userId);

            var scalarResult = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalarResult);
        }

        public async Task<Guid?> GetLatestReadableMessageIdAsync(Guid conversationId, Guid userId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT TOP 1 Id
FROM Messages
WHERE ConversationId = @ConversationId
  AND MessageType <> @ReactionType
  AND (UserId IS NULL OR UserId <> @UserId)
ORDER BY CreatedAt DESC;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);

            var scalarResult = await command.ExecuteScalarAsync();
            if (scalarResult == null || scalarResult == DBNull.Value)
            {
                return null;
            }

            return (Guid)scalarResult;
        }

        public async Task<int> CountUnreadFromStartAsync(Guid conversationId, Guid userId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT COUNT(*)
FROM Messages
WHERE ConversationId = @ConversationId
  AND MessageType <> @ReactionType
  AND (UserId IS NULL OR UserId <> @UserId);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReactionType", (int)MessageType.Reaction);

            var scalarResult = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalarResult);
        }

        public async Task<int> CountReadByAsync(Guid ConversationId, Guid MessageId)
        {
            await using var Connection = new SqlConnection(_db.ConnectionString);
            await Connection.OpenAsync();

            const string sql = @"
SELECT COUNT(*)
FROM Participants Participant
INNER JOIN Messages LastReadMessage ON LastReadMessage.Id = Participant.LastReadMessageId
INNER JOIN Messages TargetMessage ON TargetMessage.Id = @MessageId
WHERE Participant.ConversationId = @ConversationId
  AND TargetMessage.ConversationId = @ConversationId
  AND LastReadMessage.ConversationId = @ConversationId
  AND LastReadMessage.CreatedAt >= TargetMessage.CreatedAt;";

            await using var Command = new SqlCommand(sql, Connection);
            Command.Parameters.AddWithValue("@ConversationId", ConversationId);
            Command.Parameters.AddWithValue("@MessageId", MessageId);

            var ScalarResult = await Command.ExecuteScalarAsync();
            return Convert.ToInt32(ScalarResult);
        }

        public async Task<List<Guid>> GetReadByUserIdsAsync(Guid ConversationId, Guid MessageId)
        {
            var Result = new List<Guid>();

            await using var Connection = new SqlConnection(_db.ConnectionString);
            await Connection.OpenAsync();

            const string sql = @"
SELECT Participant.UserId
FROM Participants Participant
INNER JOIN Messages LastReadMessage ON LastReadMessage.Id = Participant.LastReadMessageId
INNER JOIN Messages TargetMessage ON TargetMessage.Id = @MessageId
WHERE Participant.ConversationId = @ConversationId
  AND TargetMessage.ConversationId = @ConversationId
  AND LastReadMessage.ConversationId = @ConversationId
  AND LastReadMessage.CreatedAt >= TargetMessage.CreatedAt
ORDER BY Participant.UserId;";

            await using var Command = new SqlCommand(sql, Connection);
            Command.Parameters.AddWithValue("@ConversationId", ConversationId);
            Command.Parameters.AddWithValue("@MessageId", MessageId);

            await using var Reader = await Command.ExecuteReaderAsync();
            var UserIdOrdinal = Reader.GetOrdinal("UserId");

            while (await Reader.ReadAsync())
            {
                Result.Add(Reader.GetGuid(UserIdOrdinal));
            }

            return Result;
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

        public async Task UnsoftDeleteAsync(Guid id)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
            UPDATE Messages
            SET IsDeleted = 0
            WHERE Id = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteByConversationAsync(Guid conversationId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
            DELETE FROM Messages
            WHERE ConversationId = @ConversationId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);

            await command.ExecuteNonQueryAsync();
        }

        private static Message MapMessage(SqlDataReader reader)
        {
            var idOrdinal = reader.GetOrdinal("Id");
            var conversationIdOrdinal = reader.GetOrdinal("ConversationId");
            var userIdOrdinal = reader.GetOrdinal("UserId");
            var contentOrdinal = reader.GetOrdinal("Content");
            var createdAtOrdinal = reader.GetOrdinal("CreatedAt");
            var replyToIdOrdinal = reader.GetOrdinal("ReplyToId");
            var isEditedOrdinal = reader.GetOrdinal("IsEdited");
            var isDeletedOrdinal = reader.GetOrdinal("IsDeleted");
            var messageTypeOrdinal = reader.GetOrdinal("MessageType");
            var parentMessageIdOrdinal = reader.GetOrdinal("ParentMessageId");
            var pinExpiresAtOrdinal = reader.GetOrdinal("PinExpiresAt");

            return new Message
            {
                Id = reader.GetGuid(idOrdinal),
                ConversationId = reader.GetGuid(conversationIdOrdinal),
                UserId = reader.IsDBNull(userIdOrdinal) ? null : reader.GetGuid(userIdOrdinal),
                Content = reader.IsDBNull(contentOrdinal) ? null : reader.GetString(contentOrdinal),
                CreatedAt = reader.GetDateTime(createdAtOrdinal),
                ReplyToId = reader.IsDBNull(replyToIdOrdinal) ? null : reader.GetGuid(replyToIdOrdinal),
                IsEdited = reader.GetBoolean(isEditedOrdinal),
                IsDeleted = reader.GetBoolean(isDeletedOrdinal),
                MessageType = (MessageType)reader.GetByte(messageTypeOrdinal),
                ParentMessageId = reader.IsDBNull(parentMessageIdOrdinal) ? null : reader.GetGuid(parentMessageIdOrdinal),
                PinExpiresAt = reader.IsDBNull(pinExpiresAtOrdinal) ? null : reader.GetDateTime(pinExpiresAtOrdinal)
            };
        }
    }
}
