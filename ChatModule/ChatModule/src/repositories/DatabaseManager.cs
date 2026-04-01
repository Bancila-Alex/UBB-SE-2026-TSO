using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;
using Microsoft.Data.SqlClient;

namespace ChatModule.Repositories
{
   
    public class DatabaseManager
    {
        public string ConnectionString { get; }

        public DatabaseManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM users";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString(reader.GetOrdinal("avatar_url")),
                    Bio = reader.IsDBNull(reader.GetOrdinal("bio")) ? null : reader.GetString(reader.GetOrdinal("bio")),
                    Status = (UserStatus)reader.GetInt32(reader.GetOrdinal("status")),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? null : reader.GetDateTime(reader.GetOrdinal("birthday")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                });
            }

            return users;
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            var messages = new List<Message>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM messages";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    ConversationId = reader.GetGuid(reader.GetOrdinal("conversation_id")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetGuid(reader.GetOrdinal("user_id")),
                    Content = reader.IsDBNull(reader.GetOrdinal("content")) ? null : reader.GetString(reader.GetOrdinal("content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    ReplyToId = reader.IsDBNull(reader.GetOrdinal("reply_to_id")) ? null : reader.GetGuid(reader.GetOrdinal("reply_to_id")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("is_edited")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                    MessageType = (MessageType)reader.GetInt32(reader.GetOrdinal("message_type")),
                    ParentMessageId = reader.IsDBNull(reader.GetOrdinal("parent_message_id")) ? null : reader.GetGuid(reader.GetOrdinal("parent_message_id")),
                });
            }

            return messages;
        }

        public async Task<List<Conversation>> GetConversationsAsync()
        {
            var conversations = new List<Conversation>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM conversations";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                conversations.Add(new Conversation
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Type = (ConversationType)reader.GetInt32(reader.GetOrdinal("type")),
                    Title = reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString(reader.GetOrdinal("title")),
                    IconUrl = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString(reader.GetOrdinal("icon_url")),
                    CreatedBy = reader.GetGuid(reader.GetOrdinal("created_by")),
                    PinnedMessageId = reader.IsDBNull(reader.GetOrdinal("pinned_message_id")) ? null : reader.GetGuid(reader.GetOrdinal("pinned_message_id")),
                });
            }

            return conversations;
        }

        public async Task<List<Participant>> GetParticipantsAsync()
        {
            var participants = new List<Participant>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM participants";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                participants.Add(new Participant
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    ConversationId = reader.GetGuid(reader.GetOrdinal("conversation_id")),
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                    JoinedAt = reader.GetDateTime(reader.GetOrdinal("joined_at")),
                    Role = (ParticipantRole)reader.GetInt32(reader.GetOrdinal("role")),
                    LastReadMessageId = reader.IsDBNull(reader.GetOrdinal("last_read_message_id")) ? null : reader.GetGuid(reader.GetOrdinal("last_read_message_id")),
                    TimeoutUntil = reader.IsDBNull(reader.GetOrdinal("timeout_until")) ? null : reader.GetDateTime(reader.GetOrdinal("timeout_until")),
                    IsFavourite = reader.GetBoolean(reader.GetOrdinal("is_favourite")),
                });
            }

            return participants;
        }

        public async Task<List<Friend>> GetFriendsAsync()
        {
            var friends = new List<Friend>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM friends";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                friends.Add(new Friend
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    UserId1 = reader.GetGuid(reader.GetOrdinal("user_id_1")),
                    UserId2 = reader.GetGuid(reader.GetOrdinal("user_id_2")),
                    Status = (FriendStatus)reader.GetInt32(reader.GetOrdinal("status")),
                    IsMatch = reader.GetBoolean(reader.GetOrdinal("is_match")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                });
            }

            return friends;
        }
    }
}
