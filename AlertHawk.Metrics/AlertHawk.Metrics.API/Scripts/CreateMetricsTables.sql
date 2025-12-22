-- =============================================
-- AlertHawk Metrics Tables for Azure SQL
-- =============================================
-- This script creates the necessary tables for:
-- 1. MetricsNotification - Cluster-level notification configuration
-- 2. MetricsAlert - Node metrics alerts history
-- =============================================

-- =============================================
-- Table: MetricsNotification
-- Purpose: Stores notification configurations at cluster level
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MetricsNotification]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MetricsNotification] (
        [ClusterName] NVARCHAR(255) NOT NULL,
        [NotificationId] INT NOT NULL,
        CONSTRAINT [PK_MetricsNotification] PRIMARY KEY CLUSTERED ([ClusterName] ASC, [NotificationId] ASC)
    );
    
    -- Create index on ClusterName for faster lookups
    CREATE NONCLUSTERED INDEX [IX_MetricsNotification_ClusterName] 
    ON [dbo].[MetricsNotification] ([ClusterName] ASC);
    
    PRINT 'Table MetricsNotification created successfully.';
END
ELSE
BEGIN
    PRINT 'Table MetricsNotification already exists.';
END
GO

-- =============================================
-- Table: MetricsAlert
-- Purpose: Stores node metrics alerts history
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MetricsAlert]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MetricsAlert] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [NodeName] NVARCHAR(255) NOT NULL,
        [ClusterName] NVARCHAR(255) NOT NULL,
        [TimeStamp] DATETIME2 NOT NULL,
        [Status] BIT NOT NULL,
        [Message] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_MetricsAlert] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    -- Create indexes for common query patterns
    CREATE NONCLUSTERED INDEX [IX_MetricsAlert_ClusterName_TimeStamp] 
    ON [dbo].[MetricsAlert] ([ClusterName] ASC, [TimeStamp] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_MetricsAlert_NodeName_TimeStamp] 
    ON [dbo].[MetricsAlert] ([NodeName] ASC, [TimeStamp] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_MetricsAlert_ClusterName_NodeName_TimeStamp] 
    ON [dbo].[MetricsAlert] ([ClusterName] ASC, [NodeName] ASC, [TimeStamp] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_MetricsAlert_TimeStamp] 
    ON [dbo].[MetricsAlert] ([TimeStamp] DESC);
    
    PRINT 'Table MetricsAlert created successfully.';
END
ELSE
BEGIN
    PRINT 'Table MetricsAlert already exists.';
END
GO

-- =============================================
-- Optional: Add foreign key constraint if Notification table exists
-- Uncomment if you want to enforce referential integrity
-- =============================================
-- IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notification]') AND type in (N'U'))
-- BEGIN
--     IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MetricsNotification_Notification')
--     BEGIN
--         ALTER TABLE [dbo].[MetricsNotification]
--         ADD CONSTRAINT [FK_MetricsNotification_Notification]
--         FOREIGN KEY ([NotificationId]) REFERENCES [dbo].[Notification]([Id]);
--         
--         PRINT 'Foreign key constraint FK_MetricsNotification_Notification added successfully.';
--     END
-- END
-- GO

-- =============================================
-- Optional: Add sample data (commented out)
-- =============================================
-- INSERT INTO [dbo].[MetricsNotification] ([ClusterName], [NotificationId])
-- VALUES 
--     ('production-cluster', 1),
--     ('production-cluster', 2),
--     ('staging-cluster', 1);
-- GO

PRINT 'Script execution completed.';
GO
