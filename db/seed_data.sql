-- ============================================================
--  ChatModule Seed Data
--  Run AFTER schema.sql has been applied.
--  Covers: Users, Friends, Conversations, Participants, Messages
--  (reactions + system messages included for full coverage)
-- ============================================================

USE ChatModule;
GO

-- ============================================================
--  USERS  (8 users — mix of statuses, some with optional fields)
-- ============================================================
DECLARE @U1  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @U2  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @U4  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @U5  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @U6  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';
DECLARE @U7  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000007';
DECLARE @U8  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000008';

INSERT INTO Users (Id, Username, Email, PasswordHash, AvatarUrl, Bio, Status, Birthday, Phone)
VALUES
-- Online users
(@U1, 'alice_dev',   'alice@example.com',   '$2b$10$hashedpassword1', 'https://i.pravatar.cc/150?u=1', 'Full-stack dev. Coffee enthusiast.',         0, '1995-04-12', '+40712345001'),
(@U2, 'bob_builder', 'bob@example.com',     '$2b$10$hashedpassword2', 'https://i.pravatar.cc/150?u=2', 'Building things one commit at a time.',     0, '1993-07-22', '+40712345002'),
(@U3, 'carol_ux',    'carol@example.com',   '$2b$10$hashedpassword3', 'https://i.pravatar.cc/150?u=3', 'UX designer. Pixel-perfect or bust.',        0, '1997-01-30', NULL),
-- Busy users
(@U4, 'dan_ops',     'dan@example.com',     '$2b$10$hashedpassword4', 'https://i.pravatar.cc/150?u=4', 'DevOps. Kubernetes whisperer.',              2, '1990-11-05', '+40712345004'),
(@U5, 'eva_pm',      'eva@example.com',     '$2b$10$hashedpassword5', 'https://i.pravatar.cc/150?u=5', 'Product manager. Roadmap queen.',            2, '1992-03-18', '+40712345005'),
-- Offline users
(@U6, 'frank_qa',    'frank@example.com',   '$2b$10$hashedpassword6', NULL,                            'QA engineer. I break things for a living.',  1, '1988-09-14', NULL),
(@U7, 'grace_data',  'grace@example.com',   '$2b$10$hashedpassword7', 'https://i.pravatar.cc/150?u=7', 'Data analyst. SQL all day.',                 1, '1999-06-25', '+40712345007'),
(@U8, 'henry_sec',   'henry@example.com',   '$2b$10$hashedpassword8', 'https://i.pravatar.cc/150?u=8', NULL,                                        1, NULL,         NULL);
GO

-- ============================================================
--  FRIENDS
--  Status: 0=Pending, 1=Accepted, 2=Blocked
-- ============================================================
DECLARE @U1 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @U2 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @U4 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @U5 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @U6 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';
DECLARE @U7 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000007';
DECLARE @U8 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000008';

INSERT INTO Friends (Id, UserId1, UserId2, Status, IsMatch)
VALUES
-- Accepted friendships
(NEWID(), @U1, @U2, 1, 0),   -- alice <-> bob
(NEWID(), @U1, @U3, 1, 0),   -- alice <-> carol
(NEWID(), @U1, @U4, 1, 1),   -- alice <-> dan  (matchmaking match)
(NEWID(), @U2, @U3, 1, 0),   -- bob   <-> carol
(NEWID(), @U2, @U5, 1, 1),   -- bob   <-> eva  (matchmaking match)
(NEWID(), @U3, @U6, 1, 0),   -- carol <-> frank
(NEWID(), @U4, @U5, 1, 0),   -- dan   <-> eva
(NEWID(), @U5, @U7, 1, 0),   -- eva   <-> grace
-- Pending requests
(NEWID(), @U6, @U1, 0, 0),   -- frank -> alice  (pending)
(NEWID(), @U7, @U2, 0, 0),   -- grace -> bob    (pending)
-- Blocked
(NEWID(), @U8, @U3, 2, 0);   -- henry blocked carol
GO

-- ============================================================
--  CONVERSATIONS
--  Type: 0=DM, 1=Group
-- ============================================================
DECLARE @U1  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @U2  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @U4  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @U5  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';

-- DMs
DECLARE @DM_Alice_Bob    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @DM_Alice_Carol  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';
DECLARE @DM_Bob_Eva      UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000003';
-- Groups
DECLARE @GRP_Dev         UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000004';
DECLARE @GRP_AllHands    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000005';

INSERT INTO Conversations (Id, Type, Title, IconUrl, CreatedBy)
VALUES
(@DM_Alice_Bob,   0, NULL,            NULL,                                 @U1),
(@DM_Alice_Carol, 0, NULL,            NULL,                                 @U1),
(@DM_Bob_Eva,     0, NULL,            NULL,                                 @U2),
(@GRP_Dev,        1, 'Dev Team',      'https://i.pravatar.cc/150?u=grp1',   @U1),
(@GRP_AllHands,   1, 'All Hands',     'https://i.pravatar.cc/150?u=grp2',   @U5);
GO

