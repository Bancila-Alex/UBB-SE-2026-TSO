USE ChatModule;
GO

SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRAN;

    UPDATE Conversations
    SET PinnedMessageId = NULL
    WHERE PinnedMessageId IS NOT NULL;

    UPDATE Participants
    SET LastReadMessageId = NULL
    WHERE LastReadMessageId IS NOT NULL;

    DELETE FROM Participants;
    DELETE FROM Messages;
    DELETE FROM Conversations;
    DELETE FROM Friends;
    DELETE FROM Users;

    DECLARE @U1 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
    DECLARE @U2 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
    DECLARE @U3 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
    DECLARE @U4 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';

    INSERT INTO Users (Id, Username, Email, PasswordHash, AvatarUrl, Bio, Status, Birthday, Phone)
    VALUES
    (@U1, 'alice_dev', 'alice@example.com', '$2b$10$hash01', 'https://i.pravatar.cc/150?u=1', 'Backend + infra', 0, '1995-04-12', '+40712345001'),
    (@U2, 'bob_builder', 'bob@example.com', '$2b$10$hash02', 'https://i.pravatar.cc/150?u=2', 'Frontend + UX', 0, '1993-07-22', '+40712345002'),
    (@U3, 'carol_ux', 'carol@example.com', '$2b$10$hash03', 'https://i.pravatar.cc/150?u=3', 'Design systems', 2, '1997-01-30', NULL),
    (@U4, 'dan_ops', 'dan@example.com', '$2b$10$hash04', NULL, 'DevOps + release', 1, '1990-11-05', NULL);

    INSERT INTO Friends (Id, UserId1, UserId2, Status, IsMatch, CreatedAt)
    VALUES
    (NEWID(), @U1, @U2, 1, 0, GETUTCDATE()),
    (NEWID(), @U1, @U3, 1, 0, GETUTCDATE()),
    (NEWID(), @U4, @U1, 0, 0, GETUTCDATE());

    DECLARE @DM12 UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
    DECLARE @GRP UNIQUEIDENTIFIER = '22222222-0000-0000-0000-00000000000A';

    INSERT INTO Conversations (Id, Type, Title, IconUrl, CreatedBy, PinnedMessageId, CreatedAt)
    VALUES
    (@DM12, 0, NULL, NULL, @U1, NULL, GETUTCDATE()),
    (@GRP, 1, 'Dev Team', NULL, @U1, NULL, GETUTCDATE());

    DECLARE @M1 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
    DECLARE @M2 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000002';
    DECLARE @M3 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';
    DECLARE @M4 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000004';
    DECLARE @R1 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000010';

    INSERT INTO Messages (Id, ConversationId, UserId, Content, AttachmentUrl, SharedPostId, CreatedAt, ReplyToId, IsEdited, IsDeleted, LinkPreviewTitle, LinkPreviewDesc, MessageType, ParentMessageId, PinExpiresAt)
    VALUES
    (@M1, @DM12, @U1, 'Hey Bob, can you check this?', NULL, NULL, DATEADD(MINUTE, -40, GETUTCDATE()), NULL, 0, 0, NULL, NULL, 0, NULL, NULL),
    (@M2, @DM12, @U2, 'Sure, I am on it.', NULL, NULL, DATEADD(MINUTE, -35, GETUTCDATE()), @M1, 0, 0, NULL, NULL, 0, NULL, NULL),
    (@M3, @GRP, @U1, 'Standup starts in 10 minutes.', NULL, NULL, DATEADD(MINUTE, -20, GETUTCDATE()), NULL, 0, 0, NULL, NULL, 0, NULL, NULL),
    (@M4, @GRP, @U3, 'Got it!', NULL, NULL, DATEADD(MINUTE, -15, GETUTCDATE()), @M3, 0, 0, NULL, NULL, 0, NULL, NULL),
    (@R1, @GRP, @U2, N'👍', NULL, NULL, DATEADD(MINUTE, -14, GETUTCDATE()), NULL, 0, 0, NULL, NULL, 1, @M3, NULL);

    UPDATE Conversations SET PinnedMessageId = @M3 WHERE Id = @GRP;

    INSERT INTO Participants (Id, ConversationId, UserId, JoinedAt, Role, LastReadMessageId, TimeoutUntil, IsFavourite, IsNew, Nickname)
    VALUES
    (NEWID(), @DM12, @U1, DATEADD(DAY, -10, GETUTCDATE()), 1, @M2, NULL, 1, 0, NULL),
    (NEWID(), @DM12, @U2, DATEADD(DAY, -10, GETUTCDATE()), 1, @M2, NULL, 0, 0, NULL),
    (NEWID(), @GRP, @U1, DATEADD(DAY, -8, GETUTCDATE()), 0, @M4, NULL, 0, 0, 'Alice'),
    (NEWID(), @GRP, @U2, DATEADD(DAY, -8, GETUTCDATE()), 1, @M4, NULL, 0, 0, 'Bob'),
    (NEWID(), @GRP, @U3, DATEADD(DAY, -8, GETUTCDATE()), 1, @M4, NULL, 0, 1, 'Carol');

    COMMIT TRAN;
    PRINT 'Database reset and seed complete.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
GO
