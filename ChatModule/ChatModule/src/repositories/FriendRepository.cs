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
            SELECT Id, UserId1, UserId2, Status, IsMatch, CreatedAt
            FROM Friends
            WHERE (UserId1 = @U1 AND UserId2 = @U2)
               OR (UserId1 = @U2 AND UserId2 = @U1);";

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
            SELECT Id, UserId1, UserId2, Status, IsMatch, CreatedAt
            FROM Friends
            WHERE UserId1 = @Id OR UserId2 = @Id;";

            var result = new List<Friend>();

            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapFriend(reader));
            }

            return result;
        }

        public async Task<List<Friend>> GetPendingRequestsForUserAsync(Guid userId)
        {
            const string sql = @"
            SELECT Id, UserId1, UserId2, Status, IsMatch, CreatedAt
            FROM Friends
            WHERE UserId2 = @Id AND Status = @PendingStatus;";
            var result = new List<Friend>();
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@PendingStatus", (byte)FriendStatus.Pending);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapFriend(reader));
            }
            return result;
        }

        public async Task<List<Friend>> GetAcceptedFriendsAsync(Guid userId)
        {
            const string sql = @"
            SELECT Id, UserId1, UserId2, Status, IsMatch, CreatedAt
            FROM Friends
            WHERE (UserId1 = @Id OR UserId2 = @Id) AND Status = @AcceptedStatus;";
            var result = new List<Friend>();
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@AcceptedStatus", (byte)FriendStatus.Accepted);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapFriend(reader));
            }
            return result;
        }

        public async Task<List<Guid>> GetMutualFriendIdsAsync(Guid userId1, Guid userId2)
        {
            const string sql = @"
            SELECT UserId1, UserId2
            FROM Friends
            WHERE (UserId1 = @U1 AND UserId2 IN (
                    SELECT CASE WHEN UserId1 = @U2 THEN UserId2 ELSE UserId1 END
                    FROM Friends
                    WHERE (UserId1 = @U2 OR UserId2 = @U2) AND Status = @AcceptedStatus
                ))
               OR (UserId2 = @U1 AND UserId1 IN (
                    SELECT CASE WHEN UserId1 = @U2 THEN UserId2 ELSE UserId1 END
                    FROM Friends
                    WHERE (UserId1 = @U2 OR UserId2 = @U2) AND Status = @AcceptedStatus
                )) AND Status = @AcceptedStatus;";
            var mutualFriendIds = new List<Guid>();
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);
            command.Parameters.AddWithValue("@AcceptedStatus", (byte)FriendStatus.Accepted);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var friendUserId1 = (Guid)reader["UserId1"];
                var friendUserId2 = (Guid)reader["UserId2"];
                var mutualFriendId = friendUserId1 == userId1 ? friendUserId2 : friendUserId1;
                mutualFriendIds.Add(mutualFriendId);
            }
            return mutualFriendIds;
        }

        public async Task<bool> IsFriendAsync(Guid userId1, Guid userId2)
        {
            const string sql = @"
            SELECT COUNT(*) 
            FROM Friends
            WHERE ((UserId1 = @U1 AND UserId2 = @U2) OR (UserId1 = @U2 AND UserId2 = @U1)) 
              AND Status = @AcceptedStatus;";
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);
            command.Parameters.AddWithValue("@AcceptedStatus", (byte)FriendStatus.Accepted);
            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async void CreateAsync(Friend friend)
        {
            const string sql = @"
            INSERT INTO Friends (Id, UserId1, UserId2, Status, IsMatch, CreatedAt)
            VALUES (@Id, @UserId1, @UserId2, @Status, @IsMatch, @CreatedAt);";
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", friend.Id);
            command.Parameters.AddWithValue("@UserId1", friend.UserId1);
            command.Parameters.AddWithValue("@UserId2", friend.UserId2);
            command.Parameters.AddWithValue("@Status", (byte)friend.Status);
            command.Parameters.AddWithValue("@IsMatch", friend.IsMatch);
            command.Parameters.AddWithValue("@CreatedAt", friend.CreatedAt);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateStatusAsync(Guid userId1, Guid userId2, FriendStatus status)
        {
            const string sql = @"
            UPDATE Friends
            SET Status = @Status
            WHERE (UserId1 = @U1 AND UserId2 = @U2) OR (UserId1 = @U2 AND UserId2 = @U1);";
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);
            command.Parameters.AddWithValue("@Status", (byte)status);
            await command.ExecuteNonQueryAsync();
        }

        public async void SetMatchAsync(Guid userId1, Guid userId2, bool isMatch)
        {
            const string sql = @"
            UPDATE Friends
            SET IsMatch = @IsMatch
            WHERE (UserId1 = @U1 AND UserId2 = @U2) OR (UserId1 = @U2 AND UserId2 = @U1);";
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);
            command.Parameters.AddWithValue("@IsMatch", isMatch);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(Guid userId1, Guid userId2)
        {
            const string sql = @"
            DELETE FROM Friends
            WHERE (UserId1 = @U1 AND UserId2 = @U2) OR (UserId1 = @U2 AND UserId2 = @U1);";
            await using var connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@U1", userId1);
            command.Parameters.AddWithValue("@U2", userId2);
            await command.ExecuteNonQueryAsync();
        }

        private static Friend MapFriend(SqlDataReader reader)
        {
            // Using standard ADO.NET indexing. 
            // Note that Status is cast to a byte first, because TINYINT = byte
            return new Friend
            {
                Id = (Guid)reader["Id"],
                UserId1 = (Guid)reader["UserId1"],
                UserId2 = (Guid)reader["UserId2"],
                Status = (FriendStatus)(byte)reader["Status"],
                IsMatch = (bool)reader["IsMatch"],
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}