-- ============================================================
--  MESSAGES
--  We insert messages first so we can later set PinnedMessageId
--  and LastReadMessageId via UPDATE.
-- ============================================================
DECLARE @U1 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @U2 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @U4 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @U5 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @U6 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';

DECLARE @DM_Alice_Bob    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @DM_Alice_Carol  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';
DECLARE @DM_Bob_Eva      UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000003';
DECLARE @GRP_Dev         UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000004';
DECLARE @GRP_AllHands    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000005';

-- Message IDs we'll reference (replies, reactions, pinning)
DECLARE @MSG1  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
DECLARE @MSG2  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000002';
DECLARE @MSG3  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';
DECLARE @MSG4  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000004';
DECLARE @MSG5  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000005';
DECLARE @MSG6  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000006';
DECLARE @MSG7  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000007';
DECLARE @MSG8  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000008';
DECLARE @MSG9  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000009';
DECLARE @MSG10 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000010';
DECLARE @MSG11 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000011';
DECLARE @MSG12 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000012';
DECLARE @MSG13 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000013';
DECLARE @MSG14 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000014';
DECLARE @MSG15 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000015';
DECLARE @MSG16 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000016'; -- reaction
DECLARE @MSG17 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000017'; -- reaction
DECLARE @MSG18 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000018'; -- system
DECLARE @MSG19 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000019'; -- system
DECLARE @MSG20 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000020';
DECLARE @MSG21 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000021'; -- All Hands reminder

-- ── DM: Alice ↔ Bob ──────────────────────────────────────────
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG1, @DM_Alice_Bob, @U1, 'Hey Bob! Did you push the auth branch yet?',        0, '2026-03-20 09:00:00'),
(@MSG2, @DM_Alice_Bob, @U2, 'Just pushed it. Let me know if CI passes for you.', 0, '2026-03-20 09:02:00'),
(@MSG3, @DM_Alice_Bob, @U1, 'Pipeline is green. Nice work!',                     0, '2026-03-20 09:05:00');

-- Reply from Bob to MSG3
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, ReplyToId, CreatedAt)
VALUES
(@MSG4, @DM_Alice_Bob, @U2, 'Thanks! Want to do a quick code review call?', 0, @MSG3, '2026-03-20 09:07:00');

-- Edited message
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, IsEdited, CreatedAt)
VALUES
(@MSG5, @DM_Alice_Bob, @U1, 'Sure, 10am works for me!', 0, 1, '2026-03-20 09:10:00');

-- ── DM: Alice ↔ Carol ────────────────────────────────────────
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG6,  @DM_Alice_Carol, @U1, 'Carol, can you review the new onboarding screens?', 0, '2026-03-21 11:00:00'),
(@MSG7,  @DM_Alice_Carol, @U3, 'Sure! Sending feedback shortly.',                   0, '2026-03-21 11:05:00'),
(@MSG8,  @DM_Alice_Carol, @U3, 'The contrast on the CTA button is too low. Fix it before the demo.', 0, '2026-03-21 11:10:00');

-- Message with attachment
INSERT INTO Messages (Id, ConversationId, UserId, Content, AttachmentUrl, MessageType, CreatedAt)
VALUES
(@MSG9, @DM_Alice_Carol, @U3, 'Here is the reference design.', '/uploads/reference_design.png', 0, '2026-03-21 11:12:00');

-- ── DM: Bob ↔ Eva ────────────────────────────────────────────
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG10, @DM_Bob_Eva, @U5, 'Bob, sprint planning is moved to Thursday.',      0, '2026-03-22 08:30:00'),
(@MSG11, @DM_Bob_Eva, @U2, 'Got it. I will update my calendar.',              0, '2026-03-22 08:32:00'),
(@MSG12, @DM_Bob_Eva, @U5, 'Also, can you get estimates for the search epic?', 0, '2026-03-22 08:33:00');

-- Message with link preview
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, LinkPreviewTitle, LinkPreviewDesc, CreatedAt)
VALUES
(@MSG13, @DM_Bob_Eva, @U2,
 'I put the estimates in the doc: https://docs.example.com/search-epic',
 0,
 'Search Epic Estimates',
 'Story-point breakdown for the search epic — Q2 2026',
 '2026-03-22 09:00:00');

-- ── Group: Dev Team ──────────────────────────────────────────
-- System message: group created
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG18, @GRP_Dev, NULL, 'alice_dev created the group Dev Team.', 2, '2026-03-15 10:00:00');

INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG14, @GRP_Dev, @U1, 'Hey team! Stand-up in 10.',                           0, '2026-03-25 09:50:00'),
(@MSG15, @GRP_Dev, @U2, 'On my way.',                                          0, '2026-03-25 09:51:00'),
(@MSG20, @GRP_Dev, @U4, 'Prod deploy is still pending, heads-up everyone.',    0, '2026-03-25 09:52:00');

