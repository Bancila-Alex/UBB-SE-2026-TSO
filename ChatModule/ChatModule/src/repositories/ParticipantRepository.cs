using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.src.domain.Enums;
using Microsoft.Data.SqlClient;

namespace ChatModule.Repositories
{
    public class ParticipantRepository
    {
        private readonly DatabaseManager _db;

        public ParticipantRepository(DatabaseManager db)
        {
            _db = db;
        }

        public async Task<Participant?> GetAsync(Guid conversationId, Guid userId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT TOP 1 id, conversation_id, user_id, joined_at, role, last_read_message_id, timeout_until, is_favourite
FROM participants
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapParticipant(reader);
        }

        public async Task<List<Participant>> GetAllForConversationAsync(Guid conversationId)
        {
            var participants = new List<Participant>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT id, conversation_id, user_id, joined_at, role, last_read_message_id, timeout_until, is_favourite
FROM participants
WHERE conversation_id = @ConversationId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                participants.Add(MapParticipant(reader));
            }

            return participants;
        }

        public async Task<List<Participant>> GetAllForUserAsync(Guid userId)
        {
            var participants = new List<Participant>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
SELECT id, conversation_id, user_id, joined_at, role, last_read_message_id, timeout_until, is_favourite
FROM participants
WHERE user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                participants.Add(MapParticipant(reader));
            }

            return participants;
        }

        public async Task CreateAsync(Participant participant)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
INSERT INTO participants
    (id, conversation_id, user_id, joined_at, role, last_read_message_id, timeout_until, is_favourite)
VALUES
    (@Id, @ConversationId, @UserId, @JoinedAt, @Role, @LastReadMessageId, @TimeoutUntil, @IsFavourite);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", participant.Id);
            command.Parameters.AddWithValue("@ConversationId", participant.ConversationId);
            command.Parameters.AddWithValue("@UserId", participant.UserId);
            command.Parameters.AddWithValue("@JoinedAt", participant.JoinedAt);
            command.Parameters.AddWithValue("@Role", (int)participant.Role);
            command.Parameters.AddWithValue("@LastReadMessageId", (object?)participant.LastReadMessageId ?? DBNull.Value);
            command.Parameters.AddWithValue("@TimeoutUntil", (object?)participant.TimeoutUntil ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsFavourite", participant.IsFavourite);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateRoleAsync(Guid conversationId, Guid userId, ParticipantRole role)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE participants
SET role = @Role
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Role", (int)role);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateLastReadAsync(Guid conversationId, Guid userId, Guid messageId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE participants
SET last_read_message_id = @MessageId
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@MessageId", messageId);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateTimeoutAsync(Guid conversationId, Guid userId, DateTime? until)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE participants
SET timeout_until = @Until
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Until", (object?)until ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateFavouriteAsync(Guid conversationId, Guid userId, bool isFav)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
UPDATE participants
SET is_favourite = @IsFav
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IsFav", isFav);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(Guid conversationId, Guid userId)
        {
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            const string sql = @"
DELETE FROM participants
WHERE conversation_id = @ConversationId AND user_id = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        private static Participant MapParticipant(SqlDataReader reader)
        {
            return new Participant
            {
                Id = reader.GetGuid("id"),
                ConversationId = reader.GetGuid("conversation_id"),
                UserId = reader.GetGuid("user_id"),
                JoinedAt = reader.GetDateTime("joined_at"),
                Role = (ParticipantRole)reader.GetInt32("role"),
                LastReadMessageId = reader.IsDBNull(reader.GetOrdinal("last_read_message_id")) ? null : reader.GetGuid("last_read_message_id"),
                TimeoutUntil = reader.IsDBNull(reader.GetOrdinal("timeout_until")) ? null : reader.GetDateTime("timeout_until"),
                IsFavourite = reader.GetBoolean("is_favourite"),
            };
        }
    }
}
