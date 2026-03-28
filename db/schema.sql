-- ============================================================
--  ChatModule Database Schema
--  Community Hub App -- Team 2 (TSO)
--  Run this script once in SSMS to set up your local database.
--  If the schema changes, only run the altered sections.
-- ============================================================

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ChatModule')
BEGIN
    CREATE DATABASE ChatModule;
END
GO

USE ChatModule;
GO

-- ============================================================
--  TABLE: Users
-- ============================================================
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
    Username        NVARCHAR(16)        NOT NULL,
    Email           NVARCHAR(255)       NOT NULL,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    AvatarUrl       NVARCHAR(500)       NULL,
    Bio             NVARCHAR(100)       NULL,
    Status          TINYINT             NOT NULL    DEFAULT 0,  -- 0=Online, 1=Offline, 2=Busy
    Birthday        DATE                NULL,
    Phone           NVARCHAR(16)        NULL,
    CreatedAt       DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Users             PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username    UNIQUE      (Username),
    CONSTRAINT UQ_Users_Email       UNIQUE      (Email),
    CONSTRAINT CK_Users_Username    CHECK       (LEN(Username) >= 5 AND LEN(Username) <= 16),
    CONSTRAINT CK_Users_Bio         CHECK       (LEN(Bio) <= 100)
);
GO

-- ============================================================
--  TABLE: Friends
--  One row per relationship between two users.
--  Status: 0=Pending, 1=Accepted, 2=Blocked
-- ============================================================
CREATE TABLE Friends (
    Id          UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
    UserId1     UNIQUEIDENTIFIER    NOT NULL,
    UserId2     UNIQUEIDENTIFIER    NOT NULL,
    Status      TINYINT             NOT NULL    DEFAULT 0,  -- 0=Pending, 1=Accepted, 2=Blocked
    IsMatch     BIT                 NOT NULL    DEFAULT 0,  -- Set to 1 by Team 4 matchmaking
    CreatedAt   DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Friends               PRIMARY KEY (Id),
    CONSTRAINT FK_Friends_User1         FOREIGN KEY (UserId1)   REFERENCES Users(Id),
    CONSTRAINT FK_Friends_User2         FOREIGN KEY (UserId2)   REFERENCES Users(Id),
    CONSTRAINT UQ_Friends_Pair          UNIQUE      (UserId1, UserId2),
    CONSTRAINT CK_Friends_NotSelf       CHECK       (UserId1 <> UserId2)
);
GO

-- ============================================================
--  TABLE: Conversations
--  Type: 0=DM, 1=Group
-- ============================================================
CREATE TABLE Conversations (
    Id              UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
    Type            TINYINT             NOT NULL,           -- 0=Dm, 1=Group
    Title           NVARCHAR(100)       NULL,               -- NULL for DMs, required for Groups
    IconUrl         NVARCHAR(500)       NULL,               -- NULL for DMs
    CreatedBy       UNIQUEIDENTIFIER    NOT NULL,
    PinnedMessageId UNIQUEIDENTIFIER    NULL,               -- FK added after Messages table
    CreatedAt       DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Conversations         PRIMARY KEY (Id),
    CONSTRAINT FK_Conversations_Creator FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);
GO

-- ============================================================
--  TABLE: Participants
--  One row per user per conversation.
--  Role: 0=Admin, 1=Member, 2=Banned
-- ============================================================
CREATE TABLE Participants (
    Id                  UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
    ConversationId      UNIQUEIDENTIFIER    NOT NULL,
    UserId              UNIQUEIDENTIFIER    NOT NULL,
    JoinedAt            DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
    Role                TINYINT             NOT NULL    DEFAULT 1,  -- 0=Admin, 1=Member, 2=Banned
    LastReadMessageId   UNIQUEIDENTIFIER    NULL,
    TimeoutUntil        DATETIME2           NULL,               -- NULL means no timeout active
    IsFavourite         BIT                 NOT NULL    DEFAULT 0,
    IsNew               BIT                 NOT NULL    DEFAULT 1,  -- colored border on new DMs
    Nickname            NVARCHAR(16)        NULL,               -- custom group nickname

    CONSTRAINT PK_Participants                  PRIMARY KEY (Id),
    CONSTRAINT FK_Participants_Conversation     FOREIGN KEY (ConversationId)    REFERENCES Conversations(Id),
    CONSTRAINT FK_Participants_User             FOREIGN KEY (UserId)            REFERENCES Users(Id),
    CONSTRAINT UQ_Participants_Pair             UNIQUE      (ConversationId, UserId),
    CONSTRAINT CK_Participants_Nickname         CHECK       (LEN(Nickname) <= 16)
);
GO