-- ── Group: All Hands ─────────────────────────────────────────
-- System message: group created
INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG19, @GRP_AllHands, NULL, 'eva_pm created the group All Hands.', 2, '2026-03-10 08:00:00');

INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, CreatedAt)
VALUES
(@MSG21, @GRP_AllHands, @U5,
 'Reminder: Q1 retrospective is this Friday at 3pm. Please fill in the survey before then.',
 0, '2026-03-24 12:00:00');
GO

-- ── Reactions ────────────────────────────────────────────────
-- Bob reacts 👍 to Alice's MSG3 ("Pipeline is green")
-- Carol reacts ❤️ to Alice's MSG6 in the DM
DECLARE @U2 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @DM_Alice_Bob   UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @DM_Alice_Carol UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';
DECLARE @MSG3 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';
DECLARE @MSG6 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000006';
DECLARE @MSG16 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000016';
DECLARE @MSG17 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000017';

INSERT INTO Messages (Id, ConversationId, UserId, Content, MessageType, ParentMessageId, CreatedAt)
VALUES
(@MSG16, @DM_Alice_Bob,   @U2, N'👍', 1, @MSG3, '2026-03-20 09:06:00'),
(@MSG17, @DM_Alice_Carol, @U3, N'❤️', 1, @MSG6, '2026-03-21 11:01:00');
GO

-- ============================================================
--  PARTICIPANTS
--  Role: 0=Admin, 1=Member, 2=Banned
-- ============================================================
DECLARE @U1 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @U2 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @U3 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @U4 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @U5 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @U6 UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';

DECLARE @DM_Alice_Bob    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @DM_Alice_Carol  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';
DECLARE @DM_Bob_Eva      UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000003';
DECLARE @GRP_Dev         UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000004';
DECLARE @GRP_AllHands    UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000005';

DECLARE @MSG4  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000004';
DECLARE @MSG5  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000005';
DECLARE @MSG8  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000008';
DECLARE @MSG9  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000009';
DECLARE @MSG13 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000013';
DECLARE @MSG15 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000015';
DECLARE @MSG20 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000020';

-- DM: Alice <-> Bob
INSERT INTO Participants (Id, ConversationId, UserId, Role, LastReadMessageId, IsFavourite, IsNew)
VALUES
(NEWID(), @DM_Alice_Bob, @U1, 1, @MSG5,  1, 0),
(NEWID(), @DM_Alice_Bob, @U2, 1, @MSG4,  0, 0);

-- DM: Alice <-> Carol
INSERT INTO Participants (Id, ConversationId, UserId, Role, LastReadMessageId, IsNew)
VALUES
(NEWID(), @DM_Alice_Carol, @U1, 1, @MSG9, 0),
(NEWID(), @DM_Alice_Carol, @U3, 1, @MSG8, 1);  -- Carol hasn't seen the attachment yet

-- DM: Bob <-> Eva
INSERT INTO Participants (Id, ConversationId, UserId, Role, LastReadMessageId, IsNew)
VALUES
(NEWID(), @DM_Bob_Eva, @U2, 1, @MSG13, 0),
(NEWID(), @DM_Bob_Eva, @U5, 1, @MSG13, 0);

-- Group: Dev Team  (alice=Admin, bob=Member, dan=Member, frank=Banned)
INSERT INTO Participants (Id, ConversationId, UserId, Role, LastReadMessageId, Nickname, IsNew)
VALUES
(NEWID(), @GRP_Dev, @U1, 0, @MSG20, 'Alice',   0),   -- Admin
(NEWID(), @GRP_Dev, @U2, 1, @MSG20, NULL,      0),
(NEWID(), @GRP_Dev, @U4, 1, @MSG15, NULL,      0),   -- hasn't read Dan's latest message
(NEWID(), @GRP_Dev, @U6, 2, NULL,   'FrankQA', 0);   -- Banned

-- Group: All Hands  (eva=Admin, everyone else=Member)
INSERT INTO Participants (Id, ConversationId, UserId, Role, IsNew)
VALUES
(NEWID(), @GRP_AllHands, @U5, 0, 0),   -- Admin
(NEWID(), @GRP_AllHands, @U1, 1, 0),
(NEWID(), @GRP_AllHands, @U2, 1, 0),
(NEWID(), @GRP_AllHands, @U3, 1, 0),
(NEWID(), @GRP_AllHands, @U4, 1, 0),
(NEWID(), @GRP_AllHands, @U6, 1, 1);   -- frank hasn't opened this group yet
GO

-- ============================================================
--  PIN a message in Dev Team group
--  (MSG20: "Prod deploy is still pending...")
-- ============================================================
DECLARE @GRP_Dev UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000004';
DECLARE @MSG20   UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000020';

UPDATE Conversations
   SET PinnedMessageId = @MSG20
WHERE  Id = @GRP_Dev;
GO

PRINT 'Seed data inserted successfully.';
GO
