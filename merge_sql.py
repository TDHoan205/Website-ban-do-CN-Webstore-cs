import os

# Original sql file
sql_original = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
sql_v2 = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database_v2.sql'

missing_tables = """
-- ==============================================
-- AI & KNOWLEDGE TABLES (Added to sync with EF Core)
-- ==============================================

CREATE TABLE [AIConversationLogs] (
    [log_id] int NOT NULL IDENTITY,
    [session_id] uniqueidentifier NOT NULL,
    [user_message] nvarchar(max) NOT NULL,
    [ai_response] nvarchar(max) NOT NULL,
    [intent_detected] nvarchar(50) NULL,
    [confidence_score] decimal(18,2) NULL,
    [was_escalated] bit NOT NULL,
    [user_rating] int NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_AIConversationLogs] PRIMARY KEY ([log_id])
);
GO

CREATE TABLE [FAQs] (
    [faq_id] int NOT NULL IDENTITY,
    [question] nvarchar(max) NOT NULL,
    [answer] nvarchar(max) NOT NULL,
    [category] nvarchar(50) NOT NULL,
    [keywords] nvarchar(max) NULL,
    [priority] int NOT NULL,
    [is_active] bit NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_FAQs] PRIMARY KEY ([faq_id])
);
GO

CREATE TABLE [KnowledgeChunks] (
    [chunk_id] int NOT NULL IDENTITY,
    [source_type] nvarchar(20) NOT NULL,
    [source_id] int NOT NULL,
    [chunk_type] nvarchar(30) NOT NULL,
    [raw_text] nvarchar(max) NOT NULL,
    [normalized_text] nvarchar(max) NOT NULL,
    [embedding] nvarchar(max) NOT NULL,
    [price] decimal(10,2) NULL,
    [category] nvarchar(100) NULL,
    [priority] int NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_KnowledgeChunks] PRIMARY KEY ([chunk_id])
);
GO

CREATE TABLE [ChatSessions] (
    [session_id] uniqueidentifier NOT NULL,
    [account_id] int NULL,
    [status] nvarchar(20) NOT NULL,
    [assigned_to] int NULL,
    [started_at] datetime2 NOT NULL,
    [ended_at] datetime2 NULL,
    CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([session_id]),
    CONSTRAINT [FK_ChatSessions_Accounts_account_id] FOREIGN KEY ([account_id]) REFERENCES [Accounts] ([account_id])
);
GO

CREATE TABLE [Notifications] (
    [notification_id] int NOT NULL IDENTITY,
    [account_id] int NULL,
    [type] nvarchar(50) NOT NULL,
    [message] nvarchar(max) NOT NULL,
    [is_read] bit NOT NULL,
    [link] nvarchar(255) NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([notification_id]),
    CONSTRAINT [FK_Notifications_Accounts_account_id] FOREIGN KEY ([account_id]) REFERENCES [Accounts] ([account_id])
);
GO

CREATE TABLE [ChatMessages] (
    [message_id] int NOT NULL IDENTITY,
    [session_id] uniqueidentifier NOT NULL,
    [message] nvarchar(max) NOT NULL,
    [sender_type] nvarchar(10) NOT NULL,
    [created_at] datetime2 NOT NULL,
    [metadata] nvarchar(max) NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([message_id]),
    CONSTRAINT [FK_ChatMessages_ChatSessions_session_id] FOREIGN KEY ([session_id]) REFERENCES [ChatSessions] ([session_id]) ON DELETE CASCADE
);
GO

-- CREATE MISSING INDEXES
CREATE INDEX [IX_ChatMessages_session_id] ON [ChatMessages] ([session_id]);
GO
CREATE INDEX [IX_ChatSessions_account_id] ON [ChatSessions] ([account_id]);
GO
CREATE INDEX [IX_KnowledgeChunks_category] ON [KnowledgeChunks] ([category]);
GO
CREATE INDEX [IX_KnowledgeChunks_source_type_source_id] ON [KnowledgeChunks] ([source_type], [source_id]);
GO
CREATE INDEX [IX_Notifications_account_id] ON [Notifications] ([account_id]);
GO

"""

with open(sql_original, 'r', encoding='utf-8') as f:
    orig_content = f.read()

# We need to insert the missing tables right BEFORE the "============= MOCK DATA =============" section
# so that the tables exist before anything else happens.
data_section_marker = "============= MOCK DATA ============="
insert_point = orig_content.find(data_section_marker)

if insert_point != -1:
    before = orig_content[:insert_point]
    after = orig_content[insert_point:]
    new_content = before + missing_tables + "\n\n-- " + after
else:
    new_content = orig_content + "\n\n" + missing_tables

with open(sql_v2, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Created {sql_v2}")
