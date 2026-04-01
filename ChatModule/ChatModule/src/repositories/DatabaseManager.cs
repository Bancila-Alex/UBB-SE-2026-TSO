using System;
using System.Collections.Generic;
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
                    Id = reader.GetGuid("id"),
                    Username = reader.GetString("username"),
                    Email = reader.GetString("email"),
                    PasswordHash = reader.GetString("password_hash"),
                    AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString("avatar_url"),
                    Bio = reader.IsDBNull(reader.GetOrdinal("bio")) ? null : reader.GetString("bio"),
                    Status = (UserStatus)reader.GetInt32("status"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? null : reader.GetDateTime("birthday"),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
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
                    Id = reader.GetGuid("id"),
                    ConversationId = reader.GetGuid("conversation_id"),
                    UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetGuid("user_id"),
                    Content = reader.IsDBNull(reader.GetOrdinal("content")) ? null : reader.GetString("content"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    ReplyToId = reader.IsDBNull(reader.GetOrdinal("reply_to_id")) ? null : reader.GetGuid("reply_to_id"),
                    IsEdited = reader.GetBoolean("is_edited"),
                    IsDeleted = reader.GetBoolean("is_deleted"),
                    MessageType = (MessageType)reader.GetInt32("message_type"),
                    ParentMessageId = reader.IsDBNull(reader.GetOrdinal("parent_message_id")) ? null : reader.GetGuid("parent_message_id"),
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
                    Id = reader.GetGuid("id"),
                    Type = (ConversationType)reader.GetInt32("type"),
                    Title = reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString("title"),
                    IconUrl = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString("icon_url"),
                    CreatedBy = reader.GetGuid("created_by"),
                    PinnedMessageId = reader.IsDBNull(reader.GetOrdinal("pinned_message_id")) ? null : reader.GetGuid("pinned_message_id"),
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
                    Id = reader.GetGuid("id"),
                    ConversationId = reader.GetGuid("conversation_id"),
                    UserId = reader.GetGuid("user_id"),
                    JoinedAt = reader.GetDateTime("joined_at"),
                    Role = (ParticipantRole)reader.GetInt32("role"),
                    LastReadMessageId = reader.IsDBNull(reader.GetOrdinal("last_read_message_id")) ? null : reader.GetGuid("last_read_message_id"),
                    TimeoutUntil = reader.IsDBNull(reader.GetOrdinal("timeout_until")) ? null : reader.GetDateTime("timeout_until"),
                    IsFavourite = reader.GetBoolean("is_favourite"),
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
                    Id = reader.GetGuid("id"),
                    UserId1 = reader.GetGuid("user_id_1"),
                    UserId2 = reader.GetGuid("user_id_2"),
                    Status = (FriendStatus)reader.GetInt32("status"),
                    IsMatch = reader.GetBoolean("is_match"),
                    CreatedAt = reader.GetDateTime("created_at"),
                });
            }

            return friends;
        }
    }
}