-- ============================================================
--  TABLE: Messages
--  MessageType: 0=Text, 1=Reaction, 2=System
--  sender_id NULL = system message (group created, pinned, etc.)
-- ============================================================
CREATE TABLE Messages (
    Id              UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
    ConversationId  UNIQUEIDENTIFIER    NOT NULL,
    UserId          UNIQUEIDENTIFIER    NULL,               -- NULL for system messages
    Content         NVARCHAR(1024)      NULL,
    AttachmentUrl   NVARCHAR(500)       NULL,               -- local file path for images
    SharedPostId    UNIQUEIDENTIFIER    NULL,               -- Team 1 post or Team 3 event
    CreatedAt       DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
    ReplyToId       UNIQUEIDENTIFIER    NULL,               -- references another Message
    IsEdited        BIT                 NOT NULL    DEFAULT 0,
    IsDeleted       BIT                 NOT NULL    DEFAULT 0,
    LinkPreviewTitle    NVARCHAR(255)   NULL,
    LinkPreviewDesc     NVARCHAR(500)   NULL,
    MessageType     TINYINT             NOT NULL    DEFAULT 0,  -- 0=Text, 1=Reaction, 2=System
    ParentMessageId UNIQUEIDENTIFIER    NULL,               -- for reactions: the target message
    PinExpiresAt    DATETIME2           NULL,               -- when the pin expires (NULL = not pinned)

    CONSTRAINT PK_Messages                  PRIMARY KEY (Id),
    CONSTRAINT FK_Messages_Conversation     FOREIGN KEY (ConversationId)    REFERENCES Conversations(Id),
    CONSTRAINT FK_Messages_User             FOREIGN KEY (UserId)            REFERENCES Users(Id),
    CONSTRAINT FK_Messages_ReplyTo          FOREIGN KEY (ReplyToId)         REFERENCES Messages(Id),
    CONSTRAINT FK_Messages_Parent           FOREIGN KEY (ParentMessageId)   REFERENCES Messages(Id),
    CONSTRAINT CK_Messages_Content         CHECK       (LEN(Content) <= 1024)
);
GO

-- ============================================================
--  Now that Messages exists, add the FK for PinnedMessageId
--  on Conversations (circular reference handled with ALTER)
-- ============================================================
ALTER TABLE Conversations
    ADD CONSTRAINT FK_Conversations_PinnedMessage
    FOREIGN KEY (PinnedMessageId) REFERENCES Messages(Id);
GO

-- ============================================================
--  Now that Messages exists, add the FK for LastReadMessageId
--  on Participants
-- ============================================================
ALTER TABLE Participants
    ADD CONSTRAINT FK_Participants_LastRead
    FOREIGN KEY (LastReadMessageId) REFERENCES Messages(Id);
GO

-- ============================================================
--  INDEXES
--  Speed up the most common queries in the app.
-- ============================================================

-- Look up messages in a conversation sorted by time (main chat view)
CREATE INDEX IX_Messages_ConversationId_CreatedAt
    ON Messages (ConversationId, CreatedAt);

-- Look up all conversations a user is part of (sidebar list)
CREATE INDEX IX_Participants_UserId
    ON Participants (UserId);

-- Look up all participants in a conversation (member panel)
CREATE INDEX IX_Participants_ConversationId
    ON Participants (ConversationId);

-- Look up the friendship/relationship between two users
CREATE INDEX IX_Friends_UserId1
    ON Friends (UserId1);

CREATE INDEX IX_Friends_UserId2
    ON Friends (UserId2);

-- Look up reactions for a specific message
CREATE INDEX IX_Messages_ParentMessageId
    ON Messages (ParentMessageId);
GO

PRINT 'ChatModule schema created successfully.';
GO
